using System;
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

    /// <summary>Logs when a data store's configuration carries no Implementation, so it cannot be described.</summary>
    [MessageLogging(EventId = 90110, Level = LogLevel.Error, Message = "DataStore '{dataStoreName}' has no Implementation on its configuration, so it cannot be described")]
    public static partial IGenericMessage ImplementationMissing(ILogger logger, string dataStoreName);

    /// <summary>Logs when the connection a data store references cannot be read, so a response cannot name it.</summary>
    [MessageLogging(EventId = 90111, Level = LogLevel.Error, Message = "DataStore '{dataStoreName}' references connection '{connectionId}', which could not be read, so the response cannot name it")]
    public static partial IGenericMessage ConnectionNameUnresolved(ILogger logger, Guid connectionId, string dataStoreName);

    /// <summary>The response left out the connection name because this caller may not see the connection.</summary>
    [MessageLogging(EventId = 11025, Level = LogLevel.Debug, Message = "DataStore '{dataStoreName}' response leaves out connection '{connectionId}': {reason}")]
    public static partial IGenericMessage ConnectionNameLeftOut(ILogger logger, Guid connectionId, string dataStoreName, string reason);

    /// <summary>Logs when the connection name in a create/update request does not match any known connection.</summary>
    [MessageLogging(EventId = 31001, Level = LogLevel.Warning, Message = "Connection '{connectionName}' not found — cannot resolve ConnectionId for data store '{dataStoreName}'")]
    public static partial IGenericMessage ConnectionNotFound(ILogger logger, string connectionName, string dataStoreName);

    /// <summary>Logs when a data store is asked for a data store type its connection's kind cannot back in this deployment.</summary>
    [MessageLogging(EventId = 21003, Level = LogLevel.Warning, Message = "Data store '{dataStoreName}' cannot have data store type '{storeType}': connection type '{connectionType}' offers [{available}]")]
    public static partial IGenericMessage StoreTypeNotAvailable(ILogger logger, string dataStoreName, string storeType, string connectionType, string available);

    /// <summary>Logs when cache reload after DataStore create fails — Paths will be empty in response.</summary>
    [MessageLogging(EventId = 71003, Level = LogLevel.Warning, Message = "Cache reload after DataStore create returned failure for '{dataStoreName}'; Paths will be empty in response")]
    public static partial IGenericMessage CacheReloadFailed(ILogger logger, string dataStoreName);

    /// <summary>Logs when a data store is not found during a sub-resource operation.</summary>
    [MessageLogging(EventId = 31002, Level = LogLevel.Warning, Message = "DataStore '{dataStoreName}' not found")]
    public static partial IGenericMessage DataStoreNotFound(ILogger logger, string dataStoreName);

    /// <summary>Logs when a path name is not found within a data store.</summary>
    [MessageLogging(EventId = 31003, Level = LogLevel.Warning, Message = "Path '{pathName}' not found in DataStore '{dataStoreName}'")]
    public static partial IGenericMessage PathNotFoundInDataStore(ILogger logger, string pathName, string dataStoreName);

    /// <summary>Logs when the path already holds a container of that name, so the add is refused.</summary>
    /// <remarks>
    /// A duplicate is a conflict with what is stored, not a missing thing, so it sits in the Conflict band (4xxxx, HTTP 409)
    /// with every other "already exists" refusal. It was 31004, in the NotFound band, and answered 404 (FDW-1050).
    /// </remarks>
    [MessageLogging(EventId = 41000, Level = LogLevel.Warning, Message = "Container '{containerName}' already exists in path '{pathName}' of DataStore '{dataStoreName}'")]
    public static partial IGenericMessage ContainerAlreadyExists(ILogger logger, string containerName, string pathName, string dataStoreName);

    /// <summary>Logs when a container is successfully added to a data store path.</summary>
    [MessageLogging(EventId = 11014, Level = LogLevel.Information, Message = "Added container '{containerName}' to path '{pathName}' in DataStore '{dataStoreName}'")]
    public static partial IGenericMessage ContainerAdded(ILogger logger, string containerName, string pathName, string dataStoreName);

    /// <summary>Logs when a key names a KeyTypes option that does not exist.</summary>
    [MessageLogging(EventId = 21001, Level = LogLevel.Warning, Message = "Key '{keyName}' declares unknown KeyType '{typeId}'")]
    public static partial IGenericMessage InvalidKeyType(ILogger logger, string keyName, string typeId);

    /// <summary>Logs when a Foreign key does not name the container and key it references.</summary>
    [MessageLogging(EventId = 21002, Level = LogLevel.Warning, Message = "Foreign key '{keyName}' must declare both ReferencedContainerName and ReferencedKeyName")]
    public static partial IGenericMessage ForeignKeyReferenceMissing(ILogger logger, string keyName);

    /// <summary>Logs when a key field names a field the same create request does not define.</summary>
    [MessageLogging(EventId = 31005, Level = LogLevel.Warning, Message = "Key '{keyName}' names field '{fieldName}', which is not among the container's own fields")]
    public static partial IGenericMessage KeyFieldNotFoundInContainer(ILogger logger, string keyName, string fieldName);

    /// <summary>Logs when a Foreign key's referenced container does not exist in the same DataStore.</summary>
    [MessageLogging(EventId = 31006, Level = LogLevel.Warning, Message = "Foreign key '{keyName}' references container '{containerName}', which was not found in DataStore '{dataStoreName}'")]
    public static partial IGenericMessage ReferencedContainerNotFound(ILogger logger, string keyName, string containerName, string dataStoreName);

    /// <summary>Logs when a Foreign key's referenced key does not exist on the referenced container.</summary>
    [MessageLogging(EventId = 31007, Level = LogLevel.Warning, Message = "Foreign key '{keyName}' references key '{referencedKeyName}' on container '{containerName}', which was not found")]
    public static partial IGenericMessage ReferencedKeyNotFound(ILogger logger, string keyName, string referencedKeyName, string containerName);

    /// <summary>Logs the tree a data store is being created with.</summary>
    [MessageLogging(EventId = 11015, Level = LogLevel.Debug, Message = "DataStore '{dataStoreName}' created with {paths} path(s), {containers} container(s) and {fields} field(s) from the request")]
    public static partial IGenericMessage TreeBuiltFromRequest(ILogger logger, string dataStoreName, int paths, int containers, int fields);

    /// <summary>Logs a bulk change (a classification, a datum type) that names no item to change.</summary>
    /// <remarks>A request with nothing to change is refused rather than saved as a no-op: it is almost always a caller that lost its selection.</remarks>
    [MessageLogging(EventId = 20000, Level = LogLevel.Warning, Message = "No {itemKind} was named to {change} in '{ownerName}'")]
    public static partial IGenericMessage BulkTargetsMissing(ILogger logger, string change, string itemKind, string ownerName);

    /// <summary>Logs a bulk change naming items its owner does not have; nothing is changed.</summary>
    [MessageLogging(EventId = 31008, Level = LogLevel.Warning, Message = "'{ownerName}' has no {itemKind} with id {itemIds}; nothing was changed ({change})")]
    public static partial IGenericMessage BulkTargetsNotFound(ILogger logger, string change, string ownerName, string itemKind, string itemIds);
}
