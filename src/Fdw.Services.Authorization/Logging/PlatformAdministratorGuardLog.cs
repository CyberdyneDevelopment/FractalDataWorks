using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Authorization.Logging;

/// <summary>
/// MessageLogging for the last-platform-administrator guard.
/// EventId ranges: 12711 (allowed), 32700 (role missing), 42700-42701 (conflict: refused), 62700 (configuration), 72702 (dependency).
/// </summary>
[MessageLoggingTypeCode("AUTHORIZATION")]
public static partial class PlatformAdministratorGuardLog
{
    /// <summary>
    /// Logs that a change was allowed because another platform administrator remains.
    /// </summary>
    [MessageLogging(EventId = 12711, Level = LogLevel.Information, Message = "Removing '{roleName}' from user '{userId}' leaves {remaining} other platform administrator(s)")]
    public static partial IGenericMessage AdministratorRemains(ILogger logger, string roleName, string userId, int remaining);

    /// <summary>
    /// Logs that a removal was refused because the person is the last platform administrator.
    /// </summary>
    [MessageLogging(EventId = 42700, Level = LogLevel.Warning, Message = "Removing '{roleName}' from user '{userId}' was refused: they are the last platform administrator")]
    public static partial IGenericMessage LastAdministrator(ILogger logger, string roleName, string userId);

    /// <summary>
    /// Logs that deleting the administrator role was refused because people still hold it in the global tenant.
    /// </summary>
    [MessageLogging(EventId = 42701, Level = LogLevel.Warning, Message = "Deleting '{roleName}' was refused: {holders} platform administrator(s) hold it in the global tenant")]
    public static partial IGenericMessage AdministratorRoleHeld(ILogger logger, string roleName, int holders);

    /// <summary>
    /// Logs that the global tenant could not be identified.
    /// </summary>
    [MessageLogging(EventId = 62700, Level = LogLevel.Error, Message = "The platform administrators cannot be found: {count} tenants are marked global, and exactly one must be")]
    public static partial IGenericMessage GlobalTenantNotSingle(ILogger logger, int count);

    /// <summary>
    /// Logs that a read the guard needs failed.
    /// </summary>
    [MessageLogging(EventId = 72702, Level = LogLevel.Error, Message = "The platform administrators cannot be found: reading {what} failed: {reason}")]
    public static partial IGenericMessage ReadFailed(ILogger logger, string what, string? reason);

    /// <summary>
    /// Logs that the role the guard was asked about does not exist.
    /// </summary>
    [MessageLogging(EventId = 32700, Level = LogLevel.Warning, Message = "The platform administrators cannot be found: role '{roleName}' does not exist")]
    public static partial IGenericMessage RoleNotFound(ILogger logger, string roleName);
}
