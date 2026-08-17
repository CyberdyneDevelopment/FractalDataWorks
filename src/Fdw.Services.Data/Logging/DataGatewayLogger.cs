using Microsoft.Extensions.Logging;
using Fdw.Messages;
using Fdw.MessageLogging;

namespace Fdw.Services.Data.Logging;

/// <summary>
/// Static logger class for DataGateway operations using MessageLogging infrastructure.
/// EventId range: 1-2, 1002-1036, 5200-5215
/// </summary>
[MessageLoggingTypeCode("DATA")]
public static partial class DataGatewayLogger
{
    /// <summary>
    /// Logs when a data command is being routed to a connection.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="commandType">The type of command being routed.</param>
    /// <param name="connectionName">The name of the connection.</param>
    /// <returns>A generic message containing the log information.</returns>
    [MessageLogging(
        EventId = 11031,
        Level = LogLevel.Debug,
        Message = "Routing data command {commandType} to connection {connectionName}")]
    public static partial IGenericMessage RoutingCommand(ILogger logger, string commandType, string connectionName);

    /// <summary>
    /// Logs when retrieving a data connection fails with the underlying reason.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="connectionName">The name of the connection that failed.</param>
    /// <param name="reason">The underlying reason for the failure.</param>
    /// <returns>A generic message containing the error information.</returns>
    [MessageLogging(
        EventId = 71006,
        Level = LogLevel.Error,
        Message = "Failed to get data connection '{connectionName}': {reason}")]
    public static partial IGenericMessage ConnectionRetrievalFailed(ILogger logger, string connectionName, string reason);

    /// <summary>
    /// Logs when starting to execute a container command.
    /// </summary>
    [MessageLogging(
        EventId = 11032,
        Level = LogLevel.Debug,
        Message = "Executing {commandType} on container '{containerName}' via connection '{connectionName}'")]
    public static partial IGenericMessage ExecutingContainerCommand(
        ILogger logger,
        string commandType,
        string containerName,
        string connectionName);

    /// <summary>
    /// Logs successful completion of a container command.
    /// </summary>
    [MessageLogging(
        EventId = 11033,
        Level = LogLevel.Debug,
        Message = "Container command completed on '{containerName}' in {durationMs}ms")]
    public static partial IGenericMessage ContainerCommandCompleted(
        ILogger logger,
        string containerName,
        double durationMs);

    /// <summary>
    /// Logs when a container command fails.
    /// </summary>
    [MessageLogging(
        EventId = 71007,
        Level = LogLevel.Error,
        Message = "Container command failed on '{containerName}': {reason}")]
    public static partial IGenericMessage ContainerCommandFailed(
        ILogger logger,
        string containerName,
        string reason);

    /// <summary>
    /// Logs when a source container cannot be built from configuration.
    /// </summary>
    [MessageLogging(
        EventId = 61007,
        Level = LogLevel.Error,
        Message = "Cannot build container from source '{sourceName}': no SQL table or path configured")]
    public static partial IGenericMessage SourceContainerBuildFailed(
        ILogger logger,
        string sourceName);

    /// <summary>
    /// Logs when routing command to a dataset instead of a container.
    /// </summary>
    [LoggerMessage(
        EventId = 1010,
        Level = LogLevel.Information,
        Message = "Routing command to dataset '{DataSetName}'")]
    public static partial void RoutingToDataSet(ILogger logger, string dataSetName);

    /// <summary>
    /// Logs when executing a simple (single-source) dataset.
    /// </summary>
    [LoggerMessage(
        EventId = 1011,
        Level = LogLevel.Information,
        Message = "Executing simple dataset '{DataSetName}'")]
    public static partial void ExecutingSimpleDataSet(ILogger logger, string dataSetName);

    /// <summary>
    /// Logs when executing a multi-source dataset (same connection, union across sources).
    /// </summary>
    [LoggerMessage(
        EventId = 1012,
        Level = LogLevel.Information,
        Message = "Executing multi-source dataset '{DataSetName}' with {SourceCount} sources")]
    public static partial void ExecutingMultiSourceDataSet(ILogger logger, string dataSetName, int sourceCount);

