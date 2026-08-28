using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Authorization.Endpoints;

/// <summary>
/// High-performance MessageLogging for authorization endpoint operations.
/// EventId range: 3113-3130
/// </summary>
[MessageLoggingTypeCode("ENDPOINTS5")]
public static partial class AuthorizationEndpointLog
{
    /// <summary>
    /// Logs when listing all permissions.
    /// </summary>
    [MessageLogging(EventId = 11000, Level = LogLevel.Trace,
        Message = "Listing all permissions")]
    public static partial IGenericMessage ListingPermissions(ILogger logger);

    /// <summary>
    /// Logs when permissions have been listed.
    /// </summary>
    [MessageLogging(EventId = 11001, Level = LogLevel.Information,
        Message = "Listed {count} permissions")]
    public static partial IGenericMessage ListedPermissions(ILogger logger, int count);

    /// <summary>
    /// Logs when getting role permissions.
    /// </summary>
    [MessageLogging(EventId = 11002, Level = LogLevel.Debug,
        Message = "Getting permissions for role '{roleName}'")]
    public static partial IGenericMessage GettingRolePermissions(ILogger logger, string roleName);

    /// <summary>
    /// Logs when setting role permissions.
    /// </summary>
    [MessageLogging(EventId = 11003, Level = LogLevel.Debug,
        Message = "Setting permissions for role '{roleName}'")]
    public static partial IGenericMessage SettingRolePermissions(ILogger logger, string roleName);

    /// <summary>
    /// Logs when role permissions have been updated.
    /// </summary>
    [MessageLogging(EventId = 11004, Level = LogLevel.Information,
        Message = "Role '{roleName}' permissions updated with {count} permissions")]
    public static partial IGenericMessage RolePermissionsUpdated(ILogger logger, string roleName, int count);

    /// <summary>
    /// Logs when assigning a role to a user.
    /// </summary>
    [MessageLogging(EventId = 11005, Level = LogLevel.Debug,
        Message = "Assigning role '{roleName}' to user '{userId}'")]
    public static partial IGenericMessage AssigningUserRole(ILogger logger, string roleName, string userId);

    /// <summary>
    /// Logs when a role has been assigned to a user.
    /// </summary>
    [MessageLogging(EventId = 11006, Level = LogLevel.Information,
        Message = "Role '{roleName}' assigned to user '{userId}'")]
    public static partial IGenericMessage UserRoleAssigned(ILogger logger, string roleName, string userId);

    /// <summary>
    /// Logs when revoking a role from a user.
    /// </summary>
    [MessageLogging(EventId = 11007, Level = LogLevel.Debug,
        Message = "Revoking role '{roleName}' from user '{userId}'")]
    public static partial IGenericMessage RevokingUserRole(ILogger logger, string roleName, string userId);

    /// <summary>
    /// Logs when a role has been revoked from a user.
    /// </summary>
    [MessageLogging(EventId = 11008, Level = LogLevel.Information,
        Message = "Role '{roleName}' revoked from user '{userId}'")]
    public static partial IGenericMessage UserRoleRevoked(ILogger logger, string roleName, string userId);

    /// <summary>
    /// Logs when an authorization endpoint operation fails.
    /// </summary>
    [MessageLogging(EventId = 91000, Level = LogLevel.Error,
        Message = "Authorization endpoint operation '{operation}' failed for context '{context}'")]
    public static partial IGenericMessage OperationFailed(
        ILogger logger,
        System.Exception exception,
        string operation,
        string context);


    /// <summary>
    /// Logs when the transactional role change is rolled back because a step (role write or stamp bump)
    /// failed. Nothing was persisted — the caller receives this structured failure.
    /// </summary>
    [MessageLogging(EventId = 71000, Level = LogLevel.Error,
        Message = "Atomic role change rolled back for '{context}': {reason}")]
    public static partial IGenericMessage AtomicRoleChangeFailed(ILogger logger, string context, string reason);

    /// <summary>
    /// Logs when a transaction scope cannot be opened for a role change operation.
    /// </summary>
    [MessageLogging(EventId = 71001, Level = LogLevel.Error,
        Message = "Failed to open transaction for role change for '{context}': {reason}")]
    public static partial IGenericMessage TransactionOpenFailed(ILogger logger, string context, string reason);

    /// <summary>
    /// Logs when rolling back the transaction after a failed role/permission write itself fails,
    /// leaving the transaction outcome indeterminate.
    /// </summary>
    [MessageLogging(EventId = 71002, Level = LogLevel.Error,
        Message = "Rollback after a failed role change for '{context}' did not succeed: {reason}")]
    public static partial IGenericMessage RollbackFailed(ILogger logger, string context, string? reason);
}
