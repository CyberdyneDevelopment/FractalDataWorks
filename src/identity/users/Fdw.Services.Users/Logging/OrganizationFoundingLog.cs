using System;
using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Users.Logging;

/// <summary>
/// MessageLogging for making an organization with its first administrator (FDW-1758).
/// EventId ranges: 12735 (made), 32735-32736 (the person has no account, the organization to copy is not found, 404), 72735-72738 (the
/// roles or the first administrator could not be set, the organization could not be removed again, its default workspace could not be
/// made), 102735-102736 (not the tenant's own, 403).
/// </summary>
[MessageLoggingTypeCode("USERS")]
public static partial class OrganizationFoundingLog
{
    /// <summary>Logs that the default workspace of a new organization could not be made (FDW-3668).</summary>
    /// <param name="logger">The logger that records the event.</param>
    /// <param name="organizationId">The new organization.</param>
    /// <param name="reason">Why.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(EventId = 72738, Level = LogLevel.Error, Message = "The default workspace of the new organization '{organizationId}' could not be made: {reason}; the organization is being removed again")]
    public static partial IGenericMessage DefaultWorkspaceNotMade(ILogger logger, Guid organizationId, string? reason);

    /// <summary>Logs that an organization was made with its first administrator.</summary>
    [MessageLogging(EventId = 12735, Level = LogLevel.Information, Message = "Organization '{organizationId}' of tenant '{tenantId}' was made with user '{administratorId}' as its first administrator, holding role '{roleName}'")]
    public static partial IGenericMessage Founded(ILogger logger, Guid organizationId, Guid tenantId, Guid administratorId, string roleName);

    /// <summary>Logs that the named first administrator has no account.</summary>
    [MessageLogging(EventId = 32735, Level = LogLevel.Warning, Message = "An organization was not made: user '{administratorId}', named as its first administrator, has no account")]
    public static partial IGenericMessage AdministratorNotFound(ILogger logger, Guid administratorId);

    /// <summary>Logs that the organization whose roles are to be copied does not exist.</summary>
    [MessageLogging(EventId = 32736, Level = LogLevel.Warning, Message = "An organization was not made: organization '{organizationId}', whose roles are to be copied, does not exist")]
    public static partial IGenericMessage TemplateNotFound(ILogger logger, Guid organizationId);

    /// <summary>Logs that the new organization's roles could not be copied; the organization is removed again.</summary>
    [MessageLogging(EventId = 72735, Level = LogLevel.Error, Message = "The roles of organization '{templateId}' could not be copied to the new organization '{organizationId}': {reason}; the organization is being removed again")]
    public static partial IGenericMessage RolesNotCopied(ILogger logger, Guid templateId, Guid organizationId, string? reason);

    /// <summary>Logs that the first administrator could not be given the role in the new organization; the organization is removed again.</summary>
    [MessageLogging(EventId = 72736, Level = LogLevel.Error, Message = "User '{administratorId}' could not be given role '{roleName}' in the new organization '{organizationId}': {reason}; the organization is being removed again")]
    public static partial IGenericMessage RoleNotGiven(ILogger logger, Guid administratorId, string roleName, Guid organizationId, string? reason);

    /// <summary>Logs that something the failed start made was not removed again.</summary>
    [MessageLogging(EventId = 72737, Level = LogLevel.Critical, Message = "Organization '{organizationId}' was left part-made: its {what} could not be removed: {reason}")]
    public static partial IGenericMessage NotUndone(ILogger logger, Guid organizationId, string what, string? reason);

    /// <summary>Logs that the person named as first administrator is not a member of the tenant.</summary>
    [MessageLogging(EventId = 102735, Level = LogLevel.Warning, Message = "An organization was not made: user '{administratorId}', named as its first administrator, is not a member of tenant '{tenantId}'")]
    public static partial IGenericMessage AdministratorNotInTenant(ILogger logger, Guid administratorId, Guid tenantId);

    /// <summary>Logs that the organization whose roles are to be copied belongs to another tenant.</summary>
    [MessageLogging(EventId = 102736, Level = LogLevel.Warning, Message = "An organization was not made in tenant '{tenantId}': organization '{templateId}', whose roles are to be copied, belongs to another tenant")]
    public static partial IGenericMessage TemplateInOtherTenant(ILogger logger, Guid tenantId, Guid templateId);

    /// <summary>Error: founding an organization needs the founder's role name; an empty name is refused.</summary>
    [MessageLogging(EventId = 21800, Level = LogLevel.Error,
        Message = "Founding an organization needs the name of the role its founder holds, and the name given is empty")]
    public static partial IGenericMessage FounderRoleNameRequired(ILogger logger);
}
