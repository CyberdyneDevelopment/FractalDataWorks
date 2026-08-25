using System;
using Microsoft.Extensions.Logging;
using Fdw.Messages;
using Fdw.MessageLogging;

namespace Fdw.Services.Etl.Logging;

/// <summary>
/// MessageLogging methods for ETL pipeline operations.
/// Every log message is returned in the result AND logged.
/// EventId range: 8101-8199
/// </summary>
[MessageLoggingTypeCode("ETL")]
public static partial class EtlLog
{
    // ═══════════════════════════════════════════════════════════════════════════
    // Provider Events (8101-8110)
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Logs when getting a pipeline by name.
    /// </summary>
    [MessageLogging(
        EventId = 11000,
        Level = LogLevel.Debug,
        Message = "Getting pipeline by name '{pipelineName}'")]
    public static partial IGenericMessage GettingPipelineByName(
        ILogger logger,
        string pipelineName);

    /// <summary>
    /// Logs when getting a pipeline by configuration.
    /// </summary>
    [MessageLogging(
        EventId = 11001,
        Level = LogLevel.Debug,
        Message = "Getting pipeline by configuration for type '{pipelineType}'")]
    public static partial IGenericMessage GettingPipeline(
        ILogger logger,
        string pipelineType);

    /// <summary>
    /// Logs when getting a pipeline with creation context.
    /// </summary>
    [MessageLogging(
        EventId = 11002,
        Level = LogLevel.Debug,
        Message = "Getting pipeline with context for type '{pipelineType}'")]
    public static partial IGenericMessage GettingPipelineWithContext(
        ILogger logger,
        string pipelineType);

    /// <summary>
    /// Logs when a pipeline configuration is loaded.
    /// </summary>
    [MessageLogging(
        EventId = 11003,
        Level = LogLevel.Information,
        Message = "Pipeline configuration loaded: '{pipelineName}' (type: {pipelineType})")]
    public static partial IGenericMessage PipelineConfigurationLoaded(
        ILogger logger,
        string pipelineName,
        string pipelineType);

    /// <summary>
    /// Logs when a factory is registered with the provider.
    /// </summary>
    [MessageLogging(
        EventId = 11004,
        Level = LogLevel.Information,
        Message = "Pipeline factory registered for type '{pipelineType}'")]
    public static partial IGenericMessage FactoryRegistered(
        ILogger logger,
        string pipelineType);

    /// <summary>
    /// Logs when creating a pipeline with a factory.
    /// </summary>
    [MessageLogging(
        EventId = 11005,
        Level = LogLevel.Debug,
        Message = "Creating pipeline '{pipelineName}' with factory '{factoryName}'")]
    public static partial IGenericMessage CreatingPipelineWithFactory(
        ILogger logger,
        string pipelineName,
        string factoryName);

    /// <summary>
    /// Logs when the configuration cache is cleared.
    /// </summary>
    [MessageLogging(
        EventId = 11006,
        Level = LogLevel.Information,
        Message = "Pipeline configuration cache cleared ({count} entries removed)")]
    public static partial IGenericMessage CacheCleared(
        ILogger logger,
        int count);

    // ═══════════════════════════════════════════════════════════════════════════
    // Pipeline Execution Events (8111-8120)
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Logs when a pipeline execution starts.
    /// </summary>
    [MessageLogging(
        EventId = 11007,
        Level = LogLevel.Information,
        Message = "Pipeline execution started: '{pipelineName}' (ExecutionId: {executionId})")]
    public static partial IGenericMessage ExecutionStarted(
        ILogger logger,
        string pipelineName,
        Guid executionId);

    /// <summary>
    /// Logs when a pipeline execution completes successfully.
    /// </summary>
    [MessageLogging(
        EventId = 11008,
        Level = LogLevel.Information,
        Message = "Pipeline execution completed: '{pipelineName}' (ExecutionId: {executionId}, Records: {recordCount}, Duration: {durationMs}ms)")]
    public static partial IGenericMessage ExecutionCompleted(
        ILogger logger,
        string pipelineName,
        Guid executionId,
        int recordCount,
        double durationMs);

    /// <summary>
    /// Logs when pipeline extraction phase starts.
    /// </summary>
    [MessageLogging(
        EventId = 11009,
        Level = LogLevel.Debug,
        Message = "Extract phase started for '{pipelineName}'")]
    public static partial IGenericMessage ExtractStarted(
        ILogger logger,
        string pipelineName);

