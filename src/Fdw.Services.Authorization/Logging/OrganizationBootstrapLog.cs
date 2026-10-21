using System;
using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Authorization.Logging;

/// <summary>
/// MessageLogging for giving a new organization its starting roles and first administrator (FDW-1758).
/// EventId ranges: 12760-12761 (done), 22760 (the organization to copy has no roles, 400).
/// </summary>
[MessageLoggingTypeCode("AUTHORIZATION")]
public static partial class OrganizationBootstrapLog
{
    /// <summary>Logs that an organization was given a copy of the roles of another.</summary>
    [MessageLogging(EventId = 12760, Level = LogLevel.Information, Message = "Organization '{toOrganizationId}' was given {count} roles copied from organization '{fromOrganizationId}'")]
    public static partial IGenericMessage RolesCopied(ILogger logger, Guid fromOrganizationId, Guid toOrganizationId, int count);

    /// <summary>Logs that the roles and members of an organization were removed again.</summary>
    [MessageLogging(EventId = 12761, Level = LogLevel.Information, Message = "The roles and members of organization '{organizationId}' were removed again")]
    public static partial IGenericMessage Cleared(ILogger logger, Guid organizationId);

    /// <summary>Logs that the organization to copy the roles from has none.</summary>
    [MessageLogging(EventId = 22760, Level = LogLevel.Warning, Message = "Organization '{organizationId}' has no roles to copy")]
    public static partial IGenericMessage NothingToCopy(ILogger logger, Guid organizationId);
}
