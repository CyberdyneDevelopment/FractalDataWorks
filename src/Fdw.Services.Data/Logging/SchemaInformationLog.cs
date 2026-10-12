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
    /// Logs a discovery refused because the connection chose not to discover (its option is <c>None</c>).
    /// </summary>
    /// <remarks>Category 4 (state): the request is well formed, but this connection is not one that discovers.</remarks>
    [MessageLogging(EventId = 41011, Level = LogLevel.Warning, Message = "Connection '{connectionName}' does not discover its schema (schema discovery option '{option}'); declare its containers by hand, or choose a discovery option on the connection")]
    public static partial IGenericMessage ConnectionDoesNotDiscover(ILogger logger, string connectionName, string option);

    /// <summary>
    /// Logs a connection type that declares a discovering option but performs no discovery: a registration defect.
    /// </summary>
    [MessageLogging(EventId = 61037, Level = LogLevel.Error, Message = "Connection type '{connectionType}' offers schema discovery option '{option}' to connection '{connectionName}' but performs no schema discovery (it does not implement ISchemaDiscovery)")]
    public static partial IGenericMessage ConnectionTypeNotDiscoverable(ILogger logger, string connectionName, string connectionType, string option);

    /// <summary>
    /// Logs a connection whose Implementation no registered connection type answers.
    /// </summary>
    [MessageLogging(EventId = 61035, Level = LogLevel.Error, Message = "Connection '{connectionName}' names implementation '{connectionType}', which no registered connection type answers, so its schema cannot be discovered")]
    public static partial IGenericMessage ConnectionTypeUnknown(ILogger logger, string connectionName, string connectionType);

    /// <summary>
    /// Logs a connection whose schema discovery option is not a registered option.
    /// </summary>
    [MessageLogging(EventId = 61033, Level = LogLevel.Error, Message = "Connection '{connectionName}' names schema discovery option '{option}', which is not a registered schema discovery option")]
    public static partial IGenericMessage DiscoveryOptionUnknown(ILogger logger, string connectionName, string option);

    /// <summary>
    /// Logs a connection whose schema discovery option its connection type does not offer.
    /// </summary>
    [MessageLogging(EventId = 61034, Level = LogLevel.Error, Message = "Connection '{connectionName}' names schema discovery option '{option}', which connection type '{connectionType}' does not offer")]
    public static partial IGenericMessage DiscoveryOptionNotOffered(ILogger logger, string connectionName, string option, string connectionType);

    /// <summary>
    /// Traces the discovery option a connection discovers with.
    /// </summary>
    [MessageLogging(EventId = 11267, Level = LogLevel.Trace, Message = "Connection '{connectionName}' ({connectionType}) discovers with schema discovery option '{option}'")]
    public static partial IGenericMessage DiscoveryOptionResolved(ILogger logger, string connectionName, string connectionType, string option);

    /// <summary>
    /// Logs a discovery that ran and found no containers: the source is empty or excluded by the options.
    /// </summary>
    // Why Information, not Warning: an empty source is a real state, not a fault, and the caller is
    // told the count; only a connection that cannot discover is refused, and that is a failure.
    [MessageLogging(EventId = 11268, Level = LogLevel.Information, Message = "Connection '{connectionName}' discovered no containers")]
    public static partial IGenericMessage NothingDiscovered(ILogger logger, string connectionName);

    /// <summary>
    /// Logs what a discovery added to a data store.
    /// </summary>
    // Information: it changes a store's configuration, and says by how much.
    [MessageLogging(EventId = 11269, Level = LogLevel.Information, Message = "DataStore '{dataStoreName}': discovered {containersDiscovered} container(s); added {pathsAdded} path(s), {containersAdded} container(s), {fieldsAdded} field(s); updated {fieldsUpdated} field(s)")]
    public static partial IGenericMessage SchemaMerged(ILogger logger, string dataStoreName, int containersDiscovered, int pathsAdded, int containersAdded, int fieldsAdded, int fieldsUpdated);

    /// <summary>
    /// Logs when the connection configuration cannot be resolved by name or id.
    /// </summary>
    [MessageLogging(EventId = 31039, Level = LogLevel.Error, Message = "Connection configuration '{connectionName}' not found")]
    public static partial IGenericMessage ConnectionConfigNotFound(ILogger logger, string connectionName);

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
