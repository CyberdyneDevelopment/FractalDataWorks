using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Fdw.Commands.Data;
using Fdw.Commands.Data.Abstractions;
using Fdw.Data;
using Fdw.Data.Abstractions;
using Fdw.Data.DataSets.Abstractions;
using Fdw.Data.Transformations.Configuration;
using Fdw.Messages;
using Fdw.Results;
using Fdw.ServiceTypes;
using Fdw.Configuration;
using Fdw.Services.Connections.Abstractions;
using Fdw.Services.Connections.Http.Abstractions;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Etl.Results;
using Fdw.Services.Etl.Abstractions;
using Fdw.Services.Etl.Abstractions.Execution;
using Fdw.Services.Etl.Abstractions.OptionTypes;
using Fdw.Services.Etl.Execution;
using Fdw.Services.Etl.Logging;
using Fdw.Services.Etl.Pipelines.Loads;
using Fdw.Services.Etl.Pipelines.Loads.Builders;
using Fdw.Services.Etl.Transforms;
using Fdw.Services.Pipelines.Abstractions.DataDestination;
using Fdw.Services.Notifications.Abstractions;
using Fdw.Services.Pipelines.Abstractions.DataSource;
using Fdw.Services.Pipelines.Notifications;
using Fdw.Services.Settings;

namespace Fdw.Services.Etl.Pipelines;

/// <summary>
/// Batch copy pipeline implementation that copies data in batches.
/// </summary>
public sealed class BatchCopyPipeline : EtlPipelineBase
{
    private readonly BatchCopyPipelineConfiguration _configuration;
    private readonly ILogger<BatchCopyPipeline> _logger;
    private readonly IDataGatewayProvider? _dataGateways;
    private readonly object? _calculationEngine;
    private readonly IConnectionProvider? _connectionProvider;
    private readonly IDataStoreProvider? _dataStoreProvider;
    private readonly IPipelineStatusBroadcaster? _broadcaster;
    private readonly IDataSetConfigurationProvider? _dataSets;
    private readonly IConnectionTypeResolver? _connectionTypes;
    private readonly ITransformationConfigurationProvider? _transformations;
    private readonly IRuleNotifier? _ruleNotifier;
    private readonly IEffectiveSettingsProvider? _settings;
    private int _isExecuting;

    /// <summary>
    /// Initializes a new instance of the <see cref="BatchCopyPipeline"/> class.
    /// </summary>
    /// <param name="configuration">The pipeline configuration.</param>
    /// <param name="logger">The logger instance.</param>
    /// <param name="dataGateways">The data gateway for executing commands.</param>
    /// <param name="calculationEngine">The calculation engine for transforms (optional).</param>
    /// <param name="connectionProvider">The connection provider for feature-detecting write capabilities (optional).</param>
    /// <param name="dataStoreProvider">The data store provider for resolving container metadata (optional; required for HTTP record writer path).</param>
    /// <param name="broadcaster">Optional broadcaster for live status updates (optional).</param>
    /// <param name="dataSets">The data set configuration provider that resolves the destination and source of load steps (optional; required to run load steps).</param>
    /// <param name="connectionTypes">Finds a connection's type, so load steps are checked against the capabilities the type lists (optional; required to run load steps).</param>
    /// <param name="transformations">The Transformation provider that resolves the named Transformations a field mapping's chain applies (optional; required to run a pipeline whose mappings state a chain).</param>
    /// <param name="ruleNotifier">Sends a Notify stage's notification through its rule (optional; required to run a pipeline that has a Notify stage).</param>
    /// <param name="settings">The settings provider the calculation limits of a Compute stage are read from when a run starts (optional; required to run a pipeline that has a Compute stage).</param>
    public BatchCopyPipeline(
        BatchCopyPipelineConfiguration configuration,
        ILogger<BatchCopyPipeline> logger,
        IDataGatewayProvider? dataGateways = null,
        object? calculationEngine = null,
        IConnectionProvider? connectionProvider = null,
        IDataStoreProvider? dataStoreProvider = null,
        IPipelineStatusBroadcaster? broadcaster = null,
        IDataSetConfigurationProvider? dataSets = null,
        IConnectionTypeResolver? connectionTypes = null,
        ITransformationConfigurationProvider? transformations = null,
        IRuleNotifier? ruleNotifier = null,
        IEffectiveSettingsProvider? settings = null)
        : base(logger)
    {
        _configuration = configuration;
        _logger = logger;
        _dataGateways = dataGateways;
        _calculationEngine = calculationEngine;
        _connectionProvider = connectionProvider;
        _dataStoreProvider = dataStoreProvider;
        _broadcaster = broadcaster;
        _dataSets = dataSets;
        _connectionTypes = connectionTypes;
        _transformations = transformations;
        _ruleNotifier = ruleNotifier;
        _settings = settings;
    }

    /// <inheritdoc />
    public override string Id => _configuration.Id.ToString();

    /// <inheritdoc />
    public override string Name => _configuration.Name;

    /// <inheritdoc />
    public override string PipelineType => "BatchCopy";

    /// <inheritdoc />
    public override bool IsExecuting => _isExecuting == 1;

    /// <inheritdoc />
    public override Task<IGenericResult<IEtlPipelineExecutionResult>> Execute(CancellationToken cancellationToken = default)
        => Execute(PipelineExecutionOptions.Production, cancellationToken);