    /// <summary>
    /// Logs when executing a distributed dataset (multiple connections).
    /// </summary>
    [LoggerMessage(
        EventId = 1013,
        Level = LogLevel.Information,
        Message = "Executing distributed dataset '{DataSetName}' across {SourceCount} sources and {ConnectionCount} connections")]
    public static partial void ExecutingDistributedDataSet(ILogger logger, string dataSetName, int sourceCount, int connectionCount);

    /// <summary>
    /// Logs completion of simple dataset execution.
    /// </summary>
    [LoggerMessage(
        EventId = 1014,
        Level = LogLevel.Information,
        Message = "Simple dataset '{DataSetName}' executed in {ExecutionTimeMs}ms")]
    public static partial void SimpleDataSetExecuted(ILogger logger, string dataSetName, double executionTimeMs);

    /// <summary>
    /// Logs when a multi-source dataset falls back to distributed execution.
    /// </summary>
    [LoggerMessage(
        EventId = 1015,
        Level = LogLevel.Warning,
        Message = "Multi-source dataset '{DataSetName}' on connection '{ConnectionName}' falling back to distributed execution (native JOIN not implemented)")]
    public static partial void MultiSourceDataSetFallingBackToDistributed(ILogger logger, string dataSetName, string connectionName);

    /// <summary>
    /// Logs when executing a compound (single-store pushed-down JOIN) dataset.
    /// </summary>
    [LoggerMessage(
        EventId = 1016,
        Level = LogLevel.Information,
        Message = "Executing compound dataset '{DataSetName}' with {SourceCount} sources (pushed-down join)")]
    public static partial void ExecutingCompoundDataSet(ILogger logger, string dataSetName, int sourceCount);

    /// <summary>
    /// Logs when a requested container was not found in configuration.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="containerName">The name of the container that was not found.</param>
    /// <returns>A generic message containing the error information.</returns>
    [MessageLogging(
        EventId = 31005,
        Level = LogLevel.Error,
        Message = "Container '{containerName}' not found in configuration")]
    public static partial IGenericMessage ContainerNotFound(ILogger logger, string containerName);

    /// <summary>
    /// Logs when container resolution cannot run because no IDataStoreProvider was wired at
    /// construction (bootstrap/test path). This is a fail-loud path — never a silent skip.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <returns>A generic message containing the error information.</returns>
    // Why (FDW-583): the comment above the null-check in DataGatewayService.ResolveContainer already
    // said "fail loud" but no log call backed it — container routing silently returned null and the
    // caller re-derived a generic "container not found" instead of naming the real cause.
    [MessageLogging(
        EventId = 71052,
        Level = LogLevel.Error,
        Message = "Container resolution cannot run: no IDataStoreProvider is available on this DataGatewayService instance")]
    public static partial IGenericMessage DataStoreProviderUnavailable(ILogger logger);

    /// <summary>
    /// Logs when applying calculated fields to dataset results.
    /// </summary>
    [MessageLogging(
        EventId = 11034,
        Level = LogLevel.Debug,
        Message = "Applying {calculatedFieldCount} calculated fields to dataset {dataSetName}")]
    public static partial IGenericMessage ApplyingCalculatedFields(
        ILogger logger,
        int calculatedFieldCount,
        string dataSetName);

    /// <summary>
    /// Logs successful calculated field application.
    /// </summary>
    [MessageLogging(
        EventId = 11035,
        Level = LogLevel.Debug,
        Message = "Applied calculated fields to {rowCount} rows in {elapsedMs}ms")]
    public static partial IGenericMessage CalculatedFieldsApplied(
        ILogger logger,
        int rowCount,
        double elapsedMs);

    /// <summary>
    /// Logs when calculated field execution fails for a specific field.
    /// </summary>
    [MessageLogging(
        EventId = 91003,
        Level = LogLevel.Warning,
        Message = "Failed to calculate field {fieldName} in dataset {dataSetName}: {errorMessage}")]
    public static partial IGenericMessage CalculatedFieldFailed(
        ILogger logger,
        string fieldName,
        string dataSetName,
        string errorMessage);

