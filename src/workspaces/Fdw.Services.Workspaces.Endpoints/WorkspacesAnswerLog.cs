using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Workspaces.Endpoints;

/// <summary>The coded messages the workspaces endpoints answer a refusal or a fault with: one per reason, each with the client text it shows (FDW-2335).</summary>
[MessageLoggingTypeCode("WSPACEANSWERS")]
public static partial class WorkspacesAnswerLog
{

    /// <summary>Logs that Workspace member was not found, or is not visible to the caller.</summary>
    [MessageLogging(EventId = 30001, Level = LogLevel.Warning, Message = "Workspace member was not found, or is not visible to the caller", ClientMessage = "Workspace member was not found")]
    public static partial IGenericMessage RemoveWorkspaceMemberWorkspacememberNotFound(ILogger logger);

    /// <summary>Logs that Detach workspace resource was not found, or is not visible to the caller.</summary>
    [MessageLogging(EventId = 30002, Level = LogLevel.Warning, Message = "Detach workspace resource was not found, or is not visible to the caller", ClientMessage = "Detach workspace resource was not found")]
    public static partial IGenericMessage DetachWorkspaceResourceDetachworkspaceresourceNotFound(ILogger logger);

    /// <summary>Logs that Detach workspace relationship was not found, or is not visible to the caller.</summary>
    [MessageLogging(EventId = 30003, Level = LogLevel.Warning, Message = "Detach workspace relationship was not found, or is not visible to the caller", ClientMessage = "Detach workspace relationship was not found")]
    public static partial IGenericMessage DetachWorkspaceRelationshipDetachworkspacerelationshipNotFound(ILogger logger);

    /// <summary>Logs that Workspace membership request was not found, or is not visible to the caller.</summary>
    [MessageLogging(EventId = 30004, Level = LogLevel.Warning, Message = "Workspace membership request was not found, or is not visible to the caller", ClientMessage = "Workspace membership request was not found")]
    public static partial IGenericMessage ApproveWorkspaceMembershipRequestNotFound(ILogger logger);

    /// <summary>Logs that Workspace membership request was not found, or is not visible to the caller.</summary>
    [MessageLogging(EventId = 30005, Level = LogLevel.Warning, Message = "Workspace membership request was not found, or is not visible to the caller", ClientMessage = "Workspace membership request was not found")]
    public static partial IGenericMessage ApproveWorkspaceMembershipRequestNotFound2(ILogger logger);

    /// <summary>Logs that Workspace member was not found, or is not visible to the caller.</summary>
    [MessageLogging(EventId = 30006, Level = LogLevel.Warning, Message = "Workspace member was not found, or is not visible to the caller", ClientMessage = "Workspace member was not found")]
    public static partial IGenericMessage RemoveWorkspaceMemberNotFound(ILogger logger);

    /// <summary>Logs that Workspace resource was not found, or is not visible to the caller.</summary>
    [MessageLogging(EventId = 30007, Level = LogLevel.Warning, Message = "Workspace resource was not found, or is not visible to the caller", ClientMessage = "Workspace resource was not found")]
    public static partial IGenericMessage DetachWorkspaceResourceNotFound(ILogger logger);

    /// <summary>Logs that Workspace membership request was not found, or is not visible to the caller.</summary>
    [MessageLogging(EventId = 30008, Level = LogLevel.Warning, Message = "Workspace membership request was not found, or is not visible to the caller", ClientMessage = "Workspace membership request was not found")]
    public static partial IGenericMessage DenyWorkspaceMembershipRequestNotFound(ILogger logger);

    /// <summary>Logs that Workspace membership request was not found, or is not visible to the caller.</summary>
    [MessageLogging(EventId = 30009, Level = LogLevel.Warning, Message = "Workspace membership request was not found, or is not visible to the caller", ClientMessage = "Workspace membership request was not found")]
    public static partial IGenericMessage DenyWorkspaceMembershipRequestNotFound2(ILogger logger);

    /// <summary>Logs that Workspace relationship was not found, or is not visible to the caller.</summary>
    [MessageLogging(EventId = 30010, Level = LogLevel.Warning, Message = "Workspace relationship was not found, or is not visible to the caller", ClientMessage = "Workspace relationship was not found")]
    public static partial IGenericMessage DetachWorkspaceRelationshipNotFound(ILogger logger);
}