    /// <inheritdoc />
    public override async Task<IGenericResult<IEtlPipelineExecutionResult>> Execute(
        PipelineExecutionOptions options,
        CancellationToken cancellationToken = default)
    {
        if (Interlocked.CompareExchange(ref _isExecuting, 1, 0) != 0)
        {
            return GenericResult<IEtlPipelineExecutionResult>.Failure(
                EtlLog.ExecutionFailed(_logger, Name, Guid.Empty, "Pipeline is already executing"));
        }

        var executionId = Guid.NewGuid();
        var result = new EtlPipelineExecutionResult(executionId);
        var stopwatch = Stopwatch.StartNew();

        try
        {
            EtlLog.ExecutionStarted(_logger, Name, executionId);

            if (_dataGateways == null)
            {
                return GenericResult<IEtlPipelineExecutionResult>.Failure(
                    EtlLog.ExecutionFailed(_logger, Name, executionId, "DataGateway is required for pipeline execution"));
            }

            var effectiveBatchSize = options.IsTestMode
                ? Math.Min(_configuration.BatchSize, options.MaxRowsPerSource)
                : _configuration.BatchSize;

            IDataGateway gateway;
            try
            {
                gateway = _dataGateways.ByName("Main");
            }
            catch (InvalidOperationException ex)
            {
                return GenericResult<IEtlPipelineExecutionResult>.Failure(
                    EtlLog.ExecutionFailed(_logger, Name, executionId, ex.Message));
            }

            var phaseResult = await ExecutePhases(gateway, options, effectiveBatchSize, result, cancellationToken).ConfigureAwait(false);
            if (!phaseResult.IsSuccess)
            {
                return phaseResult;
            }

            stopwatch.Stop();
            EtlLog.ExecutionCompleted(_logger, Name, executionId, result.RecordsLoaded, stopwatch.Elapsed.TotalMilliseconds);
            EtlLog.ExecutionMetrics(_logger, Name, result.RecordsExtracted, result.RecordsTransformed, result.RecordsLoaded, result.RecordsFailed);

            return GenericResult<IEtlPipelineExecutionResult>.Success(result);
        }
        catch (OperationCanceledException ex) when (cancellationToken.IsCancellationRequested)
        {
            result.AddError($"Pipeline execution was cancelled: {ex.Message}");
            result.Complete();
            return GenericResult<IEtlPipelineExecutionResult>.Failure(
                EtlLog.ExecutionFailed(_logger, Name, executionId, "Execution cancelled"));
        }
        catch (Exception ex)
        {
            result.AddError(ex.Message);
            result.Complete();
            return GenericResult<IEtlPipelineExecutionResult>.Failure(
                EtlLog.ExecutionFailed(_logger, Name, executionId, ex.Message));
        }
        finally
        {
            Interlocked.Exchange(ref _isExecuting, 0);
        }
    }

    // What the three whole-set phases used to keep in locals, now that a run moves page by page. A run
    // is one Execute call and Execute is not re-entrant (_isExecuting), so this is never shared.
    private sealed class RunState(
        TransformContext transformContext,
        List<PipelineTransformConfiguration> transforms,
        bool streams,
        PipelineLoadPlan plan,
        PipelineRunParameterBinder parameters,
        DateTimeOffset startedAt)
    {
        // What the run does to load its output: the load mode or the ordered load steps.
        public PipelineLoadPlan Plan { get; } = plan;

        // The run's typed parameter values and the watermark row it moves forward on success.
        public PipelineRunParameterBinder Parameters { get; } = parameters;

        // When the run began: the watermark a successful run stores, and the moment a value source stamps.
        public DateTimeOffset StartedAt { get; } = startedAt;

        public TransformContext TransformContext { get; } = transformContext;

        public List<PipelineTransformConfiguration> Transforms { get; } = transforms;

        // True when each page can be transformed and loaded as it arrives. False when the result would
        // differ from handling the whole set at once, in which case pages are gathered first.
        public bool Streams { get; } = streams;

        public int Batches { get; set; }

        public int StepErrors { get; set; }

        public bool Truncated { get; set; }
    }

    private async Task<IGenericResult<IEtlPipelineExecutionResult>> ExecutePhases(
        IDataGateway gateway,
        PipelineExecutionOptions options,
        int effectiveBatchSize,
        EtlPipelineExecutionResult result,
        CancellationToken cancellationToken)
    {
        // The Notify stages are gathered and checked before anything is read: a stage that cannot send fails the run at
        // once, and its OnFailure hooks are not trusted to fire.
        var hooks = NotifyHooks.Collect(_configuration.Transforms);
        var checkedHooks = await hooks.Check(_ruleNotifier, Name, _logger, cancellationToken).ConfigureAwait(false);
        if (!checkedHooks.IsSuccess)
        {
            result.Complete();
            return checkedHooks.ToNewResult<IEtlPipelineExecutionResult>();
        }

        var phases = await RunPhasesObserved(hooks, gateway, options, effectiveBatchSize, result, cancellationToken).ConfigureAwait(false);
        if (!phases.IsSuccess)
        {
            await FireHooks(hooks, options, point => point.FiresOnFailure, result, cancellationToken).ConfigureAwait(false);
            result.Complete();
            return phases.ToNewResult<IEtlPipelineExecutionResult>();
        }

        result.Complete();
        return GenericResult<IEtlPipelineExecutionResult>.Success(result);
    }

