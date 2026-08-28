using Microsoft.Extensions.Logging;
using Fdw.Messages;
using Fdw.MessageLogging;

namespace Fdw.Services.Data.Logging;

/// <summary>
/// MessageLogging for ConfigurationGateway operations.
/// EventId range: 1050-1059
/// </summary>
[MessageLoggingTypeCode("DATA")]
public static partial class ConfigurationGatewayLog
{
    /// <summary>Logged when the gateway initialises and binds its connection.</summary>
    [MessageLogging(EventId = 11013, Level = LogLevel.Debug,
        Message = "ConfigurationGateway initialised with connection '{connectionName}'")]
    public static partial IGenericMessage Initialised(ILogger logger, string connectionName);

    /// <summary>Logged when the configured connection name is not found in IDataConnectionProvider.</summary>
    [MessageLogging(EventId = 31004, Level = LogLevel.Error,
        Message = "ConfigurationGateway: connection '{connectionName}' not found in IDataConnectionProvider")]
    public static partial IGenericMessage ConnectionNotFound(ILogger logger, string connectionName);

    /// <summary>Logged when the connection factory fails to create a connection.</summary>
    [MessageLogging(EventId = 71001, Level = LogLevel.Error,
        Message = "ConfigurationGateway: failed to create connection '{connectionName}': {reason}")]
    public static partial IGenericMessage ConnectionCreationFailed(ILogger logger, string connectionName, string reason);

    /// <summary>Logged when Execute returns a failure result.</summary>
    [MessageLogging(EventId = 71002, Level = LogLevel.Error,
        Message = "ConfigurationGateway.Execute failed for container '{container}': {reason}")]
    public static partial IGenericMessage ExecuteFailed(ILogger logger, string container, string reason);

    /// <summary>Logged when Execute throws an unhandled exception.</summary>
    [MessageLogging(EventId = 71003, Level = LogLevel.Error,
        Message = "ConfigurationGateway.Execute threw an exception for container '{container}'")]
    public static partial IGenericMessage ExecuteException(ILogger logger, Exception ex, string container);

    // =========================================================================
    // Schema-binding MessageLogging (EventId range: 5585-5599)
    // Allocated within the 5500-5599 Services.Data domain range.
    // =========================================================================

    /// <summary>Logged when the bound ConfigurationSchema contains no DataStores.</summary>
    [MessageLogging(EventId = 61001, Level = LogLevel.Error,
        Message = "ConfigurationGateway: ConfigurationSchema is bound but contains no DataStores — tree will be empty and container resolution will fail")]
    public static partial IGenericMessage SchemaEmpty(ILogger logger);

    /// <summary>
    /// Logged when a DataContainerKey declares a ReferencedContainerName that cannot be resolved
    /// to any container in the same DataStore.
    /// </summary>
    /// <remarks>
    /// Why: a null ReferencedContainer on an FK key means cascade will never fire for this
    /// key. The fix is to ensure the referenced container's name exactly matches what is
    /// declared in the JSON. No fallback — fail loud per NO FALLBACKS rule.
    /// </remarks>
    [MessageLogging(EventId = 61002, Level = LogLevel.Error,
        Message = "ConfigurationGateway tree-build: key '{keyName}' on container '{childContainer}' references container '{referencedContainerName}' which was not found in the same DataStore — tree build aborted")]
    public static partial IGenericMessage KeyReferencedContainerUnresolved(
        ILogger logger, string keyName, string childContainer, string referencedContainerName);

    /// <summary>
    /// Logged when a DataContainerKey declares a ReferencedKeyName that cannot be resolved
    /// to any key on the already-resolved referenced container.
    /// </summary>
    [MessageLogging(EventId = 61003, Level = LogLevel.Error,
        Message = "ConfigurationGateway tree-build: key '{keyName}' on container '{childContainer}' references key '{referencedKeyName}' which was not found on the referenced container — tree build aborted")]
    public static partial IGenericMessage KeyReferencedKeyUnresolved(
        ILogger logger, string keyName, string childContainer, string referencedKeyName);

    /// <summary>Logged after the IDataStore tree is successfully built from the bound ConfigurationSchema.</summary>
    [MessageLogging(EventId = 11014, Level = LogLevel.Information,
        Message = "ConfigurationGateway: IDataStore tree built from ConfigurationSchema — {storeCount} store(s), {containerCount} container(s)")]
    public static partial IGenericMessage TreeBuiltFromSchema(ILogger logger, int storeCount, int containerCount);

    /// <summary>
    /// Logged when a caller attempts to execute a DataSetTarget through ConfigurationGateway,
    /// which has no DataSet federation layer.
    /// </summary>
    /// <remarks>
    /// Why: ConfigurationGateway is a single-connection gateway to ConfigurationDb. DataSet
    /// federation (multi-source, cross-connection joins) is exclusively a DataGatewayService concern.
    /// Routing a DataSetTarget here is always a caller bug — fail loud rather than silently return empty.
    /// </remarks>
    [MessageLogging(EventId = 61004, Level = LogLevel.Error,
        Message = "ConfigurationGateway does not support DataSetTarget '{dataSetName}' — DataSet execution requires DataGatewayService")]
    public static partial IGenericMessage DataSetTargetNotSupported(ILogger logger, string dataSetName);

