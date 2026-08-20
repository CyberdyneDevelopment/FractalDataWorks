using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Data.Logging;

/// <summary>
/// Source-generated logging methods for schema discovery operations (used by SchemaInformationService).
/// EventId range: 7001-7006 (original), 7010-7039 (trace + specific errors), 7090-7094 (auto-persist)
/// </summary>
[MessageLoggingTypeCode("DATA")]
public static partial class SchemaDiscoveryLog
{
    // ═══════════════════════════════════════════════════════════════════════════
    // Core Setup Operations (7001-7006)
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Logs when connection setup starts.
    /// </summary>
    [MessageLogging(EventId = 11232, Level = LogLevel.Information, Message = "Connection setup started for '{connectionName}'")]
    public static partial IGenericMessage SetupStarted(ILogger logger, string connectionName);

    /// <summary>
    /// Logs when a connection test passes.
    /// </summary>
    [MessageLogging(EventId = 11233, Level = LogLevel.Information, Message = "Connection test passed for '{connectionName}'")]
    public static partial IGenericMessage ConnectionTestPassed(ILogger logger, string connectionName);

    /// <summary>
    /// Logs when a connection test fails.
    /// </summary>
    [MessageLogging(EventId = 71036, Level = LogLevel.Error, Message = "Connection test failed for '{connectionName}': {error}")]
    public static partial IGenericMessage ConnectionTestFailed(ILogger logger, string connectionName, string error);

    /// <summary>
    /// Logs when schema discovery completes.
    /// </summary>
    [MessageLogging(EventId = 11234, Level = LogLevel.Information, Message = "Schema discovery completed: {tableCount} tables, {viewCount} views")]
    public static partial IGenericMessage DiscoveryCompleted(ILogger logger, int tableCount, int viewCount);

    /// <summary>
    /// Logs when a DataStore is created for a connection.
    /// </summary>
    [MessageLogging(EventId = 11235, Level = LogLevel.Information, Message = "DataStore '{dataStoreName}' created for connection '{connectionName}'")]
    public static partial IGenericMessage DataStoreCreated(ILogger logger, string dataStoreName, string connectionName);

    /// <summary>
    /// Logs when connection setup fails.
    /// </summary>
    [MessageLogging(EventId = 71037, Level = LogLevel.Error, Message = "Connection setup failed for '{connectionName}': {error}")]
    public static partial IGenericMessage SetupFailed(ILogger logger, string connectionName, string error);

    // ═══════════════════════════════════════════════════════════════════════════
    // Trace Operations (7010-7022)
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Traces connection testing.
    /// </summary>
    [MessageLogging(EventId = 11236, Level = LogLevel.Trace, Message = "Testing connection '{connectionName}'")]
    public static partial IGenericMessage TestingConnection(ILogger logger, string connectionName);

    /// <summary>
    /// Traces when a connection test is skipped.
    /// </summary>
    [MessageLogging(EventId = 11237, Level = LogLevel.Trace, Message = "Connection test skipped for '{connectionName}'")]
    public static partial IGenericMessage ConnectionTestSkipped(ILogger logger, string connectionName);

    /// <summary>
    /// Traces schema discoverer resolution.
    /// </summary>
    [MessageLogging(EventId = 11238, Level = LogLevel.Trace, Message = "Resolving schema discoverer for connection type '{connectionType}'")]
    public static partial IGenericMessage ResolvingDiscoverer(ILogger logger, string connectionType);

    /// <summary>
    /// Traces DataStore resolution.
    /// </summary>
    [MessageLogging(EventId = 11239, Level = LogLevel.Trace, Message = "Resolving DataStore '{dataStoreName}'")]
    public static partial IGenericMessage ResolvingDataStore(ILogger logger, string dataStoreName);

    /// <summary>
    /// Traces the start of container discovery.
    /// </summary>
    [MessageLogging(EventId = 11240, Level = LogLevel.Trace, Message = "Starting container discovery for connection '{connectionName}' using DataStore '{dataStoreName}'")]
    public static partial IGenericMessage StartingContainerDiscovery(ILogger logger, string connectionName, string dataStoreName);

