using System;
using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Multitenancy.Endpoints.Logging;

/// <summary>
/// MessageLogging for the endpoints that make and change tenants (FDW-1578).
/// EventId ranges: 12740-12741 (made, changed), 72740 (a change was made but not recorded in the audit, 500), 102740 (the caller is not a
/// platform administrator, 403).
/// </summary>
[MessageLoggingTypeCode("TENANTENDPOINTS")]
public static partial class TenantEndpointLog
{
    /// <summary>Logs that a tenant was made.</summary>
    [MessageLogging(EventId = 12740, Level = LogLevel.Information, Message = "Tenant '{tenantId}' ('{slug}') was made with user '{administratorId}' as its first administrator")]
    public static partial IGenericMessage TenantMade(ILogger logger, Guid tenantId, string slug, Guid administratorId);

    /// <summary>Logs that a tenant was changed.</summary>
    [MessageLogging(EventId = 12741, Level = LogLevel.Information, Message = "Tenant '{tenantId}' was changed: {changes}")]
    public static partial IGenericMessage TenantChanged(ILogger logger, Guid tenantId, string changes);

    /// <summary>Logs that a tenant was changed but the change could not be recorded in the audit.</summary>
    [MessageLogging(EventId = 72740, Level = LogLevel.Error, Message = "Tenant '{tenantId}' was changed but the change could not be recorded in the audit: {reason}")]
    public static partial IGenericMessage ChangeNotAudited(ILogger logger, Guid tenantId, string? reason);

    /// <summary>Logs that a tenant change was refused because the caller is not a platform administrator.</summary>
    [MessageLogging(EventId = 102740, Level = LogLevel.Warning, Message = "A tenant change was refused: the caller is not a platform administrator")]
    public static partial IGenericMessage PlatformAdministratorRequired(ILogger logger);

    /// <summary>Logs that a list of deleted tenants was refused because the caller is not a platform administrator.</summary>
    [MessageLogging(EventId = 102741, Level = LogLevel.Warning,
        Message = "A list of deleted tenants was refused: the caller is not a platform administrator",
        ClientMessage = "Only a platform administrator can list deleted companies")]
    public static partial IGenericMessage DeletedTenantsRequirePlatformAdministrator(ILogger logger);
}
