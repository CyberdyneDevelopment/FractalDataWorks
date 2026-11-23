using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Workspaces.Logging;

/// <summary>
/// MessageLogging for reading and writing which workspace owns a resource (FDW-6967).
/// </summary>
[MessageLoggingTypeCode("WORKSPACES")]
public static partial class WorkspaceResourceOwnersLog
{
    /// <summary>Logs the owners read for one resource.</summary>
    /// <param name="logger">The logger that records the event.</param>
    /// <param name="kind">The resource's kind.</param>
    /// <param name="resourceId">The resource's Id.</param>
    /// <param name="count">How many workspaces own it.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(EventId = 11760, Level = LogLevel.Trace,
        Message = "Read the owners of {kind} {resourceId}: {count} row(s)")]
    public static partial IGenericMessage OwnersRead(ILogger logger, string kind, string resourceId, int count);

    /// <summary>Logs that a workspace was recorded as a resource's owner.</summary>
    /// <param name="logger">The logger that records the event.</param>
    /// <param name="workspaceName">The owning workspace.</param>
    /// <param name="kind">The resource's kind.</param>
    /// <param name="resourceId">The resource's Id.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(EventId = 11761, Level = LogLevel.Information,
        Message = "Workspace '{workspaceName}' now owns {kind} '{resourceId}'")]
    public static partial IGenericMessage Owned(ILogger logger, string workspaceName, string kind, string resourceId);

    /// <summary>Logs that a resource's owner row was retired.</summary>
    /// <param name="logger">The logger that records the event.</param>
    /// <param name="workspaceId">The workspace that owned it.</param>
    /// <param name="attachmentId">The retired <c>Owns</c> row.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(EventId = 11762, Level = LogLevel.Information,
        Message = "Workspace '{workspaceId}' owns the resource of row '{attachmentId}' no more")]
    public static partial IGenericMessage Released(ILogger logger, string workspaceId, string attachmentId);

    /// <summary>Logs what archiving a workspace released.</summary>
    /// <param name="logger">The logger that records the event.</param>
    /// <param name="workspaceName">The archived workspace.</param>
    /// <param name="count">How many resources were released.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(EventId = 11763, Level = LogLevel.Information,
        Message = "Archiving workspace '{workspaceName}' released {count} resource(s) it owned")]
    public static partial IGenericMessage ReleasedOnArchive(ILogger logger, string workspaceName, int count);

    /// <summary>Refuses to record an owner of a kind no workspace may own.</summary>
    /// <param name="logger">The logger that records the event.</param>
    /// <param name="workspaceName">The workspace asked to own it.</param>
    /// <param name="kind">The resource's kind.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(EventId = 21760, Level = LogLevel.Warning,
        Message = "Workspace '{workspaceName}' cannot own a {kind}: the kind is not one a workspace may own",
        ClientMessage = "A workspace cannot own a {kind}.")]
    public static partial IGenericMessage KindNotOwnable(ILogger logger, string workspaceName, string kind);

    /// <summary>Refuses to record an owner of a kind no package declared.</summary>
    /// <param name="logger">The logger that records the event.</param>
    /// <param name="workspaceName">The workspace asked to own it.</param>
    /// <param name="kind">The kind named.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(EventId = 21761, Level = LogLevel.Warning,
        Message = "Workspace '{workspaceName}' cannot own a {kind}: no such resource kind is registered on this host",
        ClientMessage = "'{kind}' is not a kind of resource this host knows.")]
    public static partial IGenericMessage KindUnknown(ILogger logger, string workspaceName, string kind);

    /// <summary>Fails a read of a resource's owners.</summary>
    /// <param name="logger">The logger that records the event.</param>
    /// <param name="kind">The resource's kind.</param>
    /// <param name="resourceId">The resource's Id.</param>
    /// <param name="reason">Why the rows could not be read.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(EventId = 91760, Level = LogLevel.Error,
        Message = "The owners of {kind} '{resourceId}' could not be read: {reason}",
        ClientMessage = "Who owns it could not be read.")]
    public static partial IGenericMessage OwnersNotRead(ILogger logger, string kind, string resourceId, string reason);

    /// <summary>Fails the retiring of an owner row.</summary>
    /// <param name="logger">The logger that records the event.</param>
    /// <param name="attachmentId">The <c>Owns</c> row.</param>
    /// <param name="reason">Why it could not be retired.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(EventId = 91761, Level = LogLevel.Error,
        Message = "The owner row '{attachmentId}' could not be retired: {reason}",
        ClientMessage = "The owner could not be released.")]
    public static partial IGenericMessage ReleaseFailed(ILogger logger, string attachmentId, string reason);

    /// <summary>Fails a read or release of owner rows when the registration named no path for the workspace rows.</summary>
    /// <param name="logger">The logger that records the event.</param>
    /// <param name="dataStore">The configuration connection the rows live on.</param>
    /// <param name="container">The container the rows live in.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(EventId = 61760, Level = LogLevel.Critical,
        Message = "The owner rows in container '{container}' on '{dataStore}' cannot be reached: the workspace services were registered with no path for them",
        ClientMessage = "Who owns it could not be read: the host's workspace services are not set up.")]
    public static partial IGenericMessage PathNotDeclared(ILogger logger, string dataStore, string container);

    /// <summary>Fails a write that names no caller to record as the one who made it.</summary>
    /// <param name="logger">The logger that records the event.</param>
    /// <param name="workspaceName">The workspace asked to own the resource.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(EventId = 51760, Level = LogLevel.Warning,
        Message = "Workspace '{workspaceName}' was not made an owner: the caller could not be resolved",
        ClientMessage = "Who is asking could not be resolved.")]
    public static partial IGenericMessage CallerUnresolved(ILogger logger, string workspaceName);
}