    /// <summary>
    /// Logs the number of containers discovered.
    /// </summary>
    [MessageLogging(EventId = 11241, Level = LogLevel.Debug, Message = "Discovered {containerCount} containers for connection '{connectionName}'")]
    public static partial IGenericMessage ContainersDiscovered(ILogger logger, int containerCount, string connectionName);

    /// <summary>
    /// Traces DataStore configuration persistence.
    /// </summary>
    [MessageLogging(EventId = 11242, Level = LogLevel.Trace, Message = "Persisting DataStore configuration for '{dataStoreName}' with connectionId={connectionId}")]
    public static partial IGenericMessage PersistingDataStore(ILogger logger, string dataStoreName, System.Guid connectionId);

    /// <summary>
    /// Traces DataPath persistence.
    /// </summary>
    [MessageLogging(EventId = 11243, Level = LogLevel.Trace, Message = "Persisting DataPath '{pathName}' for DataStore '{dataStoreName}'")]
    public static partial IGenericMessage PersistingDataPath(ILogger logger, string pathName, string dataStoreName);

    /// <summary>
    /// Traces container persistence.
    /// </summary>
    [MessageLogging(EventId = 11244, Level = LogLevel.Trace, Message = "Persisting container '{containerName}' in path '{pathName}'")]
    public static partial IGenericMessage PersistingContainer(ILogger logger, string containerName, string pathName);

    /// <summary>
    /// Traces field persistence.
    /// </summary>
    [MessageLogging(EventId = 11245, Level = LogLevel.Trace, Message = "Persisting {fieldCount} fields for container '{containerName}'")]
    public static partial IGenericMessage PersistingFields(ILogger logger, int fieldCount, string containerName);

    /// <summary>
    /// Traces configuration writer resolution.
    /// </summary>
    [MessageLogging(EventId = 11246, Level = LogLevel.Trace, Message = "Resolving configuration writers for persistence")]
    public static partial IGenericMessage ResolvingConfigurationWriters(ILogger logger);

    /// <summary>
    /// Traces LastDiscoveredAt update.
    /// </summary>
    [MessageLogging(EventId = 11247, Level = LogLevel.Trace, Message = "Updating LastDiscoveredAt for DataStore '{dataStoreName}'")]
    public static partial IGenericMessage UpdatingLastDiscoveredAt(ILogger logger, string dataStoreName);

    /// <summary>
    /// Logs when connection setup completes with timing.
    /// </summary>
    [MessageLogging(EventId = 11248, Level = LogLevel.Debug, Message = "Connection setup completed for '{connectionName}' in {elapsedMs:F1}ms")]
    public static partial IGenericMessage SetupCompleted(ILogger logger, string connectionName, double elapsedMs);

    // ═══════════════════════════════════════════════════════════════════════════
    // Specific Error Methods (7030-7038) — replace ?? "fallback" patterns
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Logs when a connection test fails with no upstream error details.
    /// </summary>
    [MessageLogging(EventId = 71038, Level = LogLevel.Error, Message = "Connection test failed for '{connectionName}' with no upstream error details")]
    public static partial IGenericMessage ConnectionTestFailedNoDetails(ILogger logger, string connectionName);

    /// <summary>
    /// Logs when no schema discoverer is available for a connection type.
    /// </summary>
    [MessageLogging(EventId = 60002, Level = LogLevel.Error, Message = "No schema discoverer available for connection type '{connectionType}'")]
    public static partial IGenericMessage DiscovererNotFound(ILogger logger, string connectionType);

    /// <summary>
    /// Logs when a DataStore is not found during connection setup.
    /// </summary>
    [MessageLogging(EventId = 31038, Level = LogLevel.Error, Message = "DataStore '{dataStoreName}' not found during connection setup")]
    public static partial IGenericMessage DataStoreNotFound(ILogger logger, string dataStoreName);

    /// <summary>
    /// Logs when container discovery fails with no upstream error details.
    /// </summary>
    [MessageLogging(EventId = 71039, Level = LogLevel.Error, Message = "Container discovery failed for connection '{connectionName}' with no upstream error details")]
    public static partial IGenericMessage ContainerDiscoveryFailedNoDetails(ILogger logger, string connectionName);