    // An unexpected exception fails the run exactly as Execute reports it, but here, so the OnFailure hooks still fire.
    private async Task<IGenericResult> RunPhasesObserved(
        NotifyHooks hooks,
        IDataGateway gateway,
        PipelineExecutionOptions options,
        int effectiveBatchSize,
        EtlPipelineExecutionResult result,
        CancellationToken cancellationToken)
    {
        try
        {
            return await RunPhases(hooks, gateway, options, effectiveBatchSize, result, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            result.AddError(ex.Message);
            return GenericResult.Failure(EtlLog.ExecutionFailed(_logger, Name, result.ExecutionId, ex.Message));
        }
    }

    private async Task<IGenericResult> RunPhases(
        NotifyHooks hooks,
        IDataGateway gateway,
        PipelineExecutionOptions options,
        int effectiveBatchSize,
        EtlPipelineExecutionResult result,
        CancellationToken cancellationToken)
    {
        var prepared = await PrepareRun(gateway, options, result, cancellationToken).ConfigureAwait(false);
        if (!prepared.IsSuccess)
        {
            return prepared;
        }

        var streamed = await StreamBatches(gateway, options, effectiveBatchSize, prepared.Value!, result, cancellationToken).ConfigureAwait(false);
        if (!streamed.IsSuccess)
        {
            return streamed;
        }

        var afterStage = await FireHooks(hooks, options, point => point.FiresAfterStage, result, cancellationToken).ConfigureAwait(false);
        if (!afterStage.IsSuccess)
        {
            return afterStage;
        }

        var rejected = await RunRejectSteps(options, prepared.Value!, cancellationToken).ConfigureAwait(false);
        if (!rejected.IsSuccess)
        {
            return rejected;
        }

        var advanced = await RecordWatermark(gateway, options, prepared.Value!, result, cancellationToken).ConfigureAwait(false);
        if (!advanced.IsSuccess)
        {
            return advanced;
        }

        LogRunCompleted(options, result);
        return await FireHooks(hooks, options, point => point.FiresAfterLoad, result, cancellationToken).ConfigureAwait(false);
    }

    // A dry run (test mode that skips destination writes) has no side effects, so it sends nothing; the stages were
    // still checked at the start of the run.
    private async Task<IGenericResult> FireHooks(
        NotifyHooks hooks,
        PipelineExecutionOptions options,
        Func<IRunPoint, bool> firesAt,
        EtlPipelineExecutionResult result,
        CancellationToken cancellationToken)
    {
        if (hooks.IsEmpty || SkipsDestinationWrites(options))
        {
            return GenericResult.Success();
        }

        return await hooks.Fire(
            firesAt, NotifyRunFacts.Of(_configuration.Id, Name, result), _ruleNotifier!, _logger, cancellationToken).ConfigureAwait(false);
    }

    private async Task<IGenericResult<RunState>> PrepareRun(
        IDataGateway gateway,
        PipelineExecutionOptions options,
        EtlPipelineExecutionResult result,
        CancellationToken cancellationToken)
    {
        if (_configuration.SourceKind == null || _configuration.SourceKind == DataSourceKinds.NotFound)
        {
            return GenericResult<RunState>.Failure(EtlLog.SourceKindRequired(_logger, Name));
        }

        var plan = PipelineLoadPlan.Create(_configuration, _logger);
        if (!plan.IsSuccess)
        {
            return plan.ToNewResult<RunState>();
        }

        var parameters = await PipelineRunParameterBinder.Bind(
            gateway, EtlPipelineTypes.OperationalConnection, _configuration, options, _logger, cancellationToken).ConfigureAwait(false);
        if (!parameters.IsSuccess)
        {
            return parameters.ToNewResult<RunState>();
        }

        var writesWholeSet = await DestinationWritesWholeSet(options, cancellationToken).ConfigureAwait(false);
        if (!writesWholeSet.IsSuccess)
        {
            return writesWholeSet.ToNewResult<RunState>();
        }

        var context = await NewTransformContext(gateway, result, parameters.Value!, cancellationToken).ConfigureAwait(false);
        if (!context.IsSuccess)
        {
            return context.ToNewResult<RunState>();
        }

        var transforms = StageRunner.Ordered(_configuration.Transforms);
        return GenericResult<RunState>.Success(new RunState(
            context.Value!,
            transforms,
            streams: !writesWholeSet.Value
                && !plan.Value!.NeedsCompleteSet
                && !StageRunner.RequiresCompleteInput(transforms),
            plan.Value!,
            parameters.Value!,
            DateTimeOffset.UtcNow));
    }

    // Why the chains are resolved here, before any record moves: a Map stage's field mapping names its Transformations by id,
    // and a Transformation that no longer resolves must fail the run naming it, not be skipped halfway through the data.
    private async Task<IGenericResult<TransformContext>> NewTransformContext(
        IDataGateway gateway,
        EtlPipelineExecutionResult result,
        PipelineRunParameterBinder parameters,
        CancellationToken cancellationToken)
    {
        var chains = await FieldTransformChains.Resolve(StageRunner.Ordered(_configuration.Transforms), _transformations, _logger, cancellationToken).ConfigureAwait(false);
        if (!chains.IsSuccess)
        {
            return chains.ToNewResult<TransformContext>();
        }

        var built = await BuiltCalculationResolver.Resolve(StageRunner.Ordered(_configuration.Transforms), _transformations, _settings, _logger, cancellationToken).ConfigureAwait(false);
        if (!built.IsSuccess)
        {
            return built.ToNewResult<TransformContext>();
        }

        return GenericResult<TransformContext>.Success(new TransformContext(
            executionId: result.ExecutionId,
            logger: _logger,
            variables: new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase),
            calculationEngine: _calculationEngine,
            connectionProvider: _connectionProvider,
            dataGateway: gateway,
            parameters: parameters.Values,
            fieldTransformChains: chains.Value!,
            builtCalculations: built.Value!.Calculations,
            builtSets: built.Value.Sets));
    }

    // An HTTP record writer sends the whole set in one WriteRecords call, so it has to be handed the
    // whole set; every other destination takes batches.
    private async Task<IGenericResult<bool>> DestinationWritesWholeSet(
        PipelineExecutionOptions options,
        CancellationToken cancellationToken)
    {
        if (SkipsDestinationWrites(options)
            || _connectionProvider == null
            || !DestinationIsDataStore
            || DestinationProblem() is not null)
        {
            return GenericResult<bool>.Success(false);
        }

        var connectionResult = await DestinationConnection(cancellationToken).ConfigureAwait(false);
        return connectionResult.IsSuccess
            ? GenericResult<bool>.Success(connectionResult.Value is IHttpRecordWriterConnection)
            : connectionResult.ToNewResult<bool>();
    }

    private static bool SkipsDestinationWrites(PipelineExecutionOptions options)
        => options.IsTestMode && options.SkipDestinationWrites;

    // Reads the source a page at a time. When the run streams, each page is transformed and loaded before
    // the next is read, so memory is bounded by one page. Otherwise pages are gathered and handled as one
    // batch after the last, which is exactly what the whole-set phases did.
    private async Task<IGenericResult> StreamBatches(
        IDataGateway gateway,
        PipelineExecutionOptions options,
        int effectiveBatchSize,
        RunState run,
        EtlPipelineExecutionResult result,
        CancellationToken cancellationToken)
    {
        EtlLog.ExtractStarted(_logger, Name);
        EtlLog.TransformStarted(_logger, run.Transforms.Count);
        if (!SkipsDestinationWrites(options))
        {
            EtlLog.LoadStarted(_logger, Name);
        }

        var pending = new List<IDictionary<string, object?>>();
        var skip = 0;
        var hasMore = true;

        while (hasMore)
        {
            var page = await ExtractPage(gateway, skip, effectiveBatchSize, result, cancellationToken).ConfigureAwait(false);
            if (!page.IsSuccess)
            {
                return page;
            }

            skip += page.Value!.Count;
            hasMore = page.Value.Count > 0 && page.Value.Count == effectiveBatchSize;
            pending.AddRange(page.Value);

            if (pending.Count > 0 && (run.Streams || !hasMore))
            {
                var processed = await ProcessBatch(gateway, options, pending, run, result, cancellationToken).ConfigureAwait(false);
                if (!processed.IsSuccess)
                {
                    return processed;
                }

                pending.Clear();
            }
        }

        // An empty source still runs the transforms once, as it always did, so a misconfigured step is
        // reported instead of passing silently because there happened to be nothing to feed it.
        return run.Batches == 0
            ? await ProcessBatch(gateway, options, pending, run, result, cancellationToken).ConfigureAwait(false)
            : GenericResult.Success();
    }