    /// <summary>
    /// Logs when pipeline extraction phase completes.
    /// </summary>
    [MessageLogging(
        EventId = 11010,
        Level = LogLevel.Information,
        Message = "Extract phase completed: {recordCount} records extracted in {durationMs}ms")]
    public static partial IGenericMessage ExtractCompleted(
        ILogger logger,
        int recordCount,
        double durationMs);

    /// <summary>
    /// Logs when pipeline transformation phase starts.
    /// </summary>
    [MessageLogging(
        EventId = 11011,
        Level = LogLevel.Debug,
        Message = "Transform phase started with {transformCount} transforms")]
    public static partial IGenericMessage TransformStarted(
        ILogger logger,
        int transformCount);

    /// <summary>
    /// Logs when pipeline transformation phase completes.
    /// </summary>
    [MessageLogging(
        EventId = 11012,
        Level = LogLevel.Information,
        Message = "Transform phase completed: {recordCount} records transformed in {durationMs}ms")]
    public static partial IGenericMessage TransformCompleted(
        ILogger logger,
        int recordCount,
        double durationMs);

    /// <summary>
    /// Logs when pipeline load phase starts.
    /// </summary>
    [MessageLogging(
        EventId = 11013,
        Level = LogLevel.Debug,
        Message = "Load phase started for '{pipelineName}'")]
    public static partial IGenericMessage LoadStarted(
        ILogger logger,
        string pipelineName);

    /// <summary>
    /// Logs when pipeline load phase completes.
    /// </summary>
    [MessageLogging(
        EventId = 11014,
        Level = LogLevel.Information,
        Message = "Load phase completed: {recordCount} records loaded in {durationMs}ms")]
    public static partial IGenericMessage LoadCompleted(
        ILogger logger,
        int recordCount,
        double durationMs);

    // ═══════════════════════════════════════════════════════════════════════════
    // Transform Events (8121-8130)
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Logs when applying a transform.
    /// </summary>
    [MessageLogging(
        EventId = 11015,
        Level = LogLevel.Debug,
        Message = "Applying transform '{transformName}' (type: {transformType})")]
    public static partial IGenericMessage ApplyingTransform(
        ILogger logger,
        string transformName,
        string transformType);

    /// <summary>
    /// Logs when a transform completes.
    /// </summary>
    [MessageLogging(
        EventId = 11016,
        Level = LogLevel.Debug,
        Message = "Transform '{transformName}' completed: {inputCount} → {outputCount} records")]
    public static partial IGenericMessage TransformApplied(
        ILogger logger,
        string transformName,
        int inputCount,
        int outputCount);

    /// <summary>
    /// Logs when a transform type is registered.
    /// </summary>
    [MessageLogging(
        EventId = 11017,
        Level = LogLevel.Information,
        Message = "Transform type registered: '{transformType}'")]
    public static partial IGenericMessage TransformTypeRegistered(
        ILogger logger,
        string transformType);

    // ═══════════════════════════════════════════════════════════════════════════
    // Error Events (8141-8160)
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Logs when a pipeline configuration is not found.
    /// </summary>
    [MessageLogging(
        EventId = 31000,
        Level = LogLevel.Error,
        Message = "Pipeline configuration not found: '{pipelineName}'")]
    public static partial IGenericMessage PipelineConfigurationNotFound(
        ILogger logger,
        string pipelineName);

    /// <summary>
    /// Logs when configuration loading fails.
    /// </summary>
    [MessageLogging(
        EventId = 61000,
        Level = LogLevel.Error,
        Message = "Failed to load configuration for pipeline '{pipelineName}' (type: {pipelineType})")]
    public static partial IGenericMessage ConfigurationLoadFailed(
        ILogger logger,
        string pipelineName,
        string pipelineType);

    /// <summary>
    /// Logs when no factory is registered for a pipeline type.
    /// </summary>
    [MessageLogging(
        EventId = 61001,
        Level = LogLevel.Error,
        Message = "No factory registered for pipeline type '{pipelineType}'")]
    public static partial IGenericMessage NoFactoryRegistered(
        ILogger logger,
        string pipelineType);

    /// <summary>
    /// Logs when pipeline creation fails.
    /// </summary>
    [MessageLogging(
        EventId = 91000,
        Level = LogLevel.Error,
        Message = "Failed to create pipeline '{pipelineName}': {error}")]
    public static partial IGenericMessage PipelineCreationFailed(
        ILogger logger,
        string pipelineName,
        string error);

