using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Fdw.Configuration;
using Fdw.Commands.Data;
using Fdw.Data;
using Fdw.Messages;
using Fdw.Results;
using Fdw.ServiceTypes;
using Fdw.Services.Connections.Abstractions;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Etl.Abstractions;
using Fdw.Services.Etl.Abstractions.Execution;
using Fdw.Services.Etl.Abstractions.OptionTypes;
using Fdw.Services.Etl.Logging;
using Fdw.Services.Etl.Transforms;
using Fdw.Services.Pipelines.Notifications;

namespace Fdw.Services.Etl.Pipelines;

/// <summary>
/// Streaming pipeline implementation that processes data in a continuous stream.
/// In test mode: caps extraction, skips destination writes, retains samples in the inspector,
/// and honors pause/resume/step signals from <see cref="IPipelineTestController"/>.
/// </summary>
public sealed class StreamingPipeline : EtlPipelineBase
{
    private readonly StreamingPipelineConfiguration _configuration;
    private readonly ILogger<StreamingPipeline> _logger;
    private readonly IDataGateway? _dataGateway;
    private readonly object? _calculationEngine;
    private readonly IPlatformServiceProvider<IGenericConnection, IGenericConfiguration>? _connectionProvider;
    private readonly IPipelineTestController? _testController;
    private readonly IPipelineExecutionInspector? _inspector;
    private readonly IPipelineStatusBroadcaster? _broadcaster;
    private int _isExecuting;

    /// <summary>
    /// Initializes a new instance of the <see cref="StreamingPipeline"/> class.
    /// </summary>
    public StreamingPipeline(
        StreamingPipelineConfiguration configuration,
        ILogger<StreamingPipeline> logger,
        IDataGateway? dataGateway = null,
        object? calculationEngine = null,
        IPlatformServiceProvider<IGenericConnection, IGenericConfiguration>? connectionProvider = null,
        IPipelineTestController? testController = null,
        IPipelineExecutionInspector? inspector = null,
        IPipelineStatusBroadcaster? broadcaster = null)
        : base(logger)
    {
        _configuration = configuration;
        _logger = logger;
        _dataGateway = dataGateway;
        _calculationEngine = calculationEngine;
        _connectionProvider = connectionProvider;
        _testController = testController;
        _inspector = inspector;
        _broadcaster = broadcaster;
    }

    /// <inheritdoc />
    public override Guid Id => _configuration.Id;

    /// <inheritdoc />
    public override string Name => _configuration.Name;