    private async Task<IGenericResult> ProcessBatch(
        IDataGateway gateway,
        PipelineExecutionOptions options,
        List<IDictionary<string, object?>> records,
        RunState run,
        EtlPipelineExecutionResult result,
        CancellationToken cancellationToken)
    {
        run.Batches++;

        var transformed = await TransformRecords(records, run, result, cancellationToken).ConfigureAwait(false);
        if (!transformed.IsSuccess)
        {
            return transformed;
        }

        if (SkipsDestinationWrites(options))
        {
            result.RecordsLoaded += transformed.Value!.Count;
            return GenericResult.Success();
        }

        return await LoadRecords(gateway, transformed.Value!, run, result, cancellationToken).ConfigureAwait(false);
    }

    private void LogRunCompleted(PipelineExecutionOptions options, EtlPipelineExecutionResult result)
    {
        EtlLog.ExtractCompleted(_logger, result.RecordsExtracted, result.ExtractDuration.TotalMilliseconds);
        EtlLog.TransformCompleted(_logger, result.RecordsTransformed, result.TransformDuration.TotalMilliseconds);

        if (SkipsDestinationWrites(options))
        {
            EtlLog.TestModeWriteSkipped(_logger, Name, result.RecordsLoaded);
            return;
        }

        EtlLog.LoadCompleted(_logger, result.RecordsLoaded, result.LoadDuration.TotalMilliseconds);
    }

    private async Task<IGenericResult<List<IDictionary<string, object?>>>> ExtractPage(
        IDataGateway gateway,
        int skip,
        int effectiveBatchSize,
        EtlPipelineExecutionResult result,
        CancellationToken cancellationToken)
    {
        var extractStopwatch = Stopwatch.StartNew();

        try
        {
            cancellationToken.ThrowIfCancellationRequested();

            var page = await ReadPage(gateway, skip, effectiveBatchSize, cancellationToken).ConfigureAwait(false);
            if (page.IsSuccess)
            {
                result.RecordsExtracted += page.Value!.Count;
            }

            return page;
        }
        catch (Exception ex)
        {
            return GenericResult<List<IDictionary<string, object?>>>.Failure(
                EtlLog.ExtractFailed(_logger, ex, Name));
        }
        finally
        {
            result.ExtractDuration += extractStopwatch.Elapsed;
        }
    }

    private async Task<IGenericResult<List<IDictionary<string, object?>>>> ReadPage(
        IDataGateway gateway,
        int skip,
        int effectiveBatchSize,
        CancellationToken cancellationToken)
    {
        if (_configuration.SourceKind is not { } sourceKind)
        {
            return GenericResult<List<IDictionary<string, object?>>>.Failure(
                EtlLog.SourceKindRequired(_logger, Name));
        }

        if (string.Equals(sourceKind.Name, "DataSet", StringComparison.Ordinal))
        {
            // ELT path: resolve via dataset dispatch (carries store→path→container + RecordSelector).
            return await ExtractBatchFromDataSet(gateway, skip, effectiveBatchSize, cancellationToken).ConfigureAwait(false);
        }

        if (string.Equals(sourceKind.Name, "DataStore", StringComparison.Ordinal))
        {
            // ETL path: read one container of a configured data store, addressed by store, path and container.
            return await ExtractBatchFromDataStore(gateway, skip, effectiveBatchSize, cancellationToken).ConfigureAwait(false);
        }

        return GenericResult<List<IDictionary<string, object?>>>.Failure(
            EtlLog.UnknownSourceKind(_logger, Name, sourceKind.Name));
    }

    // ETL path: source is one container of a configured data store (FDW-1103). The gateway resolves the store as
    // the run's tenant-run principal, so row-level security and the refusal of host-declared stores apply exactly
    // as they do to any other read; nothing here names a connection type.
    private async Task<IGenericResult<List<IDictionary<string, object?>>>> ExtractBatchFromDataStore(
        IDataGateway gateway,
        int skip,
        int effectiveBatchSize,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(_configuration.SourceDataStoreName)
            || string.IsNullOrWhiteSpace(_configuration.SourceDataStorePath)
            || string.IsNullOrWhiteSpace(_configuration.SourceContainerName))
        {
            return GenericResult<List<IDictionary<string, object?>>>.Failure(
                EtlLog.ExtractFailed(_logger, Name, "SourceDataStoreName, SourceDataStorePath and SourceContainerName are all required when SourceKind is DataStore"));
        }

        var queryCommand = new QueryCommand<Dictionary<string, object?>>
        {
            Paging = new PagingExpression
            {
                Skip = skip,
                Take = effectiveBatchSize
            }
        };

        var queryResult = await gateway.Execute<IEnumerable<Dictionary<string, object?>>>(
            queryCommand,
            new DataStoreTarget(_configuration.SourceDataStoreName, _configuration.SourceDataStorePath, _configuration.SourceContainerName),
            cancellationToken).ConfigureAwait(false);

        if (!queryResult.IsSuccess)
        {
            return queryResult.ToNewResult<List<IDictionary<string, object?>>>();
        }

        if (queryResult.Value is not { } rows)
        {
            return GenericResult<List<IDictionary<string, object?>>>.Failure(
                EtlLog.ExtractFailed(_logger, Name, "the data store read succeeded but returned no row set"));
        }