    /// <summary>
    /// Logs when pipeline execution fails.
    /// </summary>
    [MessageLogging(
        EventId = 91001,
        Level = LogLevel.Error,
        Message = "Pipeline execution failed: '{pipelineName}' (ExecutionId: {executionId}) - {error}")]
    public static partial IGenericMessage ExecutionFailed(
        ILogger logger,
        string pipelineName,
        Guid executionId,
        string error);

    /// <summary>
    /// Logs when an exception occurs during pipeline operations.
    /// </summary>
    [MessageLogging(
        EventId = 91002,
        Level = LogLevel.Error,
        Message = "Exception during pipeline operation for '{pipelineName}'")]
    public static partial IGenericMessage GetPipelineByNameException(
        ILogger logger,
        Exception exception,
        string pipelineName);

    /// <summary>
    /// Logs when extraction fails.
    /// </summary>
    [MessageLogging(
        EventId = 71000,
        Level = LogLevel.Error,
        Message = "Extract phase failed for '{pipelineName}': {error}")]
    public static partial IGenericMessage ExtractFailed(
        ILogger logger,
        string pipelineName,
        string error);

    /// <summary>
    /// Logs when extraction fails due to an unhandled exception.
    /// </summary>
    [MessageLogging(
        EventId = 71001,
        Level = LogLevel.Error,
        Message = "Extract phase failed for '{pipelineName}'")]
    public static partial IGenericMessage ExtractFailed(
        ILogger logger,
        Exception ex,
        string pipelineName);

    /// <summary>
    /// Logs when transformation fails.
    /// </summary>
    [MessageLogging(
        EventId = 91003,
        Level = LogLevel.Error,
        Message = "Transform phase failed for '{pipelineName}' at transform '{transformName}': {error}")]
    public static partial IGenericMessage TransformFailed(
        ILogger logger,
        string pipelineName,
        string transformName,
        string error);

    /// <summary>
    /// Logs when transformation fails due to an unhandled exception.
    /// </summary>
    [MessageLogging(
        EventId = 91004,
        Level = LogLevel.Error,
        Message = "Transform phase failed for '{pipelineName}'")]
    public static partial IGenericMessage TransformFailed(
        ILogger logger,
        Exception ex,
        string pipelineName);

    /// <summary>
    /// Logs when loading fails.
    /// </summary>
    [MessageLogging(
        EventId = 71002,
        Level = LogLevel.Error,
        Message = "Load phase failed for '{pipelineName}': {error}")]
    public static partial IGenericMessage LoadFailed(
        ILogger logger,
        string pipelineName,
        string error);

    /// <summary>
    /// Logs when loading fails due to an unhandled exception.
    /// </summary>
    [MessageLogging(
        EventId = 71003,
        Level = LogLevel.Error,
        Message = "Load phase failed for '{pipelineName}'")]
    public static partial IGenericMessage LoadFailed(
        ILogger logger,
        Exception ex,
        string pipelineName);

    /// <summary>
    /// Logs when a record fails processing.
    /// </summary>
    [MessageLogging(
        EventId = 91005,
        Level = LogLevel.Warning,
        Message = "Record processing failed in pipeline '{pipelineName}': {error}")]
    public static partial IGenericMessage RecordProcessingFailed(
        ILogger logger,
        string pipelineName,
        string error);

    // ═══════════════════════════════════════════════════════════════════════════
    // HTTP Record Writer Load Events (8155-8158)
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Logs when the pipeline's load phase is dispatching records via the HTTP record writer capability.
    /// </summary>
    [MessageLogging(
        EventId = 11018,
        Level = LogLevel.Trace,
        Message = "Pipeline '{pipelineName}' loading {recordCount} records via HTTP record writer to connection '{connectionName}'")]
    public static partial IGenericMessage LoadingViaHttpRecordWriter(
        ILogger logger,
        string pipelineName,
        int recordCount,
        string connectionName);

    /// <summary>
    /// Logs when the HTTP record writer load path completes successfully.
    /// </summary>
    [MessageLogging(
        EventId = 11019,
        Level = LogLevel.Information,
        Message = "Pipeline '{pipelineName}' loaded {recordCount} records via HTTP record writer to connection '{connectionName}'")]
    public static partial IGenericMessage LoadViaHttpRecordWriterCompleted(
        ILogger logger,
        string pipelineName,
        int recordCount,
        string connectionName);