    /// <inheritdoc />
    // Why: the engine discriminator is intrinsic to this pipeline class (a StreamingPipeline IS "Streaming"),
    // not read from the engine body's ServiceOptionType (which can be null on a leaf typed body).
    public override string PipelineType => "Streaming";

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
            return await ExecuteCore(executionId, options, result, stopwatch, cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException oce)
        {
            result.Complete();
            EtlLog.ExecutionCompleted(_logger, Name, executionId, result.RecordsLoaded, stopwatch.Elapsed.TotalMilliseconds);
            return GenericResult<IEtlPipelineExecutionResult>.Failure(
                EtlLog.ExecutionException(_logger, oce, Name, executionId));
        }
        catch (Exception ex)
        {
            result.AddError(ex.Message);
            result.Complete();
            return GenericResult<IEtlPipelineExecutionResult>.Failure(
                EtlLog.ExecutionException(_logger, ex, Name, executionId));
        }
        finally
        {
            Interlocked.Exchange(ref _isExecuting, 0);
            LookupTransformType.ClearCache();
        }
    }

    private async Task<IGenericResult<IEtlPipelineExecutionResult>> ExecuteCore(
        Guid executionId,
        PipelineExecutionOptions options,
        EtlPipelineExecutionResult result,
        Stopwatch stopwatch,
        CancellationToken cancellationToken)
    {
        EtlLog.ExecutionStarted(_logger, Name, executionId);

        if (_dataGateway == null)
        {
            return GenericResult<IEtlPipelineExecutionResult>.Failure(
                EtlLog.ExecutionFailed(_logger, Name, executionId, "DataGateway is required for pipeline execution"));
        }

        var dataGateway = _dataGateway;
        var transformContext = new TransformContext(
            executionId: executionId,
            logger: _logger,
            variables: new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase),
            calculationEngine: _calculationEngine,
            connectionProvider: _connectionProvider,
            dataGateway: dataGateway);

        // Why: Get the test controller state if this is a test execution so the batch loop
        // can await the pause event between batches.
        PipelineTestExecutionState? testState = null;
        if (options.IsTestMode && _testController != null)
        {
            testState = _testController.GetState(executionId);
        }

        // Register the execution with the inspector if in test mode.
        if (options.IsTestMode)
        {
            _inspector?.RegisterExecution(executionId, options);
        }

        // Determine effective extraction cap.
        var effectiveTake = options.IsTestMode
            ? Math.Min(_configuration.BufferSize, options.MaxRowsPerSource)
            : _configuration.BufferSize;

        var counters = await ProcessStream(
            executionId, options, effectiveTake, testState,
            dataGateway, transformContext, stopwatch, result, cancellationToken).ConfigureAwait(false);

        if (!counters.IsSuccess)
        {
            result.Complete();
            if (options.IsTestMode) _inspector?.UnregisterExecution(executionId);
            return counters.ToNewResult<IEtlPipelineExecutionResult>();
        }

        result.Complete();
        stopwatch.Stop();
        if (options.IsTestMode) _inspector?.UnregisterExecution(executionId);

        EtlLog.ExecutionCompleted(_logger, Name, executionId, result.RecordsLoaded, stopwatch.Elapsed.TotalMilliseconds);
        EtlLog.ExecutionMetrics(_logger, Name, result.RecordsExtracted, result.RecordsTransformed, result.RecordsLoaded, result.RecordsFailed);

        return GenericResult<IEtlPipelineExecutionResult>.Success(result);
    }

    private async Task<IGenericResult> ProcessStream(
        Guid executionId,
        PipelineExecutionOptions options,
        int effectiveTake,
        PipelineTestExecutionState? testState,
        IDataGateway dataGateway,
        TransformContext transformContext,
        Stopwatch stopwatch,
        EtlPipelineExecutionResult result,
        CancellationToken cancellationToken)
    {
        var totalExtracted = 0;
        var totalTransformed = 0;
        var totalLoaded = 0;
        var totalFailed = 0;
        var buffer = new List<IDictionary<string, object?>>(effectiveTake);
        var windowStart = DateTime.UtcNow;

        // Why: In test mode, a single pass through the source is sufficient (bounded by
        // MaxRowsPerSource). Production mode loops until cancelled.
        var runOnce = options.IsTestMode;

        while (!cancellationToken.IsCancellationRequested)
        {
            await ApplyPauseGate(testState, cancellationToken).ConfigureAwait(false);
            cancellationToken.ThrowIfCancellationRequested();

            var iterationStopwatch = Stopwatch.StartNew();

            var extractResult = await ExtractAndBuffer(effectiveTake, dataGateway, buffer, cancellationToken).ConfigureAwait(false);
            if (!extractResult.IsSuccess) return extractResult;

            totalExtracted += extractResult.Value.Extracted;
            totalFailed += extractResult.Value.Failed;
            NotifyInspectorExtracted(executionId, options, extractResult.Value.Extracted, buffer);

            if (ShouldFlushBuffer(buffer.Count, windowStart) && buffer.Count > 0)
            {
                var flushResult = await RunFlushCycle(
                    executionId, options, dataGateway, buffer, transformContext, cancellationToken).ConfigureAwait(false);

                if (!flushResult.IsSuccess) return flushResult;

                totalTransformed += flushResult.Value.Transformed;
                totalLoaded += flushResult.Value.Loaded;
                totalFailed += flushResult.Value.Failed;

                await NotifyBroadcasterAfterFlush(
                    executionId, totalExtracted, totalLoaded).ConfigureAwait(false);

                if (flushResult.Value.SkipIteration) continue;
                windowStart = DateTime.UtcNow;
            }

            ApplyStepRepause(testState);
            await ApplyRateLimiting(totalLoaded, stopwatch, cancellationToken).ConfigureAwait(false);
            await WaitForFlushInterval(iterationStopwatch, cancellationToken).ConfigureAwait(false);

            if (runOnce) break;
        }

        result.RecordsExtracted = totalExtracted;
        result.RecordsTransformed = totalTransformed;
        result.RecordsLoaded = totalLoaded;
        result.RecordsFailed = totalFailed;

        return GenericResult.Success();
    }

    private static async Task ApplyPauseGate(PipelineTestExecutionState? testState, CancellationToken cancellationToken)
    {
        if (testState == null) return;
        // Why: async-safe wait — Task.Run wraps the synchronous WaitHandle.Wait so
        // the ASP.NET thread pool is not blocked during the pause.
        await Task.Run(
            () => testState.PauseEvent.Wait(cancellationToken),
            cancellationToken).ConfigureAwait(false);
    }

    private void NotifyInspectorExtracted(
        Guid executionId,
        PipelineExecutionOptions options,
        int extractedCount,
        IEnumerable<IDictionary<string, object?>> buffer)
    {
        if (!options.IsTestMode || _inspector == null) return;
        _inspector.RecordTaskIn(executionId, _configuration.Id, extractedCount);
        _inspector.AddTaskSamples(executionId, _configuration.Id, buffer);
    }

    // Why: Returns a value tuple (Transformed, Loaded, Failed, SkipIteration, failure messages)
    // instead of ref parameters because async methods cannot take ref parameters in C#.
    private async Task<IGenericResult<(int Transformed, int Loaded, int Failed, bool SkipIteration)>> RunFlushCycle(
        Guid executionId,
        PipelineExecutionOptions options,
        IDataGateway dataGateway,
        List<IDictionary<string, object?>> buffer,
        TransformContext transformContext,
        CancellationToken cancellationToken)
    {
        if (options.IsTestMode)
            _inspector?.RecordTaskHeld(executionId, _configuration.Id, buffer.Count);

        var flushOutcome = await ProcessFlush(
            executionId, options, dataGateway, buffer, transformContext, cancellationToken).ConfigureAwait(false);

        if (options.IsTestMode && _inspector != null)
        {
            _inspector.RecordTaskHeld(executionId, _configuration.Id, -buffer.Count);
            _inspector.RecordTaskOut(executionId, _configuration.Id, flushOutcome.IsSuccess ? flushOutcome.Value.Loaded : 0);
        }

        if (!flushOutcome.IsSuccess)
            return flushOutcome.ToNewResult<(int Transformed, int Loaded, int Failed, bool SkipIteration)>();

        buffer.Clear();

        return GenericResult<(int Transformed, int Loaded, int Failed, bool SkipIteration)>.Success(
            (flushOutcome.Value.Transformed, flushOutcome.Value.Loaded, flushOutcome.Value.Failed, flushOutcome.Value.SkipIteration));
    }

    private async Task NotifyBroadcasterAfterFlush(Guid executionId, long totalExtracted, long totalLoaded)
    {
        if (_broadcaster == null) return;
        var taskState = _inspector?.GetTaskState(executionId, _configuration.Id);
        await _broadcaster.BroadcastTaskStatus(
            executionId,
            _configuration.Id,
            "Running",
            taskState?.RecordsIn ?? totalExtracted,
            taskState?.RecordsOut ?? totalLoaded,
            taskState?.RecordsDiscarded ?? 0,
            taskState?.RecordsHeld ?? 0,
            taskState?.SampleBufferAtCapacity ?? false).ConfigureAwait(false);
    }

    private static void ApplyStepRepause(PipelineTestExecutionState? testState)
    {
        if (testState == null || !testState.StepPending) return;
        // Why: After one batch in step mode, re-pause so the user must issue another Step.
        testState.StepPending = false;
        testState.PauseEvent.Reset();
    }

    private async Task<IGenericResult<(int Extracted, int Failed)>> ExtractAndBuffer(
        int effectiveTake,
        IDataGateway dataGateway,
        List<IDictionary<string, object?>> buffer,
        CancellationToken cancellationToken)
    {
        var extractResult = await ExtractBatch(effectiveTake, dataGateway, cancellationToken).ConfigureAwait(false);
        if (!extractResult.IsSuccess)
        {
            if (!_configuration.ContinueOnError)
                return extractResult.ToNewResult<(int Extracted, int Failed)>();
            return GenericResult<(int Extracted, int Failed)>.Success((0, 1));
        }

        var records = extractResult.Value!;
        buffer.AddRange(records);
        return GenericResult<(int Extracted, int Failed)>.Success((records.Count, 0));
    }

    private async Task WaitForFlushInterval(Stopwatch iterationStopwatch, CancellationToken cancellationToken)
    {
        iterationStopwatch.Stop();
        var remainingDelay = _configuration.FlushIntervalMs - (int)iterationStopwatch.ElapsedMilliseconds;
        if (remainingDelay > 0)
        {
            await Task.Delay(remainingDelay, cancellationToken).ConfigureAwait(false);
        }
    }

    private async Task<IGenericResult<(int Transformed, int Loaded, int Failed, bool SkipIteration)>> ProcessFlush(
        Guid executionId,
        PipelineExecutionOptions options,
        IDataGateway dataGateway,
        List<IDictionary<string, object?>> buffer,
        TransformContext transformContext,
        CancellationToken cancellationToken)
    {
        var flushResult = await FlushAndLoad(executionId, options, dataGateway, buffer, transformContext, cancellationToken).ConfigureAwait(false);

        if (flushResult.IsTransformFailed)
        {
            if (!_configuration.ContinueOnError)
                return GenericResult<(int Transformed, int Loaded, int Failed, bool SkipIteration)>.Failure(flushResult.Messages);
            return GenericResult<(int Transformed, int Loaded, int Failed, bool SkipIteration)>.Success(
                (0, 0, buffer.Count, true));
        }

        if (flushResult.IsLoadFailed)
        {
            if (!_configuration.ContinueOnError)
                return GenericResult<(int Transformed, int Loaded, int Failed, bool SkipIteration)>.Failure(flushResult.Messages);
            return GenericResult<(int Transformed, int Loaded, int Failed, bool SkipIteration)>.Success(
                (flushResult.TransformedCount, 0, flushResult.TransformedCount, false));
        }

        return GenericResult<(int Transformed, int Loaded, int Failed, bool SkipIteration)>.Success(
            (flushResult.TransformedCount, flushResult.LoadedCount, 0, false));
    }

    private bool ShouldFlushBuffer(int bufferCount, DateTime windowStart)
    {
        var shouldFlush = bufferCount >= _configuration.BufferSize;
        if (_configuration.UseWindowing)
        {
            var windowElapsed = DateTime.UtcNow - windowStart;
            shouldFlush = shouldFlush || windowElapsed.TotalSeconds >= _configuration.WindowDurationSeconds;
        }

        return shouldFlush;
    }

    private async Task<FlushResult> FlushAndLoad(
        Guid executionId,
        PipelineExecutionOptions options,
        IDataGateway dataGateway,
        List<IDictionary<string, object?>> buffer,
        TransformContext transformContext,
        CancellationToken cancellationToken)
    {
        var transformResult = await TransformRecords(buffer, transformContext, cancellationToken).ConfigureAwait(false);
        if (!transformResult.IsSuccess)
            return FlushResult.TransformFailed(transformResult.Messages);

        var transformedRecords = transformResult.Value!;

        // Why: In test mode with SkipDestinationWrites, replace the BulkInsertCommand with a
        // no-op that logs the row count — prevents writing to production targets during testing.
        if (options.IsTestMode && options.SkipDestinationWrites)
        {
            EtlLog.TestModeWriteSkipped(_logger, Name, transformedRecords.Count);
            return FlushResult.Succeeded(transformedRecords.Count, transformedRecords.Count);
        }

        var loadResult = await LoadRecordsBatch(dataGateway, transformedRecords, cancellationToken).ConfigureAwait(false);
        if (!loadResult.IsSuccess)
            return FlushResult.LoadFailed(transformedRecords.Count, loadResult.Messages);

        return FlushResult.Succeeded(transformedRecords.Count, loadResult.Value);
    }

    private async Task ApplyRateLimiting(int totalLoaded, Stopwatch stopwatch, CancellationToken cancellationToken)
    {
        if (_configuration.MaxRecordsPerSecond.HasValue && totalLoaded > 0)
        {
            var expectedDuration = TimeSpan.FromSeconds((double)totalLoaded / _configuration.MaxRecordsPerSecond.Value);
            var actualDuration = stopwatch.Elapsed;
            if (actualDuration < expectedDuration)
            {
                var delay = expectedDuration - actualDuration;
                await Task.Delay(delay, cancellationToken).ConfigureAwait(false);
            }
        }
    }

    /// <summary>
    /// Flush result discriminated union — distinguishes transform failures from load failures.
    /// </summary>
    private sealed class FlushResult
    {
        private FlushResult() { }

        public bool IsTransformFailed { get; private init; }
        public bool IsLoadFailed { get; private init; }
        public int TransformedCount { get; private init; }
        public int LoadedCount { get; private init; }
        public IEnumerable<IGenericMessage> Messages { get; private init; } = [];

        public static FlushResult Succeeded(int transformedCount, int loadedCount) => new()
        {
            TransformedCount = transformedCount,
            LoadedCount = loadedCount
        };

        public static FlushResult TransformFailed(IEnumerable<IGenericMessage> messages) => new()
        {
            IsTransformFailed = true,
            Messages = messages
        };

        public static FlushResult LoadFailed(int transformedCount, IEnumerable<IGenericMessage> messages) => new()
        {
            IsLoadFailed = true,
            TransformedCount = transformedCount,
            Messages = messages
        };
    }

    private async Task<IGenericResult<List<IDictionary<string, object?>>>> ExtractBatch(
        int effectiveTake,
        IDataGateway dataGateway,
        CancellationToken cancellationToken)
    {
        EtlLog.ExtractStarted(_logger, Name);
        var extractStopwatch = Stopwatch.StartNew();

        try
        {
            // Why: the pipeline source is a configured DataSet — it carries the full store→path→container
            // address plus any RecordSelector/format — so resolve it through the DataSet dispatch, the
            // same read path the dataset-query endpoint uses. The connection is taken from the DataSet source.
            var queryCommand = new QueryCommand<Dictionary<string, object?>>
            {
                Paging = new PagingExpression
                {
                    Skip = 0,
                    Take = effectiveTake
                }
            };

            var queryResult = await dataGateway.Execute<IEnumerable<Dictionary<string, object?>>>(
                queryCommand, new DataSetTarget(_configuration.SourceDataSet), cancellationToken).ConfigureAwait(false);
            extractStopwatch.Stop();

            if (!queryResult.IsSuccess)
            {
                var errorMessage = GetFirstMessageText(queryResult.Messages) ?? "Unknown error";
                return GenericResult<List<IDictionary<string, object?>>>.Failure(
                    EtlLog.ExtractFailed(_logger, Name, errorMessage));
            }

            var records = queryResult.Value?.Cast<IDictionary<string, object?>>().ToList() ?? [];
            EtlLog.ExtractCompleted(_logger, records.Count, extractStopwatch.Elapsed.TotalMilliseconds);

            return GenericResult<List<IDictionary<string, object?>>>.Success(records);
        }
        catch (Exception ex)
        {
            extractStopwatch.Stop();
            return GenericResult<List<IDictionary<string, object?>>>.Failure(
                EtlLog.ExtractFailed(_logger, ex, Name));
        }
    }

    private async Task<IGenericResult<List<IDictionary<string, object?>>>> TransformRecords(
        List<IDictionary<string, object?>> records,
        TransformContext transformContext,
        CancellationToken cancellationToken)
    {
        var transformCount = _configuration.Transforms?.Count ?? 0;
        EtlLog.TransformStarted(_logger, transformCount);
        var transformStopwatch = Stopwatch.StartNew();

        try
        {
            var orderedTransforms = GetOrderedTransforms();
            if (orderedTransforms == null)
            {
                transformStopwatch.Stop();
                EtlLog.TransformCompleted(_logger, records.Count, transformStopwatch.Elapsed.TotalMilliseconds);
                return GenericResult<List<IDictionary<string, object?>>>.Success(records);
            }

            var transformedRecords = new List<IDictionary<string, object?>>();
            var errorCount = 0;

            foreach (var record in records)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var singleResult = await TransformSingleRecord(record, orderedTransforms, transformContext, cancellationToken).ConfigureAwait(false);
                if (singleResult.HardFailure != null)
                {
                    transformStopwatch.Stop();
                    return singleResult.HardFailure;
                }

                if (singleResult.IsError) errorCount++;
                if (!singleResult.RecordFailed) transformedRecords.Add(singleResult.Record!);

                if (errorCount >= _configuration.MaxErrors)
                {
                    transformStopwatch.Stop();
                    return GenericResult<List<IDictionary<string, object?>>>.Failure(
                        EtlLog.ExecutionFailed(_logger, Name, transformContext.ExecutionId, $"Maximum error count ({_configuration.MaxErrors}) exceeded"));
                }
            }

            transformStopwatch.Stop();
            EtlLog.TransformCompleted(_logger, transformedRecords.Count, transformStopwatch.Elapsed.TotalMilliseconds);

            return GenericResult<List<IDictionary<string, object?>>>.Success(transformedRecords);
        }
        catch (Exception ex)
        {
            transformStopwatch.Stop();
            return GenericResult<List<IDictionary<string, object?>>>.Failure(
                EtlLog.TransformFailed(_logger, ex, Name));
        }
    }

    private List<PipelineTransformConfiguration>? GetOrderedTransforms()
    {
        if (_configuration.Transforms == null || _configuration.Transforms.Count == 0) return null;
        return _configuration.Transforms
            .Where(t => t.IsEnabled)
            .OrderBy(t => t.ExecutionOrder)
            .ToList();
    }

    private async Task<SingleRecordTransformResult> TransformSingleRecord(
        IDictionary<string, object?> record,
        List<PipelineTransformConfiguration> orderedTransforms,
        TransformContext transformContext,
        CancellationToken cancellationToken)
    {
        var currentRecord = record;

        foreach (var transformConfig in orderedTransforms)
        {
            cancellationToken.ThrowIfCancellationRequested();
            // Why: TransformTypes (ETL.Abstractions TypeCollection) is the execution-layer registry for
            // ITransformType implementations. OperationTypes (Services.Transformations) is the ServiceTypeCollection
            // for DI-managed transformation services. These are separate registries; future unification
            // under OperationTypes is tracked in FDW-389 Phase 2.
            var transformType = TransformTypes.ByName(transformConfig.OperationType);
            if (transformType == null)
            {
                if (!_configuration.ContinueOnError)
                {
                    return SingleRecordTransformResult.AsHardFailure(
                        GenericResult<List<IDictionary<string, object?>>>.Failure(
                            EtlLog.TransformFailed(_logger, Name, transformConfig.OperationType, $"Unknown transform type: {transformConfig.OperationType}")));
                }

                return SingleRecordTransformResult.AsRecordError();
            }

            var transformResult = await transformType.Transform(currentRecord, transformConfig, transformContext, cancellationToken).ConfigureAwait(false);
            if (!transformResult.IsSuccess)
            {
                if (!_configuration.ContinueOnError)
                    return SingleRecordTransformResult.AsHardFailure(transformResult.ToNewResult<List<IDictionary<string, object?>>>());
                return SingleRecordTransformResult.AsRecordError();
            }

            if (transformResult.Value == null) return SingleRecordTransformResult.AsRecordDropped();
            currentRecord = transformResult.Value;
        }

        return SingleRecordTransformResult.AsSuccess(currentRecord);
    }

    private readonly struct SingleRecordTransformResult
    {
        private SingleRecordTransformResult(
            IDictionary<string, object?>? record,
            bool recordFailed,
            bool isError,
            IGenericResult<List<IDictionary<string, object?>>>? hardFailure)
        {
            Record = record;
            RecordFailed = recordFailed;
            IsError = isError;
            HardFailure = hardFailure;
        }

        public IDictionary<string, object?>? Record { get; }
        public bool RecordFailed { get; }
        public bool IsError { get; }
        public IGenericResult<List<IDictionary<string, object?>>>? HardFailure { get; }

        public static SingleRecordTransformResult AsSuccess(IDictionary<string, object?> record) =>
            new(record, recordFailed: false, isError: false, hardFailure: null);

        public static SingleRecordTransformResult AsRecordError() =>
            new(record: null, recordFailed: true, isError: true, hardFailure: null);

        public static SingleRecordTransformResult AsRecordDropped() =>
            new(record: null, recordFailed: true, isError: false, hardFailure: null);

        public static SingleRecordTransformResult AsHardFailure(IGenericResult<List<IDictionary<string, object?>>> failure) =>
            new(record: null, recordFailed: true, isError: false, hardFailure: failure);
    }

    private async Task<IGenericResult<int>> LoadRecordsBatch(
        IDataGateway dataGateway,
        List<IDictionary<string, object?>> records,
        CancellationToken cancellationToken)
    {
        EtlLog.LoadStarted(_logger, Name);
        var loadStopwatch = Stopwatch.StartNew();

        try
        {
            if (records.Count == 0)
            {
                loadStopwatch.Stop();
                EtlLog.LoadCompleted(_logger, 0, loadStopwatch.Elapsed.TotalMilliseconds);
                return GenericResult<int>.Success(0);
            }

            // Why: the destination is a single-source DataSet sink; the bulk insert is forwarded to
            // its one container through the DataSet dispatch, which carries the full store→path→container address.
            var insertCommand = new BulkInsertCommand<IDictionary<string, object?>>(records);

            var insertResult = await dataGateway.Execute<int>(
                insertCommand, new DataSetTarget(_configuration.DestinationDataSet), cancellationToken).ConfigureAwait(false);
            loadStopwatch.Stop();

            if (!insertResult.IsSuccess)
            {
                var errorMessage = GetFirstMessageText(insertResult.Messages) ?? "Unknown error";
                return GenericResult<int>.Failure(
                    EtlLog.LoadFailed(_logger, Name, errorMessage));
            }

            EtlLog.LoadCompleted(_logger, insertResult.Value, loadStopwatch.Elapsed.TotalMilliseconds);
            return GenericResult<int>.Success(insertResult.Value);
        }
        catch (Exception ex)
        {
            loadStopwatch.Stop();
            return GenericResult<int>.Failure(
                EtlLog.LoadFailed(_logger, ex, Name));
        }
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
            messages.Add(EtlLog.PipelineCreationFailed(_logger, "unknown", "Pipeline name is required"));

        if (string.IsNullOrWhiteSpace(_configuration.SourceConnectionName))
            messages.Add(EtlLog.PipelineCreationFailed(_logger, Name, "Source connection name is required"));

        if (string.IsNullOrWhiteSpace(_configuration.DestinationConnectionName))
            messages.Add(EtlLog.PipelineCreationFailed(_logger, Name, "Destination connection name is required"));

        if (_configuration.BufferSize < 1)
            messages.Add(EtlLog.PipelineCreationFailed(_logger, Name, "BufferSize must be at least 1"));

        if (_configuration.FlushIntervalMs < 100)
            messages.Add(EtlLog.PipelineCreationFailed(_logger, Name, "FlushIntervalMs must be at least 100"));

        if (_configuration.UseWindowing && _configuration.WindowDurationSeconds < 1)
            messages.Add(EtlLog.PipelineCreationFailed(_logger, Name, "WindowDurationSeconds must be at least 1 when windowing is enabled"));

        if (messages.Count > 0) return GenericResult.Failure(messages);
        return GenericResult.Success();
    }
}