    /// <summary>
    /// Logs when type conversion fails for calculated results.
    /// </summary>
    [MessageLogging(
        EventId = 91004,
        Level = LogLevel.Error,
        Message = "Cannot convert calculated results to type {typeName}. For DataSets with calculated fields, query as IEnumerable<Dictionary<string, object?>> or IEnumerable<object>")]
    public static partial IGenericMessage CalculatedResultConversionFailed(
        ILogger logger,
        string typeName);

    // Dataset error logging (1024-1035)

    /// <summary>
    /// Logs when a dataset is not found.
    /// </summary>
    [MessageLogging(
        EventId = 31006,
        Level = LogLevel.Error,
        Message = "Dataset '{dataSetName}' not found: {reason}")]
    public static partial IGenericMessage DataSetNotFound(
        ILogger logger,
        string dataSetName,
        string reason);

    /// <summary>
    /// Logs when dataset validation fails.
    /// </summary>
    [MessageLogging(
        EventId = 21000,
        Level = LogLevel.Error,
        Message = "Dataset '{dataSetName}' validation failed: {reason}")]
    public static partial IGenericMessage DataSetValidationFailed(
        ILogger logger,
        string dataSetName,
        string reason);

    /// <summary>
    /// Logs when source resolution fails for a dataset.
    /// </summary>
    [MessageLogging(
        EventId = 91005,
        Level = LogLevel.Error,
        Message = "Failed to resolve sources for dataset '{dataSetName}': {reason}")]
    public static partial IGenericMessage SourceResolutionFailed(
        ILogger logger,
        string dataSetName,
        string reason);

    /// <summary>
    /// Logs when a dataset has no sources configured.
    /// </summary>
    [MessageLogging(
        EventId = 21006,
        Level = LogLevel.Error,
        Message = "Dataset '{dataSetName}' has no sources configured")]
    public static partial IGenericMessage DataSetNoSources(
        ILogger logger,
        string dataSetName);

    /// <summary>
    /// Logs when filter translation fails.
    /// </summary>
    [MessageLogging(
        EventId = 91006,
        Level = LogLevel.Error,
        Message = "Filter translation failed for source '{sourceName}': {reason}")]
    public static partial IGenericMessage FilterTranslationFailed(
        ILogger logger,
        string sourceName,
        string reason);

    /// <summary>
    /// Logs when filter decomposition fails.
    /// </summary>
    [MessageLogging(
        EventId = 90002,
        Level = LogLevel.Error,
        Message = "Filter decomposition failed for dataset '{dataSetName}': {reason}")]
    public static partial IGenericMessage FilterDecompositionFailed(
        ILogger logger,
        string dataSetName,
        string reason);

    /// <summary>
    /// Logs when one or more source queries fail in federated execution.
    /// </summary>
    [MessageLogging(
        EventId = 71008,
        Level = LogLevel.Error,
        Message = "Source query failures in dataset '{dataSetName}': {errors}")]
    public static partial IGenericMessage SourceQueryFailures(
        ILogger logger,
        string dataSetName,
        string errors);

    /// <summary>
    /// Logs when a join references an unknown source.
    /// </summary>
    [MessageLogging(
        EventId = 31007,
        Level = LogLevel.Error,
        Message = "Join in dataset '{dataSetName}' references unknown source '{sourceName}'")]
    public static partial IGenericMessage JoinSourceNotFound(
        ILogger logger,
        string dataSetName,
        string sourceName);

    /// <summary>
    /// Logs when federated dataset execution fails with an exception.
    /// </summary>
    [MessageLogging(
        EventId = 91007,
        Level = LogLevel.Error,
        Message = "Federated dataset '{dataSetName}' execution failed")]
    public static partial IGenericMessage FederatedExecutionException(
        ILogger logger,
        Exception exception,
        string dataSetName);

