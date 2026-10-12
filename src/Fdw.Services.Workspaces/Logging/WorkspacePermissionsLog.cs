using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Workspaces.Logging;

/// <summary>
/// MessageLogging for <see cref="WorkspacePermissions"/>: every step of creating and removing a
/// workspace's own permissions and their role grants (FDW-937).
/// </summary>
[MessageLoggingTypeCode("WORKSPACES")]
public static partial class WorkspacePermissionsLog
{
    /// <summary>Logs that a workspace's own permissions are being created.</summary>
    /// <param name="logger">The logger that records the event.</param>
    /// <param name="workspaceName">The workspace's name.</param>
    /// <param name="workspaceId">The workspace's durable Id, which names its permissions.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(EventId = 11000, Level = LogLevel.Trace,
        Message = "Creating the permissions of workspace '{workspaceName}' ({workspaceId})")]
    public static partial IGenericMessage CreatingPermissions(ILogger logger, string workspaceName, string workspaceId);

    /// <summary>Logs that a permission already existed and was left as it is.</summary>
    /// <param name="logger">The logger that records the event.</param>
    /// <param name="permissionName">The permission's name.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(EventId = 11001, Level = LogLevel.Debug,
        Message = "Permission '{permissionName}' already exists; left as it is")]
    public static partial IGenericMessage PermissionAlreadyExists(ILogger logger, string permissionName);

    /// <summary>Logs that a permission was created.</summary>
    /// <param name="logger">The logger that records the event.</param>
    /// <param name="permissionName">The permission's name.</param>
    /// <param name="displayName">The permission's display name.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(EventId = 11002, Level = LogLevel.Information,
        Message = "Created permission '{permissionName}' ({displayName})")]
    public static partial IGenericMessage PermissionCreated(ILogger logger, string permissionName, string displayName);

    /// <summary>Logs that a workspace's own permissions are being removed.</summary>
    /// <param name="logger">The logger that records the event.</param>
    /// <param name="workspaceId">The workspace's durable Id.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(EventId = 11003, Level = LogLevel.Trace,
        Message = "Removing the permissions of workspace {workspaceId}")]
    public static partial IGenericMessage RemovingPermissions(ILogger logger, string workspaceId);

    /// <summary>Logs that a permission to be removed did not exist.</summary>
    /// <param name="logger">The logger that records the event.</param>
    /// <param name="permissionName">The permission's name.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(EventId = 11004, Level = LogLevel.Debug,
        Message = "Permission '{permissionName}' does not exist; nothing to remove")]
    public static partial IGenericMessage PermissionAbsent(ILogger logger, string permissionName);

    /// <summary>Logs that a role's grant of a permission was removed.</summary>
    /// <param name="logger">The logger that records the event.</param>
    /// <param name="permissionName">The permission's name.</param>
    /// <param name="roleId">The role the grant belonged to.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(EventId = 11005, Level = LogLevel.Information,
        Message = "Removed the grant of '{permissionName}' from role {roleId}")]
    public static partial IGenericMessage GrantRemoved(ILogger logger, string permissionName, string roleId);

    /// <summary>Logs that a permission was removed.</summary>
    /// <param name="logger">The logger that records the event.</param>
    /// <param name="permissionName">The permission's name.</param>
    /// <param name="grantCount">How many role grants of it were removed first.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(EventId = 11006, Level = LogLevel.Information,
        Message = "Removed permission '{permissionName}' and its {grantCount} role grants")]
    public static partial IGenericMessage PermissionRemoved(ILogger logger, string permissionName, int grantCount);

    /// <summary>Logs that a step of creating or removing a permission failed.</summary>
    /// <param name="logger">The logger that records the event.</param>
    /// <param name="step">What was being done.</param>
    /// <param name="permissionName">The permission's name.</param>
    /// <param name="reason">Why it failed.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(EventId = 91000, Level = LogLevel.Error,
        Message = "Could not {step} permission '{permissionName}': {reason}")]
    public static partial IGenericMessage PermissionStepFailed(ILogger logger, string step, string permissionName, string reason);
}
