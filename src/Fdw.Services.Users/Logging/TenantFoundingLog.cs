using System;
using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Users.Logging;

/// <summary>
/// MessageLogging for making a tenant with its first administrator (FDW-1578).
/// EventId ranges: 12730 (made), 32730 (the person has no account, 404), 72730-72732 (the membership or the role could not be set, the
/// tenant could not be removed again).
/// </summary>
[MessageLoggingTypeCode("USERS")]
public static partial class TenantFoundingLog
{
    /// <summary>Logs that a tenant was made with its first administrator.</summary>
    [MessageLogging(EventId = 12730, Level = LogLevel.Information, Message = "Tenant '{tenantId}' was made with user '{administratorId}' as its first administrator, holding role '{roleName}'")]
    public static partial IGenericMessage Founded(ILogger logger, Guid tenantId, Guid administratorId, string roleName);

    /// <summary>Logs that the named first administrator has no account.</summary>
    [MessageLogging(EventId = 32730, Level = LogLevel.Warning, Message = "A tenant was not made: user '{administratorId}', named as its first administrator, has no account")]
    public static partial IGenericMessage AdministratorNotFound(ILogger logger, Guid administratorId);

    /// <summary>Logs that the first administrator could not be made a member of the new tenant.</summary>
    [MessageLogging(EventId = 72730, Level = LogLevel.Error, Message = "User '{administratorId}' could not be made a member of the new tenant '{tenantId}': {reason}; the tenant is being removed again")]
    public static partial IGenericMessage MembershipNotSaved(ILogger logger, Guid administratorId, Guid tenantId, string? reason);

    /// <summary>Logs that the first administrator could not be given the role in the new tenant.</summary>
    [MessageLogging(EventId = 72731, Level = LogLevel.Error, Message = "User '{administratorId}' could not be given role '{roleName}' in the new tenant '{tenantId}': {reason}; the tenant is being removed again")]
    public static partial IGenericMessage RoleNotGiven(ILogger logger, Guid administratorId, string roleName, Guid tenantId, string? reason);

    /// <summary>Logs that what was made could not be removed again, so it must be removed by hand.</summary>
    [MessageLogging(EventId = 72732, Level = LogLevel.Critical, Message = "Tenant '{tenantId}' could not be removed again after its first administrator could not be set; remove the {what} by hand: {reason}")]
    public static partial IGenericMessage NotUndone(ILogger logger, Guid tenantId, string what, string? reason);
}