    /// <summary>
    /// Logs when a source has no container configured.
    /// </summary>
    [MessageLogging(
        EventId = 41001,
        Level = LogLevel.Error,
        Message = "Source '{sourceName}' in dataset '{dataSetName}' has no container configured")]
    public static partial IGenericMessage SourceNoContainer(
        ILogger logger,
        string sourceName,
        string dataSetName);

    /// <summary>
    /// Logs federated dataset execution completion with metrics.
    /// </summary>
    [MessageLogging(
        EventId = 11036,
        Level = LogLevel.Information,
        Message = "Federated dataset '{dataSetName}' completed: {recordCount} records in {totalMs}ms (query: {queryMs}ms, join: {joinMs}ms)")]
    public static partial IGenericMessage FederatedExecutionCompleted(
        ILogger logger,
        string dataSetName,
        int recordCount,
        double totalMs,
        double queryMs,
        double joinMs);

    /// <summary>
    /// Logs a warning when a dataset has multiple sources but no joins defined.
    /// </summary>
    [MessageLogging(
        EventId = 41002,
        Level = LogLevel.Warning,
        Message = "Dataset '{dataSetName}' has {sourceCount} sources but no joins - using Cartesian product")]
    public static partial IGenericMessage DataSetNoJoins(
        ILogger logger,
        string dataSetName,
        int sourceCount);

    /// <summary>
    /// Logs when no DataStoreType is found for a given store type name.
    /// </summary>
    [MessageLogging(
        EventId = 31009,
        Level = LogLevel.Warning,
        Message = "No DataStoreType found for '{storeTypeName}'. Container cannot be built.")]
    public static partial IGenericMessage NoDataStoreTypeFound(
        ILogger logger,
        string storeTypeName);

    /// <summary>
    /// Logs when a DataStore configuration is missing ServiceOptionType.
    /// </summary>
    [MessageLogging(
        EventId = 61008,
        Level = LogLevel.Error,
        Message = "DataStore '{dataStoreName}' has no ServiceOptionType configured. Cannot determine store type for container building.")]
    public static partial IGenericMessage DataStoreMissingServiceOptionType(
        ILogger logger,
        string dataStoreName);

    /// <summary>
    /// Logs when a DataStore is not found for a source during container building.
    /// </summary>
    [MessageLogging(
        EventId = 31008,
        Level = LogLevel.Error,
        Message = "DataStore '{dataStoreName}' not found in configuration. Cannot build container from source.")]
    public static partial IGenericMessage DataStoreNotFoundForSource(
        ILogger logger,
        string dataStoreName);

    /// <summary>
    /// Logs when access is denied to a container that requires a specific permission.
    /// </summary>
    [MessageLogging(
        EventId = 51000,
        Level = LogLevel.Warning,
        Message = "Access denied to container '{containerName}' — requires permission '{requiredPermission}'")]
    public static partial IGenericMessage ContainerAccessDenied(
        ILogger logger,
        string containerName,
        string requiredPermission);

    /// <summary>
    /// Logs when the DataStore was resolved but has no ConnectionId (empty Guid).
    /// </summary>
    // Why: ConnectionId == Guid.Empty means the DataStore was built without a connection binding —
    // a data integrity failure that must fail loud rather than fall through to a name lookup.
    [MessageLogging(
        EventId = 41003,
        Level = LogLevel.Error,
        Message = "DataStore '{dataStoreName}' has no ConnectionId. Cannot route container command.")]
    public static partial IGenericMessage DataStoreHasNoConnectionId(
        ILogger logger,
        string dataStoreName);

    /// <summary>
    /// Logs when the connection referenced by DataStore.ConnectionId cannot be resolved from the provider.
    /// </summary>
    [MessageLogging(
        EventId = 31010,
        Level = LogLevel.Error,
        Message = "Connection for DataStore '{dataStoreName}' (ConnectionId={connectionId}) could not be resolved: {reason}")]
    public static partial IGenericMessage DataStoreConnectionNotResolved(
        ILogger logger,
        string dataStoreName,
        string connectionId,
        string reason);

