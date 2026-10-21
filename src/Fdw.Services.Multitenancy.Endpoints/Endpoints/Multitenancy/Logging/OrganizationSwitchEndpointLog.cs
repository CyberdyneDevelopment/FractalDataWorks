using System;
using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Multitenancy.Endpoints.Logging;

/// <summary>
/// MessageLogging for the endpoint that switches the department a person acts in (FDW-1802).
/// EventId ranges: 12770 (done), 72770 (switched but not recorded in the audit, 500), 102770-102772 (the caller may not, 403).
/// </summary>
[MessageLoggingTypeCode("TENANTENDPOINTS")]
public static partial class OrganizationSwitchEndpointLog
{
    /// <summary>Logs that a person now acts in a department.</summary>
    [MessageLogging(EventId = 12770, Level = LogLevel.Information, Message = "User '{userId}' now acts in department '{organizationId}' of tenant '{tenantId}'")]
    public static partial IGenericMessage Switched(ILogger logger, Guid userId, Guid organizationId, Guid tenantId);

    /// <summary>Logs that a department switch was made but could not be recorded in the administrative audit.</summary>
    [MessageLogging(EventId = 72770, Level = LogLevel.Error, Message = "The switch of user '{userId}' to department '{organizationId}' was made but could not be recorded in the administrative audit: {reason}")]
    public static partial IGenericMessage SwitchNotAudited(ILogger logger, Guid userId, Guid organizationId, string? reason);

    /// <summary>Logs that the caller's token names no user or no tenant.</summary>
    [MessageLogging(EventId = 102770, Level = LogLevel.Warning, Message = "A department switch was refused: the caller's token names no user or no tenant")]
    public static partial IGenericMessage CallerNotIdentified(ILogger logger);

    /// <summary>Logs that a department of another tenant was asked for.</summary>
    [MessageLogging(EventId = 102771, Level = LogLevel.Warning, Message = "User '{userId}' asked to act in department '{organizationId}', which belongs to another tenant than '{tenantId}' they act in")]
    public static partial IGenericMessage OtherTenant(ILogger logger, Guid userId, Guid organizationId, Guid tenantId);

    /// <summary>Logs that a person was refused a department they hold no access in.</summary>
    [MessageLogging(EventId = 102772, Level = LogLevel.Warning, Message = "User '{userId}' was refused department '{organizationId}': they may not act in it")]
    public static partial IGenericMessage AccessDenied(ILogger logger, Guid userId, Guid organizationId);
}
