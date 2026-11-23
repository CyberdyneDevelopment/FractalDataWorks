using System;
using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Data.Logging;

/// <summary>
/// MessageLogging for the owner of a path or a container of a shared data store (FDW-6967): one method and code per reason, the code is the number.
/// </summary>
[MessageLoggingTypeCode("DATA")]
public static partial class DataStoreItemOwnershipLog
{
    /// <summary>Logs that a workspace was made the owner of a path or a container.</summary>
    /// <param name="logger">The logger that records the event.</param>
    /// <param name="item">The path or container, as store/path[/container].</param>
    /// <param name="workspaceName">The owning workspace.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(EventId = 11760, Level = LogLevel.Information, Message = "Workspace '{workspaceName}' now owns '{item}'")]
    public static partial IGenericMessage OwnerAssigned(ILogger logger, string item, string workspaceName);

    /// <summary>Logs that a path or a container was given back to its store.</summary>
    /// <param name="logger">The logger that records the event.</param>
    /// <param name="item">The path or container, as store/path[/container].</param>
    /// <param name="workspaceName">The workspace that owned it.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(EventId = 11761, Level = LogLevel.Information, Message = "'{item}' was released by workspace '{workspaceName}' and is the store's again")]
    public static partial IGenericMessage OwnerReleased(ILogger logger, string item, string workspaceName);

    /// <summary>Logs whose scope decided a write of a path or a container.</summary>
    /// <param name="logger">The logger that records the event.</param>
    /// <param name="item">The path or container, as store/path[/container].</param>
    /// <param name="scope">The workspace that owns it, or the store.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(EventId = 11762, Level = LogLevel.Debug, Message = "A write of '{item}' is decided at the scope of {scope}")]
    public static partial IGenericMessage WriteDecidedAt(ILogger logger, string item, string scope);

    /// <summary>Traces the registration of the path and container ownership service.</summary>
    /// <param name="logger">The logger that records the event.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(EventId = 11763, Level = LogLevel.Trace, Message = "Registered the data store path and container ownership service")]
    public static partial IGenericMessage Registered(ILogger logger);

