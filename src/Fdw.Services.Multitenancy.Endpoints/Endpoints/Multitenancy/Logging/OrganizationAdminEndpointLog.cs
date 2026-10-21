using System;
using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Multitenancy.Endpoints.Logging;

/// <summary>
/// MessageLogging for the endpoints that make and change organizations (FDW-1758).
/// EventId ranges: 12760-12761 (made, changed), 72760 (a change was made but not recorded in the audit, 500), 102760-102761 (the caller
/// may not, 403).
/// </summary>
[MessageLoggingTypeCode("TENANTENDPOINTS")]
public static partial class OrganizationAdminEndpointLog
{
    /// <summary>Logs that an organization was made.</summary>
    [MessageLogging(EventId = 12760, Level = LogLevel.Information, Message = "Organization '{organizationId}' was made in tenant '{tenantId}'")]
    public static partial IGenericMessage OrganizationMade(ILogger logger, Guid organizationId, Guid tenantId);

    /// <summary>Logs that an organization was changed.</summary>
    [MessageLogging(EventId = 12761, Level = LogLevel.Information, Message = "Organization '{organizationId}' was changed: {changes}")]
    public static partial IGenericMessage OrganizationChanged(ILogger logger, Guid organizationId, string changes);

    /// <summary>Logs that a change was made but could not be recorded in the administrative audit.</summary>
    [MessageLogging(EventId = 72760, Level = LogLevel.Error, Message = "The change to organization '{organizationId}' was made but could not be recorded in the administrative audit: {reason}")]
    public static partial IGenericMessage ChangeNotAudited(ILogger logger, Guid organizationId, string? reason);

    /// <summary>Logs that a caller who acts in no tenant tried to make an organization.</summary>
    [MessageLogging(EventId = 102760, Level = LogLevel.Warning, Message = "An organization was not made: the caller acts in no tenant")]
    public static partial IGenericMessage CallerActsInNoTenant(ILogger logger);

    /// <summary>Logs that the organization of another tenant was refused.</summary>
    [MessageLogging(EventId = 102761, Level = LogLevel.Warning, Message = "A change to an organization of tenant '{tenantId}' was refused: the caller does not act in it and is not a platform administrator")]
    public static partial IGenericMessage OtherTenant(ILogger logger, Guid tenantId);
}
