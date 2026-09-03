using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Fdw.Commands.Data;
using Fdw.Data;
using Fdw.Data.Abstractions;
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
using Fdw.Services.Etl.Logging;
using Fdw.Services.Etl.Transforms;
using Fdw.Services.Pipelines.Abstractions.DataDestination;
using Fdw.Services.Pipelines.Abstractions.DataSource;
using Fdw.Services.Pipelines.Notifications;

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
    public BatchCopyPipeline(
        BatchCopyPipelineConfiguration configuration,
        ILogger<BatchCopyPipeline> logger,
        IDataGatewayProvider? dataGateways = null,
        object? calculationEngine = null,
        IConnectionProvider? connectionProvider = null,
        IDataStoreProvider? dataStoreProvider = null,
        IPipelineStatusBroadcaster? broadcaster = null)
        : base(logger)
    {
        _configuration = configuration;
        _logger = logger;
        _dataGateways = dataGateways;
        _calculationEngine = calculationEngine;
        _connectionProvider = connectionProvider;
        _dataStoreProvider = dataStoreProvider;
        _broadcaster = broadcaster;
    }

    /// <inheritdoc />
    public override Guid Id => _configuration.Id;

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
            LookupTransformType.ClearCache();
        }
    }

    private async Task<IGenericResult<IEtlPipelineExecutionResult>> ExecutePhases(
        IDataGateway gateway,
        PipelineExecutionOptions options,
        int effectiveBatchSize,
        EtlPipelineExecutionResult result,
        CancellationToken cancellationToken)
    {
        var extractResult = await ExtractRecords(gateway, effectiveBatchSize, result, cancellationToken).ConfigureAwait(false);
        if (!extractResult.IsSuccess)
        {
            result.Complete();
            return extractResult.ToNewResult<IEtlPipelineExecutionResult>();
        }

        var records = extractResult.Value!;

        var transformResult = await TransformRecords(gateway, records, result, cancellationToken).ConfigureAwait(false);
        if (!transformResult.IsSuccess)
        {
            result.Complete();
            return transformResult.ToNewResult<IEtlPipelineExecutionResult>();
        }

        var transformedRecords = transformResult.Value!;

        if (options.IsTestMode && options.SkipDestinationWrites)
        {
            EtlLog.TestModeWriteSkipped(_logger, Name, transformedRecords.Count);
            result.RecordsLoaded = transformedRecords.Count;
            result.Complete();
            return GenericResult<IEtlPipelineExecutionResult>.Success(result);
        }

        var loadResult = await LoadRecords(gateway, transformedRecords, result, cancellationToken).ConfigureAwait(false);
        if (!loadResult.IsSuccess)
        {
            result.Complete();
            return loadResult.ToNewResult<IEtlPipelineExecutionResult>();
        }

        result.Complete();
        return GenericResult<IEtlPipelineExecutionResult>.Success(result);
    }

    private async Task<IGenericResult<List<IDictionary<string, object?>>>> ExtractRecords(
        IDataGateway gateway,
        int effectiveBatchSize,
        EtlPipelineExecutionResult result,
        CancellationToken cancellationToken)
    {
        EtlLog.ExtractStarted(_logger, Name);
        var extractStopwatch = Stopwatch.StartNew();

        cancellationToken.ThrowIfCancellationRequested();

        if (_configuration.SourceKind == null || _configuration.SourceKind == DataSourceKinds.NotFound)
        {
            return GenericResult<List<IDictionary<string, object?>>>.Failure(
                EtlLog.SourceKindRequired(_logger, Name));
        }

        try
        {
            var allRecords = new List<IDictionary<string, object?>>();
            var skip = 0;
            var hasMore = true;

            while (hasMore)
            {
                cancellationToken.ThrowIfCancellationRequested();

                IGenericResult<List<IDictionary<string, object?>>> batchResult;

                if (string.Equals(_configuration.SourceKind.Name, "DataSet", StringComparison.Ordinal))
                {
                    // ELT path: resolve via dataset dispatch (carries store→path→container + RecordSelector).
                    batchResult = await ExtractBatchFromDataSet(gateway, skip, effectiveBatchSize, cancellationToken).ConfigureAwait(false);
                }
                else if (string.Equals(_configuration.SourceKind.Name, "Connection", StringComparison.Ordinal))
                {
                    // ETL path: read directly from the physical connection + container. B3 HTTP branch
                    // will add its own logic inside ExtractBatchFromConnection when the HTTP connection
                    // type is detected. This method is the single extension point for connection-based reads.
                    batchResult = await ExtractBatchFromConnection(gateway, skip, effectiveBatchSize, cancellationToken).ConfigureAwait(false);
                }
                else
                {
                    return GenericResult<List<IDictionary<string, object?>>>.Failure(
                        EtlLog.UnknownSourceKind(_logger, Name, _configuration.SourceKind.Name));
                }

                if (!batchResult.IsSuccess)
                {
                    return batchResult;
                }

                var batch = batchResult.Value!;
                if (batch.Count == 0)
                {
                    hasMore = false;
                }
                else
                {
                    allRecords.AddRange(batch);
                    skip += batch.Count;
                    hasMore = batch.Count == effectiveBatchSize;
                }
            }

            extractStopwatch.Stop();
            result.RecordsExtracted = allRecords.Count;
            result.ExtractDuration = extractStopwatch.Elapsed;
            EtlLog.ExtractCompleted(_logger, allRecords.Count, extractStopwatch.Elapsed.TotalMilliseconds);

            return GenericResult<List<IDictionary<string, object?>>>.Success(allRecords);
        }
        catch (Exception ex)
        {
            extractStopwatch.Stop();
            return GenericResult<List<IDictionary<string, object?>>>.Failure(
                EtlLog.ExtractFailed(_logger, ex, Name));
        }
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

    // ETL path: source is a physical connection + explicit container path.
    // Extension point for B3: HTTP-specific branch logic belongs inside this method
    // once the HTTP connection type is available (connection-type detection stays below the
    // connection layer — no switch on connection type here; the gateway routes accordingly).
    private async Task<IGenericResult<List<IDictionary<string, object?>>>> ExtractBatchFromConnection(
        IDataGateway gateway,
        int skip,
        int effectiveBatchSize,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(_configuration.SourceConnectionName))
        {
            return GenericResult<List<IDictionary<string, object?>>>.Failure(
                EtlLog.ExtractFailed(_logger, Name, "SourceConnectionName is required when SourceKind is Connection"));
        }

        if (string.IsNullOrWhiteSpace(_configuration.SourceContainerPath))
        {
            return GenericResult<List<IDictionary<string, object?>>>.Failure(
                EtlLog.ExtractFailed(_logger, Name, "SourceContainerPath is required when SourceKind is Connection"));
        }

        EtlLog.ExtractingFromConnection(_logger, Name, _configuration.SourceConnectionName, _configuration.SourceContainerPath!);

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
            new DataStoreTarget(_configuration.SourceConnectionName, null, _configuration.SourceContainerPath!),
            cancellationToken).ConfigureAwait(false);

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
        IDataGateway gateway,
        List<IDictionary<string, object?>> records,
        EtlPipelineExecutionResult result,
        CancellationToken cancellationToken)
    {
        var transformCount = _configuration.Transforms?.Count ?? 0;
        EtlLog.TransformStarted(_logger, transformCount);
        var transformStopwatch = Stopwatch.StartNew();

        cancellationToken.ThrowIfCancellationRequested();

        try
        {
            if (_configuration.Transforms == null || _configuration.Transforms.Count == 0)
            {
                transformStopwatch.Stop();
                result.RecordsTransformed = records.Count;
                result.TransformDuration = transformStopwatch.Elapsed;
                EtlLog.TransformCompleted(_logger, records.Count, transformStopwatch.Elapsed.TotalMilliseconds);
                return GenericResult<List<IDictionary<string, object?>>>.Success(records);
            }

            var orderedTransforms = _configuration.Transforms
                .Where(t => t.IsEnabled)
                .OrderBy(t => t.ExecutionOrder)
                .ToList();

            var transformContext = new TransformContext(
                executionId: result.ExecutionId,
                logger: _logger,
                variables: new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase),
                calculationEngine: _calculationEngine,
                connectionProvider: _connectionProvider,
                dataGateway: gateway);

            var loopResult = await TransformStepFold(
                records, orderedTransforms, transformContext, result, cancellationToken).ConfigureAwait(false);

            transformStopwatch.Stop();
            if (!loopResult.IsSuccess)
            {
                return loopResult;
            }

            result.TransformDuration = transformStopwatch.Elapsed;
            EtlLog.TransformCompleted(_logger, result.RecordsTransformed, transformStopwatch.Elapsed.TotalMilliseconds);

            return loopResult;
        }
        catch (Exception ex)
        {
            transformStopwatch.Stop();
            return GenericResult<List<IDictionary<string, object?>>>.Failure(
                EtlLog.TransformFailed(_logger, ex, Name));
        }
    }

    private async Task<IGenericResult<List<IDictionary<string, object?>>>> TransformStepFold(
        List<IDictionary<string, object?>> records,
        List<PipelineTransformConfiguration> orderedTransforms,
        TransformContext transformContext,
        EtlPipelineExecutionResult result,
        CancellationToken cancellationToken)
    {
        var currentRecords = records;
        var stepErrorCount = 0;

        foreach (var step in orderedTransforms)
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

            var stepResult = await transformType.TransformBatch(currentRecords, step, transformContext, cancellationToken).ConfigureAwait(false);

            if (!stepResult.IsSuccess)
            {
                if (!_configuration.ContinueOnError)
                {
                    return stepResult.ToNewResult<List<IDictionary<string, object?>>>();
                }

                stepErrorCount++;
            }
            else
            {
                currentRecords = stepResult.Value?.ToList() ?? [];
            }

            EtlLog.TransformStepCompleted(_logger, step.Name, currentRecords.Count, inCount);

            if (stepErrorCount + transformContext.Errors.Count >= _configuration.MaxErrors)
            {
                return GenericResult<List<IDictionary<string, object?>>>.Failure(
                    EtlLog.ExecutionFailed(_logger, Name, result.ExecutionId, $"Maximum error count ({_configuration.MaxErrors}) exceeded"));
            }
        }

        result.RecordsTransformed = currentRecords.Count;
        result.RecordsFailed = stepErrorCount + transformContext.Errors.Count;

        return GenericResult<List<IDictionary<string, object?>>>.Success(currentRecords);
    }

    private async Task<IGenericResult> LoadRecords(
        IDataGateway gateway,
        List<IDictionary<string, object?>> records,
        EtlPipelineExecutionResult result,
        CancellationToken cancellationToken)
    {
        EtlLog.LoadStarted(_logger, Name);
        var loadStopwatch = Stopwatch.StartNew();

        cancellationToken.ThrowIfCancellationRequested();

        if (_configuration.DestinationKind == null || _configuration.DestinationKind == DataDestinationKinds.NotFound)
        {
            loadStopwatch.Stop();
            return GenericResult.Failure(EtlLog.DestinationKindRequired(_logger, Name));
        }

        try
        {
            if (records.Count == 0)
            {
                loadStopwatch.Stop();
                result.RecordsLoaded = 0;
                result.LoadDuration = loadStopwatch.Elapsed;
                EtlLog.LoadCompleted(_logger, 0, loadStopwatch.Elapsed.TotalMilliseconds);
                return GenericResult.Success();
            }

            if (string.Equals(_configuration.DestinationKind.Name, "DataSet", StringComparison.Ordinal))
            {
                // ELT path: write via DataSet dispatch (carries store→path→container address).
                if (_configuration.TruncateBeforeLoad)
                {
                    var truncateResult = await TruncateDataSet(gateway, cancellationToken).ConfigureAwait(false);
                    if (!truncateResult.IsSuccess)
                    {
                        loadStopwatch.Stop();
                        return truncateResult;
                    }
                }

                var totalLoaded = await LoadToDataSet(gateway, records, result, cancellationToken).ConfigureAwait(false);
                if (!totalLoaded.IsSuccess)
                {
                    loadStopwatch.Stop();
                    return totalLoaded;
                }

                loadStopwatch.Stop();
                result.RecordsLoaded = totalLoaded.Value;
                result.LoadDuration = loadStopwatch.Elapsed;
                EtlLog.LoadCompleted(_logger, totalLoaded.Value, loadStopwatch.Elapsed.TotalMilliseconds);
                return GenericResult.Success();
            }
            else if (string.Equals(_configuration.DestinationKind.Name, "Connection", StringComparison.Ordinal))
            {
                // ETL path: write directly to the physical connection + container.
                if (_configuration.TruncateBeforeLoad)
                {
                    var truncateResult = await TruncateConnection(gateway, cancellationToken).ConfigureAwait(false);
                    if (!truncateResult.IsSuccess)
                    {
                        loadStopwatch.Stop();
                        return truncateResult;
                    }
                }

                var totalLoaded = await LoadToConnection(gateway, records, result, cancellationToken).ConfigureAwait(false);
                if (!totalLoaded.IsSuccess)
                {
                    loadStopwatch.Stop();
                    return totalLoaded;
                }

                loadStopwatch.Stop();
                result.RecordsLoaded = totalLoaded.Value;
                result.LoadDuration = loadStopwatch.Elapsed;
                EtlLog.LoadCompleted(_logger, totalLoaded.Value, loadStopwatch.Elapsed.TotalMilliseconds);
                return GenericResult.Success();
            }
            else
            {
                loadStopwatch.Stop();
                return GenericResult.Failure(EtlLog.UnknownDestinationKind(_logger, Name, _configuration.DestinationKind.Name));
            }
        }
        catch (Exception ex)
        {
            loadStopwatch.Stop();
            return GenericResult.Failure(
                EtlLog.LoadFailed(_logger, ex, Name));
        }
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

    // ELT path: bulk insert via DataSet dispatch.
    private async Task<IGenericResult<int>> LoadToDataSet(
        IDataGateway gateway,
        List<IDictionary<string, object?>> records,
        EtlPipelineExecutionResult result,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(_configuration.DestinationDataSet))
        {
            return GenericResult<int>.Failure(
                EtlLog.LoadFailed(_logger, Name, "DestinationDataSet is required when DestinationKind is DataSet"));
        }

        var totalLoaded = 0;
        var batches = records
            .Select((record, index) => new { record, index })
            .GroupBy(x => x.index / _configuration.BatchSize)
            .Select(g => g.Select(x => x.record).ToList())
            .ToList();

        foreach (var batch in batches)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var insertResult = await gateway.Execute<int>(
                new BulkInsertCommand<IDictionary<string, object?>>(batch),
                new DataSetTarget(_configuration.DestinationDataSet),
                cancellationToken).ConfigureAwait(false);

            if (!insertResult.IsSuccess)
            {
                if (!_configuration.ContinueOnError)
                {
                    return GenericResult<int>.Failure(
                        EtlLog.LoadFailed(_logger, Name, GetFirstMessageText(insertResult.Messages) ?? "Unknown error"));
                }

                result.AddError($"Batch load failed: {GetFirstMessageText(insertResult.Messages)}");
            }
            else
            {
                totalLoaded += insertResult.Value;
            }
        }

        return GenericResult<int>.Success(totalLoaded);
    }

    // ETL path: truncate directly on the physical connection + container.
    private async Task<IGenericResult> TruncateConnection(
        IDataGateway gateway,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(_configuration.DestinationConnectionName))
        {
            return GenericResult.Failure(
                EtlLog.LoadFailed(_logger, Name, "DestinationConnectionName is required when DestinationKind is Connection"));
        }

        if (string.IsNullOrWhiteSpace(_configuration.DestinationContainerPath))
        {
            return GenericResult.Failure(
                EtlLog.LoadFailed(_logger, Name, "DestinationContainerPath is required when DestinationKind is Connection"));
        }

        EtlLog.LoadingToConnection(_logger, Name, _configuration.DestinationConnectionName, _configuration.DestinationContainerPath!);

        var truncateResult = await gateway.Execute<int>(
            new TruncateCommand(),
            new DataStoreTarget(_configuration.DestinationConnectionName, null, _configuration.DestinationContainerPath!),
            cancellationToken).ConfigureAwait(false);

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
    private async Task<IGenericResult<int>> LoadToConnection(
        IDataGateway gateway,
        List<IDictionary<string, object?>> records,
        EtlPipelineExecutionResult result,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(_configuration.DestinationConnectionName))
        {
            return GenericResult<int>.Failure(
                EtlLog.LoadFailed(_logger, Name, "DestinationConnectionName is required when DestinationKind is Connection"));
        }

        if (string.IsNullOrWhiteSpace(_configuration.DestinationContainerPath))
        {
            return GenericResult<int>.Failure(
                EtlLog.LoadFailed(_logger, Name, "DestinationContainerPath is required when DestinationKind is Connection"));
        }

        EtlLog.LoadingToConnection(_logger, Name, _configuration.DestinationConnectionName, _configuration.DestinationContainerPath!);

        if (_connectionProvider != null)
        {
            var connectionResult = await _connectionProvider.Get(
                _configuration.DestinationConnectionName, cancellationToken).ConfigureAwait(false);
            if (!connectionResult.IsSuccess)
            {
                return connectionResult.ToNewResult<int>();
            }

            if (connectionResult.Value is IHttpRecordWriterConnection httpWriter)
            {
                return await LoadToHttpRecordWriter(httpWriter, records, result, cancellationToken)
                    .ConfigureAwait(false);
            }
        }

        // BulkInsert path: SQL/tabular connections, or when connection provider is not wired.
        var totalLoaded = 0;
        var batches = records
            .Select((record, index) => new { record, index })
            .GroupBy(x => x.index / _configuration.BatchSize)
            .Select(g => g.Select(x => x.record).ToList())
            .ToList();

        foreach (var batch in batches)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var insertResult = await gateway.Execute<int>(
                new BulkInsertCommand<IDictionary<string, object?>>(batch),
                new DataStoreTarget(_configuration.DestinationConnectionName, null, _configuration.DestinationContainerPath!),
                cancellationToken).ConfigureAwait(false);

            if (!insertResult.IsSuccess)
            {
                if (!_configuration.ContinueOnError)
                {
                    return GenericResult<int>.Failure(
                        EtlLog.LoadFailed(_logger, Name, GetFirstMessageText(insertResult.Messages) ?? "Unknown error"));
                }

                result.AddError($"Batch load failed: {GetFirstMessageText(insertResult.Messages)}");
            }
            else
            {
                totalLoaded += insertResult.Value;
            }
        }

        return GenericResult<int>.Success(totalLoaded);
    }

    // HTTP record writer path: serializes rows through the container's configured format and POSTs/PUTs
    // them to the endpoint declared in the HttpRecordWriterCapability fields.
    private async Task<IGenericResult<int>> LoadToHttpRecordWriter(
        IHttpRecordWriterConnection httpWriter,
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

        var storeResult = await _dataStoreProvider.Get(
            _configuration.DestinationConnectionName!, cancellationToken).ConfigureAwait(false);
        if (!storeResult.IsSuccess)
        {
            return storeResult.ToNewResult<int>();
        }

        IDataContainer? container = null;
        foreach (var path in storeResult.Value!.Paths)
        {
            var containerResult = path.Container(_configuration.DestinationContainerPath!);
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
                    $"Container '{_configuration.DestinationContainerPath}' not found in data store '{_configuration.DestinationConnectionName}'"));
        }

        EtlLog.LoadingViaHttpRecordWriter(_logger, Name, records.Count, _configuration.DestinationConnectionName!);

        IReadOnlyList<IReadOnlyDictionary<string, object?>> rows =
            records.Select(r => (IReadOnlyDictionary<string, object?>)
                new System.Collections.ObjectModel.ReadOnlyDictionary<string, object?>(r))
            .ToList();

        var writeResult = await httpWriter.WriteRecords(container, rows, cancellationToken).ConfigureAwait(false);
        if (!writeResult.IsSuccess)
        {
            return GenericResult<int>.Failure(
                EtlLog.LoadViaHttpRecordWriterFailed(
                    _logger, Name, _configuration.DestinationConnectionName!,
                    GetFirstMessageText(writeResult.Messages) ?? "HTTP write failed"));
        }

        EtlLog.LoadViaHttpRecordWriterCompleted(_logger, Name, writeResult.Value, _configuration.DestinationConnectionName!);
        return GenericResult<int>.Success(writeResult.Value);
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

        if (messages.Count > 0)
        {
            return GenericResult.Failure(messages);
        }

        return GenericResult.Success();
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
        else if (string.Equals(_configuration.SourceKind.Name, "Connection", StringComparison.Ordinal))
        {
            if (string.IsNullOrWhiteSpace(_configuration.SourceConnectionName))
            {
                messages.Add(EtlLog.PipelineCreationFailed(_logger, Name, "SourceConnectionName is required when SourceKind is Connection"));
            }

            if (string.IsNullOrWhiteSpace(_configuration.SourceContainerPath))
            {
                messages.Add(EtlLog.PipelineCreationFailed(_logger, Name, "SourceContainerPath is required when SourceKind is Connection"));
            }
        }
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
        else if (string.Equals(_configuration.DestinationKind.Name, "Connection", StringComparison.Ordinal))
        {
            if (string.IsNullOrWhiteSpace(_configuration.DestinationConnectionName))
            {
                messages.Add(EtlLog.PipelineCreationFailed(_logger, Name, "DestinationConnectionName is required when DestinationKind is Connection"));
            }

            if (string.IsNullOrWhiteSpace(_configuration.DestinationContainerPath))
            {
                messages.Add(EtlLog.PipelineCreationFailed(_logger, Name, "DestinationContainerPath is required when DestinationKind is Connection"));
            }
        }
    }
}