    /// <summary>Refuses an owner of a data store that is not there.</summary>
    /// <param name="logger">The logger that records the event.</param>
    /// <param name="storeName">The data store named.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(EventId = 31760, Level = LogLevel.Warning, Message = "Data store '{storeName}' was not found",
        ClientMessage = "Data store '{storeName}' was not found.")]
    public static partial IGenericMessage StoreNotFound(ILogger logger, string storeName);

    /// <summary>Refuses an owner of a path the store does not have.</summary>
    /// <param name="logger">The logger that records the event.</param>
    /// <param name="storeName">The data store.</param>
    /// <param name="pathName">The path named.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(EventId = 31761, Level = LogLevel.Warning, Message = "Data store '{storeName}' has no path '{pathName}'",
        ClientMessage = "Data store '{storeName}' has no path '{pathName}'.")]
    public static partial IGenericMessage PathNotFound(ILogger logger, string storeName, string pathName);

    /// <summary>Refuses an owner of a container the path does not have.</summary>
    /// <param name="logger">The logger that records the event.</param>
    /// <param name="storeName">The data store.</param>
    /// <param name="pathName">The path.</param>
    /// <param name="containerName">The container named.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(EventId = 31762, Level = LogLevel.Warning, Message = "Path '{pathName}' of data store '{storeName}' has no container '{containerName}'",
        ClientMessage = "Path '{pathName}' of data store '{storeName}' has no container '{containerName}'.")]
    public static partial IGenericMessage ContainerNotFound(ILogger logger, string storeName, string pathName, string containerName);

    /// <summary>Refuses an owner that is a workspace that is not there.</summary>
    /// <param name="logger">The logger that records the event.</param>
    /// <param name="workspaceName">The workspace named.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(EventId = 31763, Level = LogLevel.Warning, Message = "Workspace '{workspaceName}' was not found",
        ClientMessage = "Workspace '{workspaceName}' was not found.")]
    public static partial IGenericMessage WorkspaceNotFound(ILogger logger, string workspaceName);

    /// <summary>Refuses an owner for a path or a container another workspace owns.</summary>
    /// <param name="logger">The logger that records the event.</param>
    /// <param name="item">The path or container, as store/path[/container].</param>
    /// <param name="ownerName">The workspace that owns it.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(EventId = 41760, Level = LogLevel.Warning, Message = "'{item}' is owned by workspace '{ownerName}'; it must be released before another workspace owns it",
        ClientMessage = "'{item}' is owned by workspace '{ownerName}'. It must be released before another workspace can own it.")]
    public static partial IGenericMessage OwnedByAnother(ILogger logger, string item, string ownerName);

    /// <summary>Refuses an owner that is a workspace of another tenant than the store's.</summary>
    /// <param name="logger">The logger that records the event.</param>
    /// <param name="item">The path or container, as store/path[/container].</param>
    /// <param name="workspaceName">The workspace named.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(EventId = 41761, Level = LogLevel.Warning, Message = "Workspace '{workspaceName}' cannot own '{item}': it belongs to another tenant than the store",
        ClientMessage = "Workspace '{workspaceName}' belongs to another tenant than the store, so it cannot own '{item}'.")]
    public static partial IGenericMessage WorkspaceNotInStoreTenant(ILogger logger, string item, string workspaceName);

    /// <summary>Refuses a release of a path or a container that has no owner of its own.</summary>
    /// <param name="logger">The logger that records the event.</param>
    /// <param name="item">The path or container, as store/path[/container].</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(EventId = 41762, Level = LogLevel.Warning, Message = "'{item}' has no owner of its own to release",
        ClientMessage = "'{item}' has no owner of its own to release.")]
    public static partial IGenericMessage NotOwned(ILogger logger, string item);

    /// <summary>Refuses a write of an unowned path or container to a caller the store's rule refuses.</summary>
    /// <param name="logger">The logger that records the event.</param>
    /// <param name="item">The path or container, as store/path[/container].</param>
    /// <param name="permission">The store's permission the caller does not hold.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(EventId = 51760, Level = LogLevel.Warning, Message = "'{item}' belongs to its store and the caller does not hold '{permission}'",
        ClientMessage = "'{item}' belongs to its data store, and changing it needs the permission '{permission}'.")]
    public static partial IGenericMessage StorePermissionMissing(ILogger logger, string item, string permission);

    /// <summary>Refuses a write by a caller who could not be resolved.</summary>
    /// <param name="logger">The logger that records the event.</param>
    /// <param name="item">The path or container, as store/path[/container].</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(EventId = 51761, Level = LogLevel.Warning, Message = "A write of '{item}' was refused: the caller is not signed in",
        ClientMessage = "Sign in to change '{item}'.")]
    public static partial IGenericMessage CallerUnresolved(ILogger logger, string item);

    /// <summary>Fails a read that finds more than one workspace owning a path or a container.</summary>
    /// <param name="logger">The logger that records the event.</param>
    /// <param name="item">The path or container, as store/path[/container].</param>
    /// <param name="count">How many owner rows were found.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(EventId = 91760, Level = LogLevel.Error, Message = "'{item}' has {count} owner rows; one owner is the rule and none is picked",
        ClientMessage = "'{item}' is recorded with more than one owner, so who decides its writes cannot be told.")]
    public static partial IGenericMessage OwnerAmbiguous(ILogger logger, string item, int count);

    /// <summary>Fails a write whose touched paths and containers cannot be told.</summary>
    /// <param name="logger">The logger that records the event.</param>
    /// <param name="ex">Why the stored and written store could not be compared.</param>
    /// <param name="storeName">The data store being written.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(EventId = 91762, Level = LogLevel.Error, Message = "A write of data store '{storeName}' was refused: the paths and containers it changes could not be told",
        ClientMessage = "The change to data store '{storeName}' was not saved: what it changes could not be told.")]
    public static partial IGenericMessage ChangeNotCompared(ILogger logger, Exception ex, string storeName);

    /// <summary>Fails a write of a data store that is not there to compare with.</summary>
    /// <param name="logger">The logger that records the event.</param>
    /// <param name="storeId">The data store's Id.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(EventId = 31764, Level = LogLevel.Warning, Message = "Data store '{storeId}' was not found to compare a write with",
        ClientMessage = "The data store was not found.")]
    public static partial IGenericMessage StoreNotFoundForWrite(ILogger logger, string storeId);

    /// <summary>Logs how many scopes decided a write of a data store.</summary>
    /// <param name="logger">The logger that records the event.</param>
    /// <param name="storeName">The data store being written.</param>
    /// <param name="items">How many paths and containers the write touches.</param>
    /// <param name="scopes">How many scopes decided it.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(EventId = 11764, Level = LogLevel.Debug, Message = "A write of data store '{storeName}' touches {items} path(s) and container(s), decided at {scopes} scope(s)")]
    public static partial IGenericMessage WriteDecided(ILogger logger, string storeName, int items, int scopes);

    /// <summary>Fails a read whose owner row names a workspace that cannot be read.</summary>
    /// <param name="logger">The logger that records the event.</param>
    /// <param name="item">The path or container, as store/path[/container].</param>
    /// <param name="workspaceId">The workspace the owner row names.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(EventId = 91761, Level = LogLevel.Error, Message = "The owner row of '{item}' names workspace '{workspaceId}', which could not be read",
        ClientMessage = "The workspace that owns '{item}' could not be read.")]
    public static partial IGenericMessage OwnerWorkspaceMissing(ILogger logger, string item, string workspaceId);
}