    /// <summary>
    /// Logs when the HTTP record writer load path fails.
    /// </summary>
    [MessageLogging(
        EventId = 71004,
        Level = LogLevel.Error,
        Message = "Pipeline '{pipelineName}' failed to load records via HTTP record writer to connection '{connectionName}': {error}")]
    public static partial IGenericMessage LoadViaHttpRecordWriterFailed(
        ILogger logger,
        string pipelineName,
        string connectionName,
        string error);

    /// <summary>
    /// Logs when the destination connection does not support any known write capability (neither
    /// BulkInsert nor HTTP record writer), so the pipeline cannot load records.
    /// </summary>
    [MessageLogging(
        EventId = 61002,
        Level = LogLevel.Error,
        Message = "Pipeline '{pipelineName}' destination connection '{connectionName}' does not support a known write capability")]
    public static partial IGenericMessage DestinationConnectionTypeUnsupported(
        ILogger logger,
        string pipelineName,
        string connectionName);

    // ═══════════════════════════════════════════════════════════════════════════
    // Diagnostic Events (8161-8180)
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Logs pipeline configuration details for debugging.
    /// </summary>
    [MessageLogging(
        EventId = 11020,
        Level = LogLevel.Debug,
        Message = "Pipeline '{pipelineName}' configuration - Type: {pipelineType}, Source: {source}, Destination: {destination}")]
    public static partial IGenericMessage PipelineConfigurationDetails(
        ILogger logger,
        string pipelineName,
        string pipelineType,
        string source,
        string destination);

    /// <summary>
    /// Logs execution metrics for debugging.
    /// </summary>
    [MessageLogging(
        EventId = 11021,
        Level = LogLevel.Debug,
        Message = "Execution metrics for '{pipelineName}' - Extracted: {extracted}, Transformed: {transformed}, Loaded: {loaded}, Failed: {failed}")]
    public static partial IGenericMessage ExecutionMetrics(
        ILogger logger,
        string pipelineName,
        int extracted,
        int transformed,
        int loaded,
        int failed);

    /// <summary>
    /// Logs when a batch is being processed.
    /// </summary>
    [MessageLogging(
        EventId = 11022,
        Level = LogLevel.Debug,
        Message = "Processing batch {batchNumber} of {totalBatches} ({recordCount} records)")]
    public static partial IGenericMessage ProcessingBatch(
        ILogger logger,
        int batchNumber,
        int totalBatches,
        int recordCount);

    /// <summary>
    /// Logs provider state for debugging.
    /// </summary>
    [MessageLogging(
        EventId = 11023,
        Level = LogLevel.Debug,
        Message = "Pipeline provider state - Factories: {factoryCount}, Cached: {cachedCount}")]
    public static partial IGenericMessage ProviderState(
        ILogger logger,
        int factoryCount,
        int cachedCount);

    /// <summary>
    /// Logs when generic command execution is attempted on a pipeline.
    /// </summary>
    [MessageLogging(
        EventId = 41000,
        Level = LogLevel.Warning,
        Message = "Pipeline '{pipelineName}' does not support generic command execution. Use Execute method instead")]
    public static partial IGenericMessage CommandExecutionNotSupported(
        ILogger logger,
        string pipelineName);

    /// <summary>
    /// Logs when an unhandled exception occurs during pipeline execution.
    /// </summary>
    [MessageLogging(
        EventId = 91006,
        Level = LogLevel.Error,
        Message = "Unhandled exception during execution of pipeline '{pipelineName}' (ExecutionId: {executionId})")]
    public static partial IGenericMessage ExecutionException(
        ILogger logger,
        Exception exception,
        string pipelineName,
        Guid executionId);

    /// <summary>
    /// Logs when a calculation expression fails for a field.
    /// </summary>
    [MessageLogging(
        EventId = 91007,
        Level = LogLevel.Error,
        Message = "Calculation failed for field '{outputField}'")]
    public static partial IGenericMessage CalculationFailed(
        ILogger logger,
        Exception exception,
        string outputField);

    // ═══════════════════════════════════════════════════════════════════════════
    // Background Executor Events (8170-8189)
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Logs when a pipeline execution request is successfully enqueued.
    /// </summary>
    [MessageLogging(
        EventId = 11024,
        Level = LogLevel.Information,
        Message = "Pipeline execution request enqueued: '{pipelineName}' ({executionId})")]
    public static partial IGenericMessage ExecutionEnqueued(
        ILogger logger,
        string pipelineName,
        Guid executionId);