    /// <summary>
    /// Logs the start of opening a streaming record-source cursor over a container.
    /// </summary>
    [MessageLogging(
        EventId = 11037,
        Level = LogLevel.Debug,
        Message = "Opening record-source cursor on container '{container}' in DataStore '{dataStore}'")]
    public static partial IGenericMessage OpeningRecordSource(
        ILogger logger,
        string dataStore,
        string container);

    /// <summary>
    /// Logs when the resolved connection cannot stream a record-source cursor.
    /// </summary>
    // Why: fail loud — a caller that asked for a streaming cursor must not silently receive a
    // materialized result. The connection type lacks IRecordSourceConnection; the caller falls back
    // to the materializing Execute path only on this explicit non-success.
    [MessageLogging(
        EventId = 61009,
        Level = LogLevel.Error,
        Message = "Connection for DataStore '{dataStoreName}' does not support streaming record sources (IRecordSourceConnection).")]
    public static partial IGenericMessage RecordSourceNotSupported(
        ILogger logger,
        string dataStoreName);

    /// <summary>
    /// Logs when a streaming record-source cursor was opened successfully.
    /// </summary>
    [MessageLogging(
        EventId = 11038,
        Level = LogLevel.Debug,
        Message = "Record-source cursor opened on container '{container}' in DataStore '{dataStore}'")]
    public static partial IGenericMessage RecordSourceOpened(
        ILogger logger,
        string dataStore,
        string container);

    // Federated dataset execution logging (5200-5279)

    /// <summary>
    /// Logs the start of federated dataset execution.
    /// </summary>
    [MessageLogging(
        EventId = 11039,
        Level = LogLevel.Information,
        Message = "Executing distributed dataset '{dataSetName}' across {sourceCount} sources")]
    public static partial IGenericMessage ExecutingDistributedDataSetInternal(
        ILogger logger,
        string dataSetName,
        int sourceCount);

    /// <summary>
    /// Logs the count of decomposed filters.
    /// </summary>
    [MessageLogging(
        EventId = 11040,
        Level = LogLevel.Debug,
        Message = "Filter decomposed into {filterCount} source-specific filters")]
    public static partial IGenericMessage FilterDecomposed(
        ILogger logger,
        int filterCount);

    /// <summary>
    /// Logs the query built for a source.
    /// </summary>
    [MessageLogging(
        EventId = 11041,
        Level = LogLevel.Debug,
        Message = "Built query for source '{sourceName}': Container='{containerName}', Connection='{connectionName}', HasFilter={hasFilter}")]
    public static partial IGenericMessage BuiltSourceQuery(
        ILogger logger,
        string sourceName,
        string containerName,
        string connectionName,
        bool hasFilter);

    /// <summary>
    /// Logs when a source query is being executed.
    /// </summary>
    [MessageLogging(
        EventId = 11042,
        Level = LogLevel.Debug,
        Message = "Executing source query: Source='{sourceName}', Container='{containerName}'")]
    public static partial IGenericMessage ExecutingSourceQuery(
        ILogger logger,
        string sourceName,
        string containerName);

    /// <summary>
    /// Logs when all source queries complete.
    /// </summary>
    [MessageLogging(
        EventId = 11043,
        Level = LogLevel.Information,
        Message = "All source queries completed in {totalMs}ms (parallel execution)")]
    public static partial IGenericMessage SourceQueriesCompleted(
        ILogger logger,
        double totalMs);

    /// <summary>
    /// Logs join operation details.
    /// </summary>
    [MessageLogging(
        EventId = 11044,
        Level = LogLevel.Debug,
        Message = "Joined '{leftSource}'.'{leftField}' with '{rightSource}'.'{rightField}' using {joinType} join - Result count: {count}")]
    public static partial IGenericMessage JoinCompleted(
        ILogger logger,
        string leftSource,
        string leftField,
        string rightSource,
        string rightField,
        string joinType,
        int count);

    // Trace/Debug logging for execution flow (5206-5215)