    /// <summary>
    /// Logs when configuration persistence fails with no upstream error details.
    /// </summary>
    [MessageLogging(EventId = 71040, Level = LogLevel.Error, Message = "Configuration persistence failed for connection '{connectionName}' with no upstream error details")]
    public static partial IGenericMessage PersistenceFailedNoDetails(ILogger logger, string connectionName);

    /// <summary>
    /// Logs when saving a DataStore configuration fails.
    /// </summary>
    [MessageLogging(EventId = 71041, Level = LogLevel.Error, Message = "Failed to save DataStore configuration for '{dataStoreName}'")]
    public static partial IGenericMessage DataStoreSaveFailed(ILogger logger, string dataStoreName);

    /// <summary>
    /// Logs when saving a DataPath fails.
    /// </summary>
    [MessageLogging(EventId = 71042, Level = LogLevel.Error, Message = "Failed to save DataPath '{pathName}' for DataStore '{dataStoreName}'")]
    public static partial IGenericMessage DataPathSaveFailed(ILogger logger, string pathName, string dataStoreName);

    /// <summary>
    /// Logs when persisting containers fails.
    /// </summary>
    [MessageLogging(EventId = 71043, Level = LogLevel.Error, Message = "Failed to persist containers for DataStore '{dataStoreName}'")]
    public static partial IGenericMessage ContainerPersistFailed(ILogger logger, string dataStoreName);

    /// <summary>
    /// Logs when updating LastDiscoveredAt timestamp fails.
    /// </summary>
    [MessageLogging(EventId = 71044, Level = LogLevel.Error, Message = "Failed to update LastDiscoveredAt for DataStore '{dataStoreName}'")]
    public static partial IGenericMessage LastDiscoveredAtUpdateFailed(ILogger logger, string dataStoreName);

    // ═══════════════════════════════════════════════════════════════════════════
    // Auto-Persist Operations (7090-7094)
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Logs when discovery result persistence starts.
    /// </summary>
    [MessageLogging(EventId = 11249, Level = LogLevel.Information, Message = "Persisting discovery results for DataStore '{dataStoreName}': {containerCount} containers across {pathCount} paths")]
    public static partial IGenericMessage PersistStarted(ILogger logger, string dataStoreName, int containerCount, int pathCount);

    /// <summary>
    /// Logs when discovery result persistence completes.
    /// </summary>
    [MessageLogging(EventId = 11250, Level = LogLevel.Information, Message = "Discovery results persisted for DataStore '{dataStoreName}': {pathsWritten} paths, {containersWritten} containers, {fieldsWritten} fields")]
    public static partial IGenericMessage PersistCompleted(ILogger logger, string dataStoreName, int pathsWritten, int containersWritten, int fieldsWritten);

    /// <summary>
    /// Logs when an existing DataStore is found during persistence.
    /// </summary>
    [MessageLogging(EventId = 11251, Level = LogLevel.Trace, Message = "Existing DataStore '{dataStoreName}' found (Id={dataStoreId}), updating configuration")]
    public static partial IGenericMessage ExistingDataStoreFound(ILogger logger, string dataStoreName, System.Guid dataStoreId);

    /// <summary>
    /// Logs when discovery result persistence fails.
    /// </summary>
    [MessageLogging(EventId = 71045, Level = LogLevel.Error, Message = "Failed to persist configuration for DataStore '{dataStoreName}': {error}")]
    public static partial IGenericMessage PersistFailed(ILogger logger, string dataStoreName, string error);

    /// <summary>
    /// Logs when LastDiscoveredAt timestamp is updated.
    /// </summary>
    [MessageLogging(EventId = 11252, Level = LogLevel.Debug, Message = "Updated LastDiscoveredAt on DataStore '{dataStoreName}' to {timestamp}")]
    public static partial IGenericMessage LastDiscoveredAtUpdated(ILogger logger, string dataStoreName, System.DateTimeOffset timestamp);
}
