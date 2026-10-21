using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Authorization.Logging;

/// <summary>
/// MessageLogging for finding who holds a permission (FDW-2144).
/// EventId ranges: 12730 (resolved), 32730 (permission missing), 62730 (configuration), 72730 (dependency).
/// </summary>
[MessageLoggingTypeCode("AUTHORIZATION")]
public static partial class PermissionHoldersLog
{
    /// <summary>
    /// Logs the people found to hold a permission in a tenant.
    /// </summary>
    [MessageLogging(EventId = 12730, Level = LogLevel.Debug, Message = "{count} user(s) hold '{permission}' in tenant {tenantId}")]
    public static partial IGenericMessage HoldersResolved(ILogger logger, int count, string permission, string tenantId);

    /// <summary>
    /// Logs that the permission asked about does not exist.
    /// </summary>
    [MessageLogging(EventId = 32730, Level = LogLevel.Warning, Message = "Who holds '{permission}' cannot be found: no such permission exists")]
    public static partial IGenericMessage PermissionNotFound(ILogger logger, string permission);

    /// <summary>
    /// Logs that the global tenant could not be identified.
    /// </summary>
    [MessageLogging(EventId = 62730, Level = LogLevel.Error, Message = "Who holds '{permission}' on the platform cannot be found: {count} tenants are marked global, and exactly one must be")]
    public static partial IGenericMessage GlobalTenantNotSingle(ILogger logger, string permission, int count);

    /// <summary>
    /// Logs that a read needed to find the holders failed.
    /// </summary>
    [MessageLogging(EventId = 72730, Level = LogLevel.Error, Message = "Who holds '{permission}' cannot be found: reading {what} failed: {reason}")]
    public static partial IGenericMessage ReadFailed(ILogger logger, string permission, string what, string? reason);
}