    /// <summary>
    /// Logs when the pipeline execution queue is full and a request is rejected.
    /// </summary>
    [MessageLogging(
        EventId = 81000,
        Level = LogLevel.Warning,
        Message = "Pipeline execution queue full, rejecting request for: '{pipelineName}'")]
    public static partial IGenericMessage ExecutionQueueFull(
        ILogger logger,
        string pipelineName);

    /// <summary>
    /// Logs when a pipeline execution request is dequeued for processing.
    /// </summary>
    [MessageLogging(
        EventId = 11025,
        Level = LogLevel.Information,
        Message = "Pipeline execution dequeued: '{pipelineName}' ({executionId})")]
    public static partial IGenericMessage ExecutionDequeued(
        ILogger logger,
        string pipelineName,
        Guid executionId);

    /// <summary>
    /// Logs when a DI scope is created for a pipeline execution.
    /// </summary>
    [MessageLogging(
        EventId = 11026,
        Level = LogLevel.Debug,
        Message = "Pipeline execution scope created: {executionId}")]
    public static partial IGenericMessage ExecutionScopeCreated(
        ILogger logger,
        Guid executionId);

    /// <summary>
    /// Logs when a pipeline execution fails in the background service.
    /// </summary>
    [MessageLogging(
        EventId = 91008,
        Level = LogLevel.Error,
        Message = "Pipeline execution failed in background: '{pipelineName}' ({executionId}): {error}")]
    public static partial IGenericMessage ExecutionFailedInBackground(
        ILogger logger,
        string pipelineName,
        Guid executionId,
        string error);

    /// <summary>
    /// Logs when the pipeline background executor starts.
    /// </summary>
    [MessageLogging(
        EventId = 11027,
        Level = LogLevel.Information,
        Message = "Pipeline background executor started")]
    public static partial IGenericMessage BackgroundExecutorStarted(
        ILogger logger);

    /// <summary>
    /// Logs when the pipeline background executor stops (graceful shutdown).
    /// </summary>
    [MessageLogging(
        EventId = 11028,
        Level = LogLevel.Information,
        Message = "Pipeline background executor stopping, draining queue")]
    public static partial IGenericMessage BackgroundExecutorStopping(
        ILogger logger);

    /// <summary>
    /// Logs when a pipeline execution is cancelled during host shutdown.
    /// </summary>
    [MessageLogging(
        EventId = 11029,
        Level = LogLevel.Information,
        Message = "Pipeline execution cancelled during shutdown: '{pipelineName}' ({executionId})")]
    public static partial IGenericMessage ExecutionCancelledDuringShutdown(
        ILogger logger,
        Exception ex,
        string pipelineName,
        Guid executionId);

    /// <summary>
    /// Logs when an unhandled exception occurs in the background executor for a pipeline.
    /// </summary>
    [MessageLogging(
        EventId = 91009,
        Level = LogLevel.Error,
        Message = "Unhandled exception in background executor for '{pipelineName}' ({executionId})")]
    public static partial IGenericMessage ExecutionExceptionInBackground(
        ILogger logger,
        Exception exception,
        string pipelineName,
        Guid executionId);

    // ═══════════════════════════════════════════════════════════════════════════
    // Test Mode Events (8179-8199)
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Logs when a destination write is skipped in test mode.
    /// </summary>
    [MessageLogging(
        EventId = 11030,
        Level = LogLevel.Information,
        Message = "TEST MODE: Skipping write for task '{taskName}' — would write {rowCount} row(s)")]
    public static partial IGenericMessage TestModeWriteSkipped(
        ILogger logger,
        string taskName,
        int rowCount);

    /// <summary>
    /// Logs when a test-mode execution controller is registered for an execution.
    /// </summary>
    [MessageLogging(
        EventId = 11031,
        Level = LogLevel.Trace,
        Message = "Test controller registered for execution {executionId}")]
    public static partial IGenericMessage TestControllerRegistered(
        ILogger logger,
        Guid executionId);

    /// <summary>
    /// Logs when a test-mode execution controller is unregistered (execution complete).
    /// </summary>
    [MessageLogging(
        EventId = 11032,
        Level = LogLevel.Trace,
        Message = "Test controller unregistered for execution {executionId}")]
    public static partial IGenericMessage TestControllerUnregistered(
        ILogger logger,
        Guid executionId);

    /// <summary>
    /// Logs when a test execution is paused.
    /// </summary>
    [MessageLogging(
        EventId = 11033,
        Level = LogLevel.Information,
        Message = "Test execution paused: {executionId}")]
    public static partial IGenericMessage TestExecutionPaused(
        ILogger logger,
        Guid executionId);

