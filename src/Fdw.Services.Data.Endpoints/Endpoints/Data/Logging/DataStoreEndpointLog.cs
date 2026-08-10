using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Data.Endpoints.Logging;

/// <summary>
/// MessageLogging for DataStore endpoint base operations.
/// EventId range: 7248–7249
/// </summary>
/// <remarks>
/// Why: Relocated from 7138-7139 to avoid collision with ConnectionProviderLogger (7130-7163).
/// The 7248-7260 range is reserved for configuration endpoint logs.
/// </remarks>
[MessageLoggingTypeCode("DATAENDPOINTS")]
public static partial class DataStoreEndpointLog
{
    /// <summary>Logs when a modification is rejected because the data store is a system configuration.</summary>
    [MessageLogging(EventId = 41002, Level = LogLevel.Warning, Message = "Rejected modification of system data store '{dataStoreName}' — system configurations are read-only")]
    public static partial IGenericMessage SystemDataStoreReadOnly(ILogger logger, string dataStoreName);

    /// <summary>Logs when the connection name in a create/update request does not match any known connection.</summary>
    [MessageLogging(EventId = 31001, Level = LogLevel.Warning, Message = "Connection '{connectionName}' not found — cannot resolve ConnectionId for data store '{dataStoreName}'")]
    public static partial IGenericMessage ConnectionNotFound(ILogger logger, string connectionName, string dataStoreName);

    /// <summary>Logs when cache reload after DataStore create fails — Paths will be empty in response.</summary>
    [MessageLogging(EventId = 71003, Level = LogLevel.Warning, Message = "Cache reload after DataStore create returned failure for '{dataStoreName}'; Paths will be empty in response")]
    public static partial IGenericMessage CacheReloadFailed(ILogger logger, string dataStoreName);

    /// <summary>Logs when a data store is not found during a sub-resource operation.</summary>
    [MessageLogging(EventId = 31002, Level = LogLevel.Warning, Message = "DataStore '{dataStoreName}' not found")]
    public static partial IGenericMessage DataStoreNotFound(ILogger logger, string dataStoreName);

    /// <summary>Logs when a path name is not found within a data store.</summary>
    [MessageLogging(EventId = 31003, Level = LogLevel.Warning, Message = "Path '{pathName}' not found in DataStore '{dataStoreName}'")]
    public static partial IGenericMessage PathNotFoundInDataStore(ILogger logger, string pathName, string dataStoreName);

    /// <summary>Logs when a container is successfully added to a data store path.</summary>
    [MessageLogging(EventId = 11014, Level = LogLevel.Information, Message = "Added container '{containerName}' to path '{pathName}' in DataStore '{dataStoreName}'")]
    public static partial IGenericMessage ContainerAdded(ILogger logger, string containerName, string pathName, string dataStoreName);
}