    /// <summary>
    /// Logs entry into container execution with resolution strategy.
    /// </summary>
    [LoggerMessage(
        EventId = 5206,
        Level = LogLevel.Trace,
        Message = "ExecuteContainer entering: DataStore='{DataStoreName}', Path='{PathName}', Container='{ContainerName}'")]
    public static partial void ExecuteContainerEntering(
        ILogger logger,
        string dataStoreName,
        string? pathName,
        string containerName);

    /// <summary>
    /// Logs entry into source query execution.
    /// </summary>
    [LoggerMessage(
        EventId = 5207,
        Level = LogLevel.Trace,
        Message = "ExecuteSourceQuery entering: Source='{SourceName}', Connection='{ConnectionName}'")]
    public static partial void ExecuteSourceQueryEntering(
        ILogger logger,
        string sourceName,
        string connectionName);

    /// <summary>
    /// Logs the resolved DataStore type during container building.
    /// </summary>
    [LoggerMessage(
        EventId = 5208,
        Level = LogLevel.Debug,
        Message = "Resolved DataStoreType '{StoreTypeName}' for DataStore '{DataStoreName}'")]
    public static partial void DataStoreTypeResolved(
        ILogger logger,
        string storeTypeName,
        string dataStoreName);

    /// <summary>
    /// Logs when field mappings are resolved for a dataset source.
    /// </summary>
    [LoggerMessage(
        EventId = 5209,
        Level = LogLevel.Trace,
        Message = "Resolved {MappingCount} field mappings for source '{SourceName}'")]
    public static partial void FieldMappingsResolved(
        ILogger logger,
        int mappingCount,
        string sourceName);

    /// <summary>
    /// Logs source query result with row count.
    /// </summary>
    [LoggerMessage(
        EventId = 5210,
        Level = LogLevel.Debug,
        Message = "Source query for '{SourceName}' returned in {ElapsedMs}ms")]
    public static partial void SourceQueryCompleted(
        ILogger logger,
        string sourceName,
        double elapsedMs);

    // ═══════════════════════════════════════════════════════════════════════════
    // Transaction Events (5211-5214)
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Logs when a connection does not support transactions.
    /// </summary>
    [MessageLogging(
        EventId = 61010,
        Level = LogLevel.Error,
        Message = "Connection '{connectionName}' does not support transactions")]
    public static partial IGenericMessage TransactionNotSupported(ILogger logger, string connectionName);

    /// <summary>
    /// Logs when beginning a transaction fails.
    /// </summary>
    [MessageLogging(
        EventId = 71009,
        Level = LogLevel.Error,
        Message = "Failed to begin transaction on connection '{connectionName}': {reason}")]
    public static partial IGenericMessage BeginTransactionFailed(ILogger logger, string connectionName, string reason);

    /// <summary>
    /// Traces when a transaction scope is opened via the DataGateway.
    /// </summary>
    [LoggerMessage(
        EventId = 5213,
        Level = LogLevel.Trace,
        Message = "Transaction scope opened on connection '{ConnectionName}'")]
    public static partial void TransactionScopeOpened(ILogger logger, string connectionName);

    /// <summary>
    /// Logs when a command is executed with a mismatched connection in a transaction scope.
    /// </summary>
    [MessageLogging(
        EventId = 41004,
        Level = LogLevel.Error,
        Message = "Transaction was opened on connection '{transactionConnectionName}' but command targets '{commandConnectionName}'")]
    public static partial IGenericMessage TransactionConnectionMismatch(
        ILogger logger,
        string transactionConnectionName,
        string commandConnectionName);

    /// <summary>
    /// Logs when field-name renaming (physical→logical) is applied to source rows.
    /// </summary>
    [MessageLogging(
        EventId = 11045,
        Level = LogLevel.Debug,
        Message = "Applying {mappingCount} physical→logical field renames on rows from source '{sourceName}'")]
    public static partial IGenericMessage ApplyingFieldRename(
        ILogger logger,
        int mappingCount,
        string sourceName);

