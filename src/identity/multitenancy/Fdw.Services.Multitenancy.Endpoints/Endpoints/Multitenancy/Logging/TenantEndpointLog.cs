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

    /// <summary>Logs that the request failed: IsSelectable is required.</summary>
    [MessageLogging(EventId = 20001, Level = LogLevel.Warning, Message = "IsSelectable is required", ClientMessage = "IsSelectable is required")]
    public static partial IGenericMessage CreateTenantIsselectableIsRequired(ILogger logger);

    /// <summary>Logs that Tenant was not found, or is not visible to the caller.</summary>
    [MessageLogging(EventId = 30001, Level = LogLevel.Warning, Message = "Tenant was not found, or is not visible to the caller", ClientMessage = "Tenant was not found")]
    public static partial IGenericMessage UpdateTenantTenantNotFound(ILogger logger);

    /// <summary>Logs that Tenant tenantId was not found, or is not visible to the caller.</summary>
    [MessageLogging(EventId = 30002, Level = LogLevel.Warning, Message = "Tenant {tenantIdentifier} was not found, or is not visible to the caller", ClientMessage = "Tenant {tenantIdentifier} was not found")]
    public static partial IGenericMessage GetTenantTenantNotFound(ILogger logger, string tenantIdentifier);

    /// <summary>Logs that The caller's token carries no usable tenant claim, so there is no current tenant.</summary>
    [MessageLogging(EventId = 30003, Level = LogLevel.Warning, Message = "The caller's token carries no usable tenant claim, so there is no current tenant", ClientMessage = "You have no current tenant")]
    public static partial IGenericMessage GetCurrentTenantNoTenantClaim(ILogger logger);

    /// <summary>Logs that The caller's tenant tenantId was not found.</summary>
    [MessageLogging(EventId = 30004, Level = LogLevel.Warning, Message = "The caller's tenant {tenantId} was not found", ClientMessage = "Your tenant {tenantId} was not found")]
    public static partial IGenericMessage GetCurrentTenantTenantNotFound(ILogger logger, Guid tenantId);

    /// <summary>Logs that Tenant tenantId to switch into was not found, or is not visible to the caller.</summary>
    [MessageLogging(EventId = 30005, Level = LogLevel.Warning, Message = "Tenant {tenantId} to switch into was not found, or is not visible to the caller", ClientMessage = "Tenant {tenantId} was not found")]
    public static partial IGenericMessage SwitchTenantTenantNotFound(ILogger logger, Guid tenantId);

    /// <summary>Logs that The first administrator 'administrator' of the new tenant was not found.</summary>
    [MessageLogging(EventId = 30006, Level = LogLevel.Warning, Message = "The first administrator '{administrator}' of the new tenant was not found", ClientMessage = "The first administrator '{administrator}' was not found")]
    public static partial IGenericMessage CreateTenantFirstAdministratorNotFound(ILogger logger, string administrator);

    /// <summary>Logs that The request to create tenant was refused: the caller is not allowed.</summary>
    [MessageLogging(EventId = 102742, Level = LogLevel.Warning, Message = "The request to create tenant was refused: the caller is not allowed", ClientMessage = "You are not allowed to create tenant")]
    public static partial IGenericMessage CreateTenantForbidden(ILogger logger);

    /// <summary>Logs that The request to update tenant was refused: the caller is not allowed.</summary>
    [MessageLogging(EventId = 102743, Level = LogLevel.Warning, Message = "The request to update tenant was refused: the caller is not allowed", ClientMessage = "You are not allowed to update tenant")]
    public static partial IGenericMessage UpdateTenantForbidden(ILogger logger);

    /// <summary>Logs that The request to switch tenant was refused: the caller is not allowed.</summary>
    [MessageLogging(EventId = 102744, Level = LogLevel.Warning, Message = "The request to switch tenant was refused: the caller is not allowed", ClientMessage = "You are not allowed to switch tenant")]
    public static partial IGenericMessage SwitchTenantForbidden(ILogger logger);

    /// <summary>Logs that The request to switch tenant was refused: the caller is not allowed.</summary>
    [MessageLogging(EventId = 102745, Level = LogLevel.Warning, Message = "The request to switch tenant was refused: the caller is not allowed", ClientMessage = "You are not allowed to switch tenant")]
    public static partial IGenericMessage SwitchTenantForbidden2(ILogger logger);

    /// <summary>Logs that The request to restore tenant was refused: the caller is not allowed.</summary>
    [MessageLogging(EventId = 102746, Level = LogLevel.Warning, Message = "The request to restore tenant was refused: the caller is not allowed", ClientMessage = "You are not allowed to restore tenant")]
    public static partial IGenericMessage RestoreTenantForbidden(ILogger logger);

    /// <summary>Logs that The request to get tenant was refused: the caller is not allowed.</summary>
    [MessageLogging(EventId = 102747, Level = LogLevel.Warning, Message = "The request to get tenant was refused: the caller is not allowed", ClientMessage = "You are not allowed to get tenant")]
    public static partial IGenericMessage GetTenantForbidden(ILogger logger);

    /// <summary>Logs that The request to delete tenant was refused: the caller is not allowed.</summary>
    [MessageLogging(EventId = 102748, Level = LogLevel.Warning, Message = "The request to delete tenant was refused: the caller is not allowed", ClientMessage = "You are not allowed to delete tenant")]
    public static partial IGenericMessage DeleteTenantForbidden(ILogger logger);
}
