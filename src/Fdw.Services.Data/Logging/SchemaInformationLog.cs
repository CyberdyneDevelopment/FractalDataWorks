using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Data.Logging;

/// <summary>
/// Source-generated logging methods for SchemaInformationService.
/// EventId range: 8800-8820
/// </summary>
[MessageLoggingTypeCode("DATA")]
public static partial class SchemaInformationLog
{
    // ═══════════════════════════════════════════════════════════════════════════
    // GetSchema / Cache hits (8800-8804)
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Traces when GetSchema begins.
    /// </summary>
    [MessageLogging(EventId = 11253, Level = LogLevel.Trace, Message = "GetSchema called for connection '{connectionName}'")]
    public static partial IGenericMessage GetSchemaStarted(ILogger logger, string connectionName);

    /// <summary>
    /// Logs when a cached DataStore is found and returned without rediscovery.
    /// </summary>
    [MessageLogging(EventId = 11254, Level = LogLevel.Debug, Message = "Returning cached schema for connection '{connectionName}' (DataStore '{dataStoreName}')")]
    public static partial IGenericMessage CacheHit(ILogger logger, string connectionName, string dataStoreName);

    /// <summary>
    /// Traces when no cached DataStore exists and discovery will proceed.
    /// </summary>
    [MessageLogging(EventId = 11255, Level = LogLevel.Trace, Message = "No cached DataStore found for connection '{connectionName}', starting discovery")]
    public static partial IGenericMessage CacheMiss(ILogger logger, string connectionName);

    /// <summary>
    /// Traces when RefreshSchema begins (cache is intentionally bypassed).
    /// </summary>
    [MessageLogging(EventId = 11256, Level = LogLevel.Trace, Message = "RefreshSchema called for connection '{connectionName}' — bypassing cache")]
    public static partial IGenericMessage RefreshStarted(ILogger logger, string connectionName);

    // ═══════════════════════════════════════════════════════════════════════════
    // Discovery flow (8805-8812)
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Logs when schema discovery is disabled for a connection.
    /// </summary>
    [MessageLogging(EventId = 11257, Level = LogLevel.Warning, Message = "Schema discovery is disabled for connection '{connectionName}' — returning failure")]
    public static partial IGenericMessage DiscoveryDisabled(ILogger logger, string connectionName);

    /// <summary>
    /// Logs when the connection's type does not support schema discovery.
    /// </summary>
    [MessageLogging(EventId = 61022, Level = LogLevel.Warning, Message = "Connection type '{connectionType}' for '{connectionName}' does not implement ISchemaDiscovery")]
    public static partial IGenericMessage ConnectionTypeNotDiscoverable(ILogger logger, string connectionName, string connectionType);

    /// <summary>
    /// Logs when the connection configuration cannot be resolved by name.
    /// </summary>
    [MessageLogging(EventId = 31039, Level = LogLevel.Error, Message = "Connection configuration '{connectionName}' not found")]
    public static partial IGenericMessage ConnectionConfigNotFound(ILogger logger, string connectionName);

    /// <summary>
    /// Logs when the connection has no ServiceOptionType set.
    /// </summary>
    [MessageLogging(EventId = 61023, Level = LogLevel.Error, Message = "Connection '{connectionName}' has no ServiceOptionType — cannot resolve connection type for discovery")]
    public static partial IGenericMessage ConnectionTypeMissing(ILogger logger, string connectionName);

    /// <summary>
    /// Traces when discovery begins for a connection.
    /// </summary>
    [MessageLogging(EventId = 11258, Level = LogLevel.Trace, Message = "Starting schema discovery for connection '{connectionName}' (type: {connectionType})")]
    public static partial IGenericMessage DiscoveryStarting(ILogger logger, string connectionName, string connectionType);

    /// <summary>
    /// Logs when discovery succeeds and the schema is returned.
    /// </summary>
    [MessageLogging(EventId = 11259, Level = LogLevel.Information, Message = "Schema discovery succeeded for connection '{connectionName}' — DataStore '{dataStoreName}'")]
    public static partial IGenericMessage DiscoverySucceeded(ILogger logger, string connectionName, string dataStoreName);

    /// <summary>
    /// Logs when discovery fails via the orchestrator.
    /// </summary>
    [MessageLogging(EventId = 71046, Level = LogLevel.Error, Message = "Schema discovery failed for connection '{connectionName}': {error}")]
    public static partial IGenericMessage DiscoveryFailed(ILogger logger, string connectionName, string error);

    /// <summary>
    /// Logs when discovery fails with no upstream error details.
    /// </summary>
    [MessageLogging(EventId = 71047, Level = LogLevel.Error, Message = "Schema discovery failed for connection '{connectionName}' with no upstream error details")]
    public static partial IGenericMessage DiscoveryFailedNoDetails(ILogger logger, string connectionName);

    // ═══════════════════════════════════════════════════════════════════════════
    // Post-discovery DataStore resolution (8815-8820)
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Logs when the DataStore cannot be found in cache after successful discovery.
    /// </summary>
    [MessageLogging(EventId = 31040, Level = LogLevel.Error, Message = "DataStore not found in cache after discovery for connection '{connectionName}'")]
    public static partial IGenericMessage DataStoreNotFoundAfterDiscovery(ILogger logger, string connectionName);

    /// <summary>
    /// Logs when the connection cannot be built for discovery.
    /// </summary>
    [MessageLogging(EventId = 71048, Level = LogLevel.Error, Message = "Failed to build connection '{connectionName}' for discovery: {error}")]
    public static partial IGenericMessage ConnectionBuildFailed(ILogger logger, string connectionName, string error);

    /// <summary>
    /// Logs when the connection cannot be built with no upstream error details.
    /// </summary>
    [MessageLogging(EventId = 71049, Level = LogLevel.Error, Message = "Failed to build connection '{connectionName}' for discovery with no upstream error details")]
    public static partial IGenericMessage ConnectionBuildFailedNoDetails(ILogger logger, string connectionName);
}