    /// <summary>
    /// Logs when a test execution is resumed.
    /// </summary>
    [MessageLogging(
        EventId = 11034,
        Level = LogLevel.Information,
        Message = "Test execution resumed: {executionId}")]
    public static partial IGenericMessage TestExecutionResumed(
        ILogger logger,
        Guid executionId);

    /// <summary>
    /// Logs when a step command is issued to a test execution.
    /// </summary>
    [MessageLogging(
        EventId = 11035,
        Level = LogLevel.Information,
        Message = "Test execution step issued: {executionId}")]
    public static partial IGenericMessage TestExecutionStepped(
        ILogger logger,
        Guid executionId);

    /// <summary>
    /// Logs when a test execution is aborted.
    /// </summary>
    [MessageLogging(
        EventId = 11036,
        Level = LogLevel.Information,
        Message = "Test execution aborted: {executionId}")]
    public static partial IGenericMessage TestExecutionAborted(
        ILogger logger,
        Guid executionId);

    // ═══════════════════════════════════════════════════════════════════════════
    // ETL/ELT Kind Branching Events (8124-8127, 8168-8169)
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Logs when SourceKind is null at extract time — configuration is incomplete.
    /// </summary>
    [MessageLogging(
        EventId = 21000,
        Level = LogLevel.Error,
        Message = "Source kind is required for pipeline '{pipelineName}' but was not set")]
    public static partial IGenericMessage SourceKindRequired(
        ILogger logger,
        string pipelineName);

    /// <summary>
    /// Logs when DestinationKind is null at load time — configuration is incomplete.
    /// </summary>
    [MessageLogging(
        EventId = 21001,
        Level = LogLevel.Error,
        Message = "Destination kind is required for pipeline '{pipelineName}' but was not set")]
    public static partial IGenericMessage DestinationKindRequired(
        ILogger logger,
        string pipelineName);

    /// <summary>
    /// Logs when SourceKind has an unrecognised name that the executor cannot branch on.
    /// </summary>
    [MessageLogging(
        EventId = 21002,
        Level = LogLevel.Error,
        Message = "Unknown source kind '{kind}' for pipeline '{pipelineName}'")]
    public static partial IGenericMessage UnknownSourceKind(
        ILogger logger,
        string pipelineName,
        string kind);

    /// <summary>
    /// Logs when DestinationKind has an unrecognised name that the executor cannot branch on.
    /// </summary>
    [MessageLogging(
        EventId = 21003,
        Level = LogLevel.Error,
        Message = "Unknown destination kind '{kind}' for pipeline '{pipelineName}'")]
    public static partial IGenericMessage UnknownDestinationKind(
        ILogger logger,
        string pipelineName,
        string kind);

    /// <summary>
    /// Logs when the ETL extract path reads from a physical connection directly (not via DataSet dispatch).
    /// </summary>
    [MessageLogging(
        EventId = 11037,
        Level = LogLevel.Debug,
        Message = "Extracting from connection '{connectionName}', container '{containerPath}' for pipeline '{pipelineName}'")]
    public static partial IGenericMessage ExtractingFromConnection(
        ILogger logger,
        string pipelineName,
        string connectionName,
        string containerPath);

    /// <summary>
    /// Logs when the ETL load path writes to a physical connection directly (not via DataSet dispatch).
    /// </summary>
    [MessageLogging(
        EventId = 11038,
        Level = LogLevel.Debug,
        Message = "Loading to connection '{connectionName}', container '{containerPath}' for pipeline '{pipelineName}'")]
    public static partial IGenericMessage LoadingToConnection(
        ILogger logger,
        string pipelineName,
        string connectionName,
        string containerPath);

    // ═══════════════════════════════════════════════════════════════════════════
    // Pipeline Factory Events (8199)
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Logs when the pipeline factory carries the kind-level Transforms onto the engine body across the
    /// kind→engine unwrap seam (the engine's [NotMapped] Transforms cannot load via its own FK, so the
    /// factory transfers the composed transforms from the ETL-kind body).
    /// </summary>
    [MessageLogging(
        EventId = 11039,
        Level = LogLevel.Debug,
        Message = "Transferred {transformCount} transform(s) from the ETL-kind body to engine pipeline '{pipelineName}'")]
    public static partial IGenericMessage TransformsTransferredKindToEngine(
        ILogger logger,
        string pipelineName,
        int transformCount);