        return GenericResult<List<IDictionary<string, object?>>>.Success(
            rows.Cast<IDictionary<string, object?>>().ToList());
    }

    // ELT path: source is a pre-defined logical DataSet — resolved through the DataSet dispatch,
    // the same read path the dataset-query endpoint uses. Connection is taken from the DataSet source.
    private async Task<IGenericResult<List<IDictionary<string, object?>>>> ExtractBatchFromDataSet(
        IDataGateway gateway,
        int skip,
        int effectiveBatchSize,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(_configuration.SourceDataSet))
        {
            return GenericResult<List<IDictionary<string, object?>>>.Failure(
                EtlResultCodes.ByName("SourceDataSetRequired"),
                ResultDetails.Create("PipelineName", Name));
        }

        var queryCommand = new QueryCommand<Dictionary<string, object?>>
        {
            Paging = new PagingExpression
            {
                Skip = skip,
                Take = effectiveBatchSize
            }
        };

        var queryResult = await gateway.Execute<IEnumerable<Dictionary<string, object?>>>(
            queryCommand, new DataSetTarget(_configuration.SourceDataSet), cancellationToken).ConfigureAwait(false);

        if (!queryResult.IsSuccess)
        {
            return GenericResult<List<IDictionary<string, object?>>>.Failure(
                EtlLog.ExtractFailed(_logger, Name, GetFirstMessageText(queryResult.Messages) ?? "Unknown error"));
        }

        var batch = queryResult.Value?.ToList() ?? [];
        return GenericResult<List<IDictionary<string, object?>>>.Success(
            batch.Cast<IDictionary<string, object?>>().ToList());
    }


    private async Task<IGenericResult<List<IDictionary<string, object?>>>> TransformRecords(
        List<IDictionary<string, object?>> records,
        RunState run,
        EtlPipelineExecutionResult result,
        CancellationToken cancellationToken)
    {
        var transformStopwatch = Stopwatch.StartNew();

        try
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (run.Transforms.Count == 0)
            {
                result.RecordsTransformed += records.Count;
                return GenericResult<List<IDictionary<string, object?>>>.Success(records);
            }

            return await TransformStepFold(records, run, result, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            return GenericResult<List<IDictionary<string, object?>>>.Failure(
                EtlLog.TransformFailed(_logger, ex, Name));
        }
        finally
        {
            result.TransformDuration += transformStopwatch.Elapsed;
        }
    }

    private async Task<IGenericResult<List<IDictionary<string, object?>>>> TransformStepFold(
        List<IDictionary<string, object?>> records,
        RunState run,
        EtlPipelineExecutionResult result,
        CancellationToken cancellationToken)
    {
        var currentRecords = records;

        foreach (var step in run.Transforms)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var transformType = TransformTypes.ByName(step.OperationType);
            if (transformType == TransformTypes.NotFound)
            {
                return GenericResult<List<IDictionary<string, object?>>>.Failure(
                    EtlLog.TransformFailed(_logger, Name, step.OperationType, $"Unknown transform type: {step.OperationType}"));
            }

            var inCount = currentRecords.Count;
            EtlLog.TransformStepStarted(_logger, step.Name, step.OperationType, inCount);

            var stepResult = await transformType.TransformBatch(currentRecords, step, run.TransformContext, cancellationToken).ConfigureAwait(false);

            if (!stepResult.IsSuccess)
            {
                if (!_configuration.ContinueOnError || StageFailures.StopsRun(stepResult))
                {
                    return stepResult.ToNewResult<List<IDictionary<string, object?>>>();
                }

                run.StepErrors++;
            }
            else
            {
                currentRecords = stepResult.Value?.ToList() ?? [];
            }

            EtlLog.TransformStepCompleted(_logger, step.Name, currentRecords.Count, inCount);

            if (run.StepErrors + run.TransformContext.Errors.Count >= _configuration.MaxErrors)
            {
                return GenericResult<List<IDictionary<string, object?>>>.Failure(
                    EtlLog.ExecutionFailed(_logger, Name, run.TransformContext.ExecutionId, $"Maximum error count ({_configuration.MaxErrors}) exceeded"));
            }
        }

        result.RecordsTransformed += currentRecords.Count;
        result.RecordsFailed = run.StepErrors + run.TransformContext.Errors.Count;

        return GenericResult<List<IDictionary<string, object?>>>.Success(currentRecords);
    }

    private async Task<IGenericResult> LoadRecords(
        IDataGateway gateway,
        List<IDictionary<string, object?>> records,
        RunState run,
        EtlPipelineExecutionResult result,
        CancellationToken cancellationToken)
    {
        var loadStopwatch = Stopwatch.StartNew();

        try
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (run.Plan.NeedsCompleteSet)
            {
                return await LoadThroughUnitOfWork(records, run, result, cancellationToken).ConfigureAwait(false);
            }

            if (_configuration.DestinationKind == null || _configuration.DestinationKind == DataDestinationKinds.NotFound)
            {
                return GenericResult.Failure(EtlLog.DestinationKindRequired(_logger, Name));
            }

            if (records.Count == 0)
            {
                return GenericResult.Success();
            }

            var toDataSet = string.Equals(_configuration.DestinationKind.Name, "DataSet", StringComparison.Ordinal);
            if (!toDataSet && !DestinationIsDataStore)
            {
                return GenericResult.Failure(EtlLog.UnknownDestinationKind(_logger, Name, _configuration.DestinationKind.Name));
            }

            var truncated = await TruncateOnce(gateway, toDataSet, run, cancellationToken).ConfigureAwait(false);
            if (!truncated.IsSuccess)
            {
                return truncated;
            }

            // DataSet is the ELT path (written through DataSet dispatch, which carries the store→path→
            // container address); DataStore is the ETL path (written straight to the named container of
            // a configured data store).
            var loaded = toDataSet
                ? await LoadToDataSet(gateway, records, run, result, cancellationToken).ConfigureAwait(false)
                : await LoadToContainer(gateway, records, run, result, cancellationToken).ConfigureAwait(false);
            if (!loaded.IsSuccess)
            {
                return loaded;
            }

            result.RecordsLoaded += loaded.Value;
            return GenericResult.Success();
        }
        catch (Exception ex)
        {
            return GenericResult.Failure(EtlLog.LoadFailed(_logger, ex, Name));
        }
        finally
        {
            result.LoadDuration += loadStopwatch.Elapsed;
        }
    }

    // Truncation happens once per run, immediately before the first batch is written -- after that batch
    // has been extracted and transformed, so an empty or unreadable source never empties the destination.
    private async Task<IGenericResult> TruncateOnce(
        IDataGateway gateway,
        bool toDataSet,
        RunState run,
        CancellationToken cancellationToken)
    {
        if (!run.Plan.TruncatesFirst || run.Truncated)
        {
            return GenericResult.Success();
        }

        var truncateResult = toDataSet
            ? await TruncateDataSet(gateway, cancellationToken).ConfigureAwait(false)
            : await TruncateContainer(gateway, cancellationToken).ConfigureAwait(false);
        if (truncateResult.IsSuccess)
        {
            run.Truncated = true;
        }

        return truncateResult;
    }

    // ELT path: destination is a single-source DataSet sink. Truncate is forwarded through the DataSet
    // dispatch which carries the full store→path→container address.
    private async Task<IGenericResult> TruncateDataSet(
        IDataGateway gateway,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(_configuration.DestinationDataSet))
        {
            return GenericResult.Failure(
                EtlLog.LoadFailed(_logger, Name, "DestinationDataSet is required when DestinationKind is DataSet"));
        }

        var truncateResult = await gateway.Execute<int>(
            new TruncateCommand(), new DataSetTarget(_configuration.DestinationDataSet), cancellationToken).ConfigureAwait(false);
        if (!truncateResult.IsSuccess)
        {
            return GenericResult.Failure(
                EtlLog.LoadFailed(_logger, Name, $"Failed to truncate destination: {GetFirstMessageText(truncateResult.Messages)}"));
        }

        return GenericResult.Success();
    }

    // ELT path: the pipeline's own load via DataSet dispatch.
    private async Task<IGenericResult<int>> LoadToDataSet(
        IDataGateway gateway,
        List<IDictionary<string, object?>> records,
        RunState run,
        EtlPipelineExecutionResult result,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(_configuration.DestinationDataSet))
        {
            return GenericResult<int>.Failure(
                EtlLog.LoadFailed(_logger, Name, "DestinationDataSet is required when DestinationKind is DataSet"));
        }

        var target = new DataSetTarget(_configuration.DestinationDataSet);
        return await LoadBatches(
            records, run, result, (command, token) => gateway.Execute<int>(command, target, token), cancellationToken).ConfigureAwait(false);
    }

    // Splits the output into batches, builds the load command of each (an insert, or an upsert) and runs it through
    // execute; a failed batch fails the load unless the pipeline continues on error.
    private async Task<IGenericResult<int>> LoadBatches(
        List<IDictionary<string, object?>> records,
        RunState run,
        EtlPipelineExecutionResult result,
        Func<IDataCommand, CancellationToken, Task<IGenericResult<int>>> execute,
        CancellationToken cancellationToken)
    {
        var totalLoaded = 0;
        var batches = records
            .Select((record, index) => new { record, index })
            .GroupBy(x => x.index / _configuration.BatchSize)
            .Select(g => g.Select(x => x.record).ToList())
            .ToList();

        foreach (var batch in batches)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var command = OwnLoadCommand(run, batch);
            if (!command.IsSuccess)
            {
                return command.ToNewResult<int>();
            }

            var insertResult = await execute(command.Value!, cancellationToken).ConfigureAwait(false);
            if (insertResult.IsSuccess)
            {
                totalLoaded += insertResult.Value;
            }
            else if (_configuration.ContinueOnError)
            {
                result.AddError($"Batch load failed: {GetFirstMessageText(insertResult.Messages)}");
            }
            else
            {
                return GenericResult<int>.Failure(
                    EtlLog.LoadFailed(_logger, Name, GetFirstMessageText(insertResult.Messages) ?? "Unknown error"));
            }
        }

        return GenericResult<int>.Success(totalLoaded);
    }

    // ETL path: truncate the named container of a data store.
    private async Task<IGenericResult> TruncateContainer(
        IDataGateway gateway,
        CancellationToken cancellationToken)
    {
        if (DestinationProblem() is { } problem)
        {
            return GenericResult.Failure(EtlLog.LoadFailed(_logger, Name, problem));
        }

        var target = DestinationTarget();
        EtlLog.LoadingToConnection(_logger, Name, target.DataStore, target.Container);

        var truncateResult = await gateway.Execute<int>(new TruncateCommand(), target, cancellationToken).ConfigureAwait(false);

        if (!truncateResult.IsSuccess)
        {
            return GenericResult.Failure(
                EtlLog.LoadFailed(_logger, Name, $"Failed to truncate connection destination: {GetFirstMessageText(truncateResult.Messages)}"));
        }

        return GenericResult.Success();
    }

    // ETL path: write directly to the physical connection + container.
    // Feature-detects the destination connection's write capability and branches:
    //   IHttpRecordWriterConnection → serialize via RecordWriterTypes and POST/PUT to configured endpoint
    //   fallthrough                  → BulkInsertCommand (SQL/tabular connections)
    // Fail loud if neither capability is present (NO FALLBACKS).
    private async Task<IGenericResult<int>> LoadToContainer(
        IDataGateway gateway,
        List<IDictionary<string, object?>> records,
        RunState run,
        EtlPipelineExecutionResult result,
        CancellationToken cancellationToken)
    {
        if (DestinationProblem() is { } problem)
        {
            return GenericResult<int>.Failure(EtlLog.LoadFailed(_logger, Name, problem));
        }

        var destination = DestinationTarget();
        EtlLog.LoadingToConnection(_logger, Name, destination.DataStore, destination.Container);

        if (_connectionProvider != null)
        {
            var connectionResult = await DestinationConnection(cancellationToken).ConfigureAwait(false);
            if (!connectionResult.IsSuccess)
            {
                return connectionResult.ToNewResult<int>();
            }

            if (connectionResult.Value is IHttpRecordWriterConnection httpWriter)
            {
                if (run.Plan.Keyed)
                {
                    return GenericResult<int>.Failure(PipelineRunLog.LoadModeNotForDestination(
                        _logger, Name, _configuration.LoadMode!, "an HTTP record writer sends rows and cannot match them on keys"));
                }

                return await LoadToHttpRecordWriter(httpWriter, destination, records, result, cancellationToken)
                    .ConfigureAwait(false);
            }
        }

        // Insert or upsert path: SQL/tabular connections, or when connection provider is not wired.
        return await LoadBatches(
            records, run, result, (command, token) => gateway.Execute<int>(command, destination, token), cancellationToken).ConfigureAwait(false);
    }

    // HTTP record writer path: serializes rows through the container's configured format and POSTs/PUTs
    // them to the endpoint declared in the HttpRecordWriterCapability fields.
    private async Task<IGenericResult<int>> LoadToHttpRecordWriter(
        IHttpRecordWriterConnection httpWriter,
        DataStoreTarget destination,
        List<IDictionary<string, object?>> records,
        EtlPipelineExecutionResult result,
        CancellationToken cancellationToken)
    {
        if (_dataStoreProvider == null)
        {
            return GenericResult<int>.Failure(
                EtlLog.LoadFailed(_logger, Name,
                    "IDataStoreProvider is required for HTTP record writer load path"));
        }

        var storeResult = await _dataStoreProvider.Get(destination.DataStore, cancellationToken).ConfigureAwait(false);
        if (!storeResult.IsSuccess)
        {
            return storeResult.ToNewResult<int>();
        }

        IDataContainer? container = null;
        foreach (var path in storeResult.Value!.Paths)
        {
            var containerResult = path.Container(destination.Container);
            if (containerResult.IsSuccess)
            {
                container = containerResult.Value;
                break;
            }
        }

        if (container == null)
        {
            return GenericResult<int>.Failure(
                EtlLog.LoadFailed(_logger, Name,
                    $"Container '{destination.Container}' not found in data store '{destination.DataStore}'"));
        }

        EtlLog.LoadingViaHttpRecordWriter(_logger, Name, records.Count, destination.DataStore);

        IReadOnlyList<IReadOnlyDictionary<string, object?>> rows =
            records.Select(r => (IReadOnlyDictionary<string, object?>)
                new System.Collections.ObjectModel.ReadOnlyDictionary<string, object?>(r))
            .ToList();

        var writeResult = await httpWriter.WriteRecords(container, rows, cancellationToken).ConfigureAwait(false);
        if (!writeResult.IsSuccess)
        {
            return GenericResult<int>.Failure(
                EtlLog.LoadViaHttpRecordWriterFailed(
                    _logger, Name, destination.DataStore,
                    GetFirstMessageText(writeResult.Messages) ?? "HTTP write failed"));
        }

        EtlLog.LoadViaHttpRecordWriterCompleted(_logger, Name, writeResult.Value, destination.DataStore);
        return GenericResult<int>.Success(writeResult.Value);
    }

    // The command that loads one batch of the pipeline's own load: an insert for Append and Replace, an upsert for Upsert,
    // built by the builder of that kind from the load's options and value sources.
    private IGenericResult<IDataCommand> OwnLoadCommand(RunState run, List<IDictionary<string, object?>> batch)
    {
        var unit = run.Plan.OwnLoad!;
        var built = LoadStepCommandBuilders.ByName(unit.Kind.Name)
            .Build(new LoadCommandInput(unit.Owner, unit.Row, batch, null, run.StartedAt, _logger));
        if (!built.IsSuccess)
        {
            return built.ToNewResult<IDataCommand>();
        }

        return built.Value!.Count == 1
            ? GenericResult<IDataCommand>.Success(built.Value[0])
            : GenericResult<IDataCommand>.Failure(PipelineRunLog.LoadOptionsRefused(
                _logger, unit.Owner, unit.Kind.Name, "the load builds more than one command, which only the unit of work can run"));
    }

    // Load steps, and an own load that flags the keys absent from the source, run through the unit of work on the whole
    // output: one transaction per connection, rolled back when a step fails.
    private async Task<IGenericResult> LoadThroughUnitOfWork(
        List<IDictionary<string, object?>> records,
        RunState run,
        EtlPipelineExecutionResult result,
        CancellationToken cancellationToken)
    {
        IReadOnlyList<LoadUnit> units = run.Plan.HasSteps ? run.Plan.Steps : [run.Plan.OwnLoad!];
        var executor = NewLoadExecutor();
        var loaded = await executor.Run(Name, units, records, run.Parameters.Values, run.StartedAt, cancellationToken).ConfigureAwait(false);
        if (!loaded.IsSuccess)
        {
            return GenericResult.Failure(EtlLog.LoadFailed(_logger, Name, LoadFailureText.Of(loaded)));
        }

        result.RecordsLoaded += loaded.Value;
        return GenericResult.Success();
    }

    private PipelineLoadExecutor NewLoadExecutor() => new(
        new LoadTargetResolver(_dataSets, _dataStoreProvider),
        _connectionProvider,
        _connectionTypes,
        _logger);

    // Reject steps write the records the stages rejected, after the main load committed and in a unit of work of their own
    // (FDW-1142). A failure fails the run, so its watermark does not move; the main load stays committed, as one connection's
    // commit cannot be undone by another's failure.
    private async Task<IGenericResult> RunRejectSteps(
        PipelineExecutionOptions options,
        RunState run,
        CancellationToken cancellationToken)
    {
        if (run.Plan.RejectSteps.Count == 0 || SkipsDestinationWrites(options))
        {
            return GenericResult.Success();
        }

        if (run.TransformContext.Rejections.Count == 0)
        {
            PipelineRunLog.RejectStepsSkippedNoRejections(_logger, Name);
            return GenericResult.Success();
        }

        var rows = RejectedRows.Of(Name, run.TransformContext.Rejections, _logger);
        if (!rows.IsSuccess)
        {
            return rows;
        }

        var written = await NewLoadExecutor()
            .Run(Name, run.Plan.RejectSteps, rows.Value!, run.Parameters.Values, run.StartedAt, cancellationToken)
            .ConfigureAwait(false);
        if (!written.IsSuccess)
        {
            return GenericResult.Failure(EtlLog.LoadFailed(_logger, Name, LoadFailureText.Of(written)));
        }

        PipelineRunLog.RejectedRecordsWritten(_logger, Name, run.TransformContext.Rejections.Count);
        return GenericResult.Success();
    }

    // A successful run moves the pipeline's watermark to the moment it began, after its load committed and never before.
    // A run that recorded errors (continue-on-error) leaves it where it was, so what failed is read again next time.
    private async Task<IGenericResult> RecordWatermark(
        IDataGateway gateway,
        PipelineExecutionOptions options,
        RunState run,
        EtlPipelineExecutionResult result,
        CancellationToken cancellationToken)
    {
        if (SkipsDestinationWrites(options) || options.TenantId is not { } tenantId)
        {
            return GenericResult.Success();
        }

        if (!result.IsSuccess)
        {
            PipelineRunLog.WatermarkHeldForErrors(_logger, Name);
            return GenericResult.Success();
        }

        return await PipelineWatermarks.Record(
            gateway, EtlPipelineTypes.OperationalConnection, run.Parameters.Watermark, _configuration.Id, tenantId,
            run.StartedAt, result.ExecutionId, Name, _logger, cancellationToken).ConfigureAwait(false);
    }

    private static string? GetFirstMessageText(IEnumerable<IGenericMessage> messages)
    {
        using var enumerator = messages.GetEnumerator();
        return enumerator.MoveNext() ? enumerator.Current.Message : null;
    }

    /// <inheritdoc />
    public override IGenericResult Validate()
    {
        var messages = new List<IGenericMessage>();

        if (string.IsNullOrWhiteSpace(_configuration.Name))
        {
            messages.Add(EtlLog.PipelineCreationFailed(_logger, "unknown", "Pipeline name is required"));
        }

        if (_configuration.MaxParallelism < 1)
        {
            messages.Add(EtlLog.PipelineCreationFailed(_logger, Name, "MaxParallelism must be at least 1"));
        }

        ValidateSourceKind(messages);
        ValidateDestinationKind(messages);
        ValidateLoadDefinition(messages);

        if (messages.Count > 0)
        {
            return GenericResult.Failure(messages);
        }

        return GenericResult.Success();
    }

    private void ValidateLoadDefinition(List<IGenericMessage> messages)
    {
        var plan = PipelineLoadPlan.Create(_configuration, _logger);
        if (!plan.IsSuccess)
        {
            messages.AddRange(plan.Messages);
        }
    }

    private void ValidateSourceKind(List<IGenericMessage> messages)
    {
        if (_configuration.SourceKind == null || _configuration.SourceKind == DataSourceKinds.NotFound)
        {
            messages.Add(EtlLog.SourceKindRequired(_logger, Name));
            return;
        }

        if (string.Equals(_configuration.SourceKind.Name, "DataSet", StringComparison.Ordinal))
        {
            if (string.IsNullOrWhiteSpace(_configuration.SourceDataSet))
            {
                messages.Add(EtlLog.PipelineCreationFailed(_logger, Name, "SourceDataSet is required when SourceKind is DataSet"));
            }
        }
        else if (string.Equals(_configuration.SourceKind.Name, "DataStore", StringComparison.Ordinal))
        {
            if (string.IsNullOrWhiteSpace(_configuration.SourceDataStorePath))
            {
                messages.Add(EtlLog.PipelineCreationFailed(_logger, Name, "SourceDataStorePath is required when SourceKind is DataStore"));
            }

            if (string.IsNullOrWhiteSpace(_configuration.SourceContainerName))
            {
                messages.Add(EtlLog.PipelineCreationFailed(_logger, Name, "SourceContainerName is required when SourceKind is DataStore"));
            }
        }
    }

    // A DataStore destination writes into one container of a configured data store through the data gateway.
    private bool DestinationIsDataStore =>
        string.Equals(_configuration.DestinationKind?.Name, "DataStore", StringComparison.Ordinal);

    // What a data store destination still lacks, or null when its container is fully named: the store, the path and the container
    // (FDW-1269). Call only for a DataStore destination.
    private string? DestinationProblem() =>
        string.IsNullOrWhiteSpace(_configuration.DestinationDataStoreName)
            ? "DestinationDataStoreName is required when DestinationKind is DataStore"
            : string.IsNullOrWhiteSpace(_configuration.DestinationDataStorePath)
                ? "DestinationDataStorePath is required when DestinationKind is DataStore"
                : string.IsNullOrWhiteSpace(_configuration.DestinationContainerName)
                    ? "DestinationContainerName is required when DestinationKind is DataStore"
                    : null;

    // The container the destination writes to. Call only after DestinationProblem() returned null.
    private DataStoreTarget DestinationTarget() =>
        new(_configuration.DestinationDataStoreName!, _configuration.DestinationDataStorePath, _configuration.DestinationContainerName!);

    // The connection behind the destination data store, which decides how rows are written (an HTTP record writer sends them,
    // anything else takes a bulk insert). It is found through the store's ConnectionId, so no caller names a connection above the
    // connection layer.
    private async Task<IGenericResult<IGenericConnection>> DestinationConnection(CancellationToken cancellationToken)
    {
        if (_dataStoreProvider == null)
        {
            return GenericResult<IGenericConnection>.Failure(
                EtlLog.LoadFailed(_logger, Name, "IDataStoreProvider is required to resolve a data store destination's connection"));
        }

        var store = await _dataStoreProvider.Get(_configuration.DestinationDataStoreName!, cancellationToken).ConfigureAwait(false);
        return store.IsSuccess
            ? await _connectionProvider!.Get(store.Value!.ConnectionId, cancellationToken).ConfigureAwait(false)
            : store.ToNewResult<IGenericConnection>();
    }

    private void ValidateDestinationKind(List<IGenericMessage> messages)
    {
        if (_configuration.DestinationKind == null || _configuration.DestinationKind == DataDestinationKinds.NotFound)
        {
            messages.Add(EtlLog.DestinationKindRequired(_logger, Name));
            return;
        }

        if (string.Equals(_configuration.DestinationKind.Name, "DataSet", StringComparison.Ordinal))
        {
            if (string.IsNullOrWhiteSpace(_configuration.DestinationDataSet))
            {
                messages.Add(EtlLog.PipelineCreationFailed(_logger, Name, "DestinationDataSet is required when DestinationKind is DataSet"));
            }
        }
        else if (DestinationIsDataStore)
        {
            if (DestinationProblem() is { } problem)
            {
                messages.Add(EtlLog.PipelineCreationFailed(_logger, Name, problem));
            }
        }
    }
}
