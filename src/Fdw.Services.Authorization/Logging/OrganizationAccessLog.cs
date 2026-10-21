using System;
using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Authorization.Logging;

/// <summary>
/// MessageLogging for the roles and members of an organization (FDW-1593).
/// EventId ranges: 12750-12754 (done), 22750-22751 (refused as invalid, 400), 32750-32752 (not found, 404), 42750-42752 (conflict, 409),
/// 52750 (not signed in, 401), 72750-72752 (store failures, 500), 102750-102751 (signed in but not allowed here, 403).
/// </summary>
[MessageLoggingTypeCode("AUTHORIZATION")]
public static partial class OrganizationAccessLog
{
    /// <summary>Logs that a role was defined.</summary>
    [MessageLogging(EventId = 12750, Level = LogLevel.Information, Message = "Role '{roleName}' was defined in organization '{organizationId}' with {permissionCount} permissions")]
    public static partial IGenericMessage RoleDefined(ILogger logger, string roleName, Guid organizationId, int permissionCount);

    /// <summary>Logs that a role was changed.</summary>
    [MessageLogging(EventId = 12751, Level = LogLevel.Information, Message = "Role '{roleName}' of organization '{organizationId}' was changed; {holders} people hold it and were refreshed")]
    public static partial IGenericMessage RoleRedefined(ILogger logger, string roleName, Guid organizationId, int holders);

    /// <summary>Logs that a role was deleted.</summary>
    [MessageLogging(EventId = 12752, Level = LogLevel.Information, Message = "Role '{roleName}' of organization '{organizationId}' was deleted")]
    public static partial IGenericMessage RoleDeleted(ILogger logger, string roleName, Guid organizationId);

    /// <summary>Logs that a person was given a role in an organization.</summary>
    [MessageLogging(EventId = 12753, Level = LogLevel.Information, Message = "User '{userId}' was given role '{roleName}' in organization '{organizationId}'")]
    public static partial IGenericMessage MemberGiven(ILogger logger, Guid userId, string roleName, Guid organizationId);

    /// <summary>Logs that a person was taken out of an organization.</summary>
    [MessageLogging(EventId = 12754, Level = LogLevel.Information, Message = "User '{userId}' was removed from organization '{organizationId}'")]
    public static partial IGenericMessage MemberRemoved(ILogger logger, Guid userId, Guid organizationId);

    /// <summary>Logs that a role was refused because it has no name.</summary>
    [MessageLogging(EventId = 22750, Level = LogLevel.Warning, Message = "A role of organization '{organizationId}' was refused: it has no name")]
    public static partial IGenericMessage RoleNameRequired(ILogger logger, Guid organizationId);

    /// <summary>Logs that a role was refused because it carries no permission.</summary>
    [MessageLogging(EventId = 22751, Level = LogLevel.Warning, Message = "Role '{roleName}' of organization '{organizationId}' was refused: a role carries at least one permission, because a person is in an organization by holding one of its access rows")]
    public static partial IGenericMessage RoleNeedsPermission(ILogger logger, string roleName, Guid organizationId);

    /// <summary>Logs that the organization does not exist.</summary>
    [MessageLogging(EventId = 32750, Level = LogLevel.Warning, Message = "Organization '{organizationId}' does not exist")]
    public static partial IGenericMessage OrganizationNotFound(ILogger logger, Guid organizationId);

    /// <summary>Logs that the role does not exist in the organization.</summary>
    [MessageLogging(EventId = 32751, Level = LogLevel.Warning, Message = "Organization '{organizationId}' has no role '{roleName}'")]
    public static partial IGenericMessage RoleNotFound(ILogger logger, Guid organizationId, string roleName);

    /// <summary>Logs that the person is not in the organization.</summary>
    [MessageLogging(EventId = 32752, Level = LogLevel.Warning, Message = "User '{userId}' is not in organization '{organizationId}'")]
    public static partial IGenericMessage MemberNotFound(ILogger logger, Guid userId, Guid organizationId);

    /// <summary>Logs that a role already exists in the organization.</summary>
    [MessageLogging(EventId = 42750, Level = LogLevel.Warning, Message = "Organization '{organizationId}' already has a role '{roleName}'")]
    public static partial IGenericMessage RoleExists(ILogger logger, Guid organizationId, string roleName);

    /// <summary>Logs that a role was kept because people hold it.</summary>
    [MessageLogging(EventId = 42751, Level = LogLevel.Warning, Message = "Role '{roleName}' of organization '{organizationId}' was kept: {holders} people hold it")]
    public static partial IGenericMessage RoleHeld(ILogger logger, string roleName, Guid organizationId, int holders);

    /// <summary>Logs that a built-in role was kept.</summary>
    [MessageLogging(EventId = 42752, Level = LogLevel.Warning, Message = "Role '{roleName}' of organization '{organizationId}' was kept: the organization was made with it")]
    public static partial IGenericMessage RoleBuiltIn(ILogger logger, string roleName, Guid organizationId);

    /// <summary>Logs that nobody is signed in.</summary>
    [MessageLogging(EventId = 52750, Level = LogLevel.Warning, Message = "An organization change was refused: the caller is not signed in")]
    public static partial IGenericMessage CallerRequired(ILogger logger);

    /// <summary>Logs that a read from the store failed.</summary>
    [MessageLogging(EventId = 72750, Level = LogLevel.Error, Message = "{what} could not be read: {reason}")]
    public static partial IGenericMessage ReadFailed(ILogger logger, string what, string? reason);

    /// <summary>Logs that a write to the store failed.</summary>
    [MessageLogging(EventId = 72751, Level = LogLevel.Error, Message = "{what} could not be written: {reason}")]
    public static partial IGenericMessage WriteFailed(ILogger logger, string what, string? reason);

    /// <summary>Logs that a role was changed but someone who holds it could not be refreshed, so their access may be out of date.</summary>
    [MessageLogging(EventId = 72752, Level = LogLevel.Critical, Message = "Role '{roleName}' of organization '{organizationId}' was changed but the access of user '{userId}' could not be refreshed: {reason}")]
    public static partial IGenericMessage HolderNotRefreshed(ILogger logger, string roleName, Guid organizationId, Guid userId, string? reason);

    /// <summary>Logs that the caller does not act in the organization.</summary>
    [MessageLogging(EventId = 102750, Level = LogLevel.Warning, Message = "An organization change was refused: it is for organization '{organizationId}', which the caller does not act in")]
    public static partial IGenericMessage NotActingInOrganization(ILogger logger, Guid organizationId);

    /// <summary>Logs that the organization belongs to a tenant the caller does not act in.</summary>
    [MessageLogging(EventId = 102751, Level = LogLevel.Warning, Message = "An organization change was refused: organization '{organizationId}' belongs to tenant '{tenantId}', which the caller does not act in")]
    public static partial IGenericMessage OtherTenant(ILogger logger, Guid organizationId, Guid tenantId);
}