    // ═══════════════════════════════════════════════════════════════════════════
    // State Walk / Lifecycle Warning Events (91010-91013)
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Logs when a step in the Scheduled→Triggered→Initialized→Running state walk fails.
    /// Non-fatal — execution proceeds; the row may remain at an earlier state.
    /// </summary>
    [MessageLogging(
        EventId = 91010,
        Level = LogLevel.Error,
        Message = "State transition to '{toState}' failed for execution {executionId}: {message}")]
    public static partial IGenericMessage StateTransitionStepFailed(
        ILogger logger,
        Guid executionId,
        string toState,
        string? message);

    /// <summary>
    /// Logs when the Complete call inside HandleCancellation fails.
    /// Non-fatal — broadcast and signaler still run.
    /// </summary>
    [MessageLogging(
        EventId = 91011,
        Level = LogLevel.Error,
        Message = "Cancellation Complete failed for execution {executionId}: {message}")]
    public static partial IGenericMessage CancellationCompleteFailed(
        ILogger logger,
        Guid executionId,
        string? message);

    /// <summary>
    /// Logs when the metrics Update command in CompleteWithMetrics fails.
    /// Non-fatal — tracker Complete and broadcast still run.
    /// </summary>
    [MessageLogging(
        EventId = 91012,
        Level = LogLevel.Error,
        Message = "Metrics update failed for execution {executionId}: {message}")]
    public static partial IGenericMessage MetricsUpdateFailed(
        ILogger logger,
        Guid executionId,
        string? message);

    /// <summary>
    /// Logs when the execution tracker Complete call in CompleteWithMetrics fails.
    /// Non-fatal — broadcast and signaler still run.
    /// </summary>
    [MessageLogging(
        EventId = 91013,
        Level = LogLevel.Error,
        Message = "Execution tracker Complete failed for execution {executionId}: {message}")]
    public static partial IGenericMessage CompletionRecordFailed(
        ILogger logger,
        Guid executionId,
        string? message);

    // ═══════════════════════════════════════════════════════════════════════════
    // Combine Transform Authoring Events (FDW-556) — typed children replace
    // ConfigurationJson; set-based execution via TransformBatch (11040-11055)
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Logs when a transform-operation spec (the create/update request) is mapped onto its typed
    /// configuration children by <c>ITransformOperationMapper</c>.
    /// </summary>
    [MessageLogging(
        EventId = 11040,
        Level = LogLevel.Debug,
        Message = "Mapped transform spec '{name}' ({operationType}) to typed config")]
    public static partial IGenericMessage TransformSpecMapped(
        ILogger logger,
        string name,
        string operationType);

    /// <summary>
    /// Logs when a set-based transform step starts in the <c>BatchCopyPipeline</c> fold.
    /// </summary>
    [MessageLogging(
        EventId = 11041,
        Level = LogLevel.Debug,
        Message = "Transform step '{name}' ({operationType}) starting over {inCount} rows")]
    public static partial IGenericMessage TransformStepStarted(
        ILogger logger,
        string name,
        string operationType,
        int inCount);

    /// <summary>
    /// Logs when a set-based transform step completes in the <c>BatchCopyPipeline</c> fold.
    /// </summary>
    [MessageLogging(
        EventId = 11042,
        Level = LogLevel.Information,
        Message = "Transform step '{name}' produced {outCount} rows (in {inCount})")]
    public static partial IGenericMessage TransformStepCompleted(
        ILogger logger,
        string name,
        int outCount,
        int inCount);

    /// <summary>
    /// Logs the group-by fan-in of an Aggregate transform step.
    /// </summary>
    [MessageLogging(
        EventId = 11043,
        Level = LogLevel.Debug,
        Message = "Aggregate '{name}' grouped {inCount} to {groupCount} on [{groupByFields}]")]
    public static partial IGenericMessage AggregateGrouped(
        ILogger logger,
        string name,
        int inCount,
        int groupCount,
        string groupByFields);

    /// <summary>
    /// Logs when a Lookup transform's batch pre-load populates its runtime cache.
    /// </summary>
    [MessageLogging(
        EventId = 11044,
        Level = LogLevel.Debug,
        Message = "Lookup '{name}' preloaded {keyCount} keys from {connection}:{dataSet}")]
    public static partial IGenericMessage LookupBatchPreloaded(
        ILogger logger,
        string name,
        int keyCount,
        string connection,
        string dataSet);

