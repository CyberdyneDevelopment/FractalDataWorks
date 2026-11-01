using System;
using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Authorization.Logging;

/// <summary>
/// MessageLogging for giving a role to a person in a tenant.
/// EventId ranges: 12720-12721 (given, already held), 22720 (the grant could not be saved, 400), 32720 (the role does not exist, 404),
/// 72720-72721 (the person's roles or the role could not be read, 500).
/// </summary>
[MessageLoggingTypeCode("AUTHORIZATION")]
public static partial class UserRoleAssignmentLog
{
    /// <summary>Logs that a role was given.</summary>
    [MessageLogging(EventId = 12720, Level = LogLevel.Information, Message = "Role '{roleName}' was given to user '{userId}' in tenant '{tenantId}'")]
    public static partial IGenericMessage Assigned(ILogger logger, string roleName, Guid userId, Guid tenantId);

    /// <summary>Logs that the person already held the role, so nothing was written.</summary>
    [MessageLogging(EventId = 12721, Level = LogLevel.Information, Message = "User '{userId}' already holds role '{roleName}' in tenant '{tenantId}'; nothing was written")]
    public static partial IGenericMessage AlreadyHeld(ILogger logger, string roleName, Guid userId, Guid tenantId);

    /// <summary>Logs that the grant could not be saved.</summary>
    [MessageLogging(EventId = 22720, Level = LogLevel.Warning, Message = "Role '{roleName}' could not be given to user '{userId}' in tenant '{tenantId}': {reason}")]
    public static partial IGenericMessage NotSaved(ILogger logger, string roleName, Guid userId, Guid tenantId, string? reason);

    /// <summary>Logs that the role to give does not exist.</summary>
    [MessageLogging(EventId = 32720, Level = LogLevel.Warning, Message = "Role '{roleName}' does not exist, so it cannot be given")]
    public static partial IGenericMessage RoleNotFound(ILogger logger, string roleName);

    /// <summary>Logs that the role to give could not be read.</summary>
    [MessageLogging(EventId = 72721, Level = LogLevel.Error, Message = "Role '{roleName}' could not be read: {reason}")]
    public static partial IGenericMessage RoleNotRead(ILogger logger, string roleName, string? reason);

    /// <summary>Logs that the roles the person holds could not be read.</summary>
    [MessageLogging(EventId = 72720, Level = LogLevel.Error, Message = "The roles user '{userId}' holds in tenant '{tenantId}' could not be read: {reason}")]
    public static partial IGenericMessage HeldRolesNotRead(ILogger logger, Guid userId, Guid tenantId, string? reason);
}