    /// <summary>
    /// Logged when container resolution is asked for an empty container name and returns null.
    /// </summary>
    /// <remarks>
    /// Why Error, not Debug (FDW-583): the operation cannot complete — Execute turns this into a
    /// structured Failure result, and the printed record must name the reason instead of staying
    /// silent below the default print threshold.
    /// </remarks>
    [MessageLogging(EventId = 11015, Level = LogLevel.Error,
        Message = "ConfigurationGateway.ResolveContainer: returning null — Container is empty")]
    public static partial IGenericMessage ResolveContainerEmpty(ILogger logger);

    /// <summary>
    /// Logged when container resolution cannot find the named store in the built tree and returns null.
    /// </summary>
    /// <remarks>
    /// Why Error, not Debug (FDW-583): same reasoning as <see cref="ResolveContainerEmpty"/> — this is
    /// a terminal, addressed lookup (the caller named the store), not a probe-loop miss, so a failure
    /// here is the final answer for the request and must print.
    /// </remarks>
    [MessageLogging(EventId = 11016, Level = LogLevel.Error,
        Message = "ConfigurationGateway.ResolveContainer: store '{dataStoreName}' not found in tree of {count} store(s): [{available}]")]
    public static partial IGenericMessage ResolveContainerStoreNotFound(ILogger logger, string dataStoreName, int count, string available);

    /// <summary>
    /// Logged when container resolution scanned every path in the resolved store (target.Path was
    /// empty) and none contained the named container.
    /// </summary>
    [MessageLogging(EventId = 61024, Level = LogLevel.Error,
        Message = "ConfigurationGateway.ResolveContainer: container '{containerName}' not found in any path of DataStore '{dataStoreName}'")]
    public static partial IGenericMessage ResolveContainerNotFoundInAnyPath(ILogger logger, string containerName, string dataStoreName);

    /// <summary>Traces entry into ConfigurationGateway.Execute with the target address.</summary>
    [MessageLogging(EventId = 11017, Level = LogLevel.Trace,
        Message = "ConfigurationGateway.Execute entry: store='{dataStore}', path='{path}', container='{container}'")]
    public static partial IGenericMessage ExecuteEntry(ILogger logger, string dataStore, string? path, string container);

    /// <summary>Traces exit from ConfigurationGateway.Execute with the outcome.</summary>
    [MessageLogging(EventId = 11018, Level = LogLevel.Trace,
        Message = "ConfigurationGateway.Execute exit: container='{container}', success={success}")]
    public static partial IGenericMessage ExecuteExit(ILogger logger, string container, bool success);

    /// <summary>Traces entry into ConfigurationGateway.BuildConnection.</summary>
    [MessageLogging(EventId = 11019, Level = LogLevel.Trace,
        Message = "ConfigurationGateway.BuildConnection entry: resolving connection '{connectionName}'")]
    public static partial IGenericMessage BuildConnectionEntry(ILogger logger, string connectionName);

    /// <summary>Traces exit from ConfigurationGateway.BuildConnection with the outcome.</summary>
    [MessageLogging(EventId = 11020, Level = LogLevel.Trace,
        Message = "ConfigurationGateway.BuildConnection exit: connection '{connectionName}', success={success}")]
    public static partial IGenericMessage BuildConnectionExit(ILogger logger, string connectionName, bool success);

    /// <summary>Logged when the gateway is registered, naming the connection type it will use.</summary>
    [MessageLogging(EventId = 11014, Level = LogLevel.Information,
        Message = "ConfigurationGateway registered using connection type '{connectionType}' from '{schemaFile}'. "
                + "To use a different type, set ServiceOptionType on the connection in that file and reference the "
                + "package that provides it (e.g. ReferenceConnections.MsSql.ServiceType for 'MsSql'). "
                + "Registered types: {registeredTypes}")]
    public static partial IGenericMessage RegisteredWithConnectionType(
        ILogger logger, string connectionType, string schemaFile, string registeredTypes);

    /// <summary>Logged when the schema names a connection type that is not registered in ConnectionTypes.</summary>
    [MessageLogging(EventId = 71004, Level = LogLevel.Error,
        Message = "ConfigurationGateway: connection type '{connectionType}' declared in '{schemaFile}' is not "
                + "registered. Is the package that provides it referenced? A [ServiceTypeOption] registers itself "
                + "at assembly load, so an unreferenced package contributes nothing. Registered types: {registeredTypes}")]
    public static partial IGenericMessage ConnectionTypeNotRegistered(
        ILogger logger, string connectionType, string schemaFile, string registeredTypes);
}