    /// <summary>
    /// Logs when an Aggregate transform has no group-by fields and/or aggregations — fail loud
    /// rather than a silent pass-through.
    /// </summary>
    [MessageLogging(
        EventId = 11045,
        Level = LogLevel.Error,
        Message = "Aggregate '{name}' has no group-by fields and/or aggregations")]
    public static partial IGenericMessage AggregateParamsMissing(
        ILogger logger,
        string name);

    /// <summary>
    /// Logs when a Lookup transform is missing its connection/dataset/keys or lookup columns —
    /// fail loud rather than a silent pass-through.
    /// </summary>
    [MessageLogging(
        EventId = 11046,
        Level = LogLevel.Error,
        Message = "Lookup '{name}' missing connection/dataset/keys or lookup columns")]
    public static partial IGenericMessage LookupParamsMissing(
        ILogger logger,
        string name);

    /// <summary>
    /// Logs when a Calculate transform has no computed columns — fail loud rather than a silent
    /// pass-through.
    /// </summary>
    [MessageLogging(
        EventId = 11047,
        Level = LogLevel.Error,
        Message = "Calculate '{name}' has no computed columns")]
    public static partial IGenericMessage CalculationParamsMissing(
        ILogger logger,
        string name);

    /// <summary>
    /// Logs when a Filter transform has no filter expression — fail loud rather than a silent
    /// pass-through.
    /// </summary>
    [MessageLogging(
        EventId = 11048,
        Level = LogLevel.Error,
        Message = "Filter '{name}' has no filter expression")]
    public static partial IGenericMessage FilterExpressionMissing(
        ILogger logger,
        string name);

    /// <summary>
    /// Logs when the transform-operation type name does not resolve against <c>TransformTypes</c>.
    /// </summary>
    [MessageLogging(
        EventId = 11049,
        Level = LogLevel.Error,
        Message = "Unknown transform type '{operationType}'")]
    public static partial IGenericMessage UnknownTransformType(
        ILogger logger,
        string operationType);

    /// <summary>
    /// Logs when an aggregation names a function that does not resolve against <c>AggregateFunctions</c>.
    /// </summary>
    [MessageLogging(
        EventId = 11050,
        Level = LogLevel.Error,
        Message = "Unknown aggregate function '{function}' on '{name}'")]
    public static partial IGenericMessage UnknownAggregateFunction(
        ILogger logger,
        string function,
        string name);


    /// <summary>
    /// Logs when a transform option receives a configuration instance of the wrong concrete type.
    /// </summary>
    [MessageLogging(
        EventId = 11052,
        Level = LogLevel.Error,
        Message = "Transform '{name}' received configuration of type '{actual}'")]
    public static partial IGenericMessage WrongConfigurationType(
        ILogger logger,
        string name,
        string actual);

    /// <summary>
    /// Logs when a Map transform has no field mappings — fail loud rather than a silent pass-through.
    /// </summary>
    [MessageLogging(
        EventId = 11053,
        Level = LogLevel.Error,
        Message = "Map '{name}' has no field mappings")]
    public static partial IGenericMessage MapFieldMappingsMissing(
        ILogger logger,
        string name);

    /// <summary>
    /// Logs when a Lookup transform names a join type that does not resolve against <c>LookupJoinTypes</c>.
    /// </summary>
    [MessageLogging(
        EventId = 11054,
        Level = LogLevel.Error,
        Message = "Unknown lookup join type '{joinType}' on '{name}'")]
    public static partial IGenericMessage UnknownJoinType(
        ILogger logger,
        string joinType,
        string name);


    /// <summary>
    /// Logs when the per-record <c>Transform</c> is invoked on a set-based transform option
    /// (currently Aggregate) — the engine must call <c>TransformBatch</c> instead.
    /// </summary>
    [MessageLogging(
        EventId = 11056,
        Level = LogLevel.Error,
        Message = "Transform '{name}' is set-based and cannot run per-record; the engine must call TransformBatch")]
    public static partial IGenericMessage TransformRequiresBatchExecution(
        ILogger logger,
        string name);

    /// <summary>
    /// Logs when a per-execution WorkAuthenticationContext is established on the background
    /// execution's DI scope, carrying the execution's TenantId for RLS SESSION_CONTEXT.
    /// </summary>
    [MessageLogging(
        EventId = 11057,
        Level = LogLevel.Information,
        Message = "WorkAuthenticationContext established for execution {executionId} with TenantId {tenantId}")]
    public static partial IGenericMessage WorkAuthenticationContextEstablished(
        ILogger logger,
        Guid executionId,
        Guid tenantId);

}