    /// <summary>
    /// Logs when a non-query (write) command targets a DataSet that does not resolve to exactly
    /// one source — a write has no unambiguous target across multiple sources, so it fails loud.
    /// </summary>
    [MessageLogging(
        EventId = 41005,
        Level = LogLevel.Error,
        Message = "Write command '{commandType}' targets DataSet '{dataSetName}' which has {sourceCount} sources; a write requires exactly one source")]
    public static partial IGenericMessage DataSetWriteRequiresSingleSource(
        ILogger logger,
        string commandType,
        string dataSetName,
        int sourceCount);

    /// <summary>
    /// Logs when a dataset strategy receives an execution context of an unexpected concrete type
    /// (it cannot read the providers it needs). Fail loud — no fallback.
    /// </summary>
    [MessageLogging(
        EventId = 11046,
        Level = LogLevel.Error,
        Message = "DataSet '{dataSetName}' strategy received an unsupported execution context type '{contextType}'")]
    public static partial IGenericMessage DataSetContextInvalid(
        ILogger logger,
        string dataSetName,
        string contextType);

    /// <summary>
    /// Logs when a Compound dataset's sources span more than one data store (Compound pushes the join
    /// down to a single store, so cross-store sources are invalid — fail loud).
    /// </summary>
    [MessageLogging(
        EventId = 11047,
        Level = LogLevel.Error,
        Message = "Compound DataSet '{dataSetName}' has sources spanning {storeCount} data stores; a compound (pushed-down) join requires a single store")]
    public static partial IGenericMessage CompoundSourcesSpanStores(
        ILogger logger,
        string dataSetName,
        int storeCount);

    /// <summary>
    /// Logs when a Compound dataset cannot push its join down to the resolved store (the connection
    /// does not advertise the compound-query capability). Fail loud — never degrade to in-memory.
    /// </summary>
    [MessageLogging(
        EventId = 11048,
        Level = LogLevel.Error,
        Message = "Compound DataSet '{dataSetName}' cannot push its join down to store '{dataStore}': {reason}")]
    public static partial IGenericMessage CompoundPushdownUnavailable(
        ILogger logger,
        string dataSetName,
        string dataStore,
        string reason);

    /// <summary>
    /// Logs when a Federated dataset has no resolvable federation strategy (the configured strategy
    /// name is missing or not a registered FederationStrategies member). Fail loud — no default.
    /// </summary>
    [MessageLogging(
        EventId = 11049,
        Level = LogLevel.Error,
        Message = "Federated DataSet '{dataSetName}' has no resolvable federation strategy: {reason}")]
    public static partial IGenericMessage FederationStrategyMissing(
        ILogger logger,
        string dataSetName,
        string reason);

    /// <summary>
    /// Logs when a Federated source's connection does not advertise the record-source capability
    /// (<c>IRecordSourceConnection</c>), so the cross-store in-memory join cannot pull it as
    /// <c>DataRecord</c>. Fail loud — no fallback to a materializing path (NO FALLBACKS).
    /// </summary>
    [MessageLogging(
        EventId = 11050,
        Level = LogLevel.Error,
        Message = "Federated DataSet '{dataSetName}' source '{sourceName}' connection '{connectionName}' does not support record-source streaming (IRecordSourceConnection); a federated in-memory join requires record-source-capable connections")]
    public static partial IGenericMessage FederatedSourceNotRecordCapable(
        ILogger logger,
        string dataSetName,
        string sourceName,
        string connectionName);

    /// <summary>
    /// Logs when reading a record from a Federated source's record-source cursor failed (a per-record
    /// parse/convert error surfaced as a failed result). Fail loud — do not silently drop the record.
    /// </summary>
    [MessageLogging(
        EventId = 11051,
        Level = LogLevel.Error,
        Message = "Federated DataSet '{dataSetName}' source '{sourceName}' failed to read a record: {reason}")]
    public static partial IGenericMessage FederatedRecordReadFailed(
        ILogger logger,
        string dataSetName,
        string sourceName,
        string reason);
}
