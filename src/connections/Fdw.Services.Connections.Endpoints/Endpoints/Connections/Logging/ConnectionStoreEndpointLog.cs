using System;
using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Connections.Endpoints.Logging;

/// <summary>
/// MessageLogging for the endpoints that grant connections to tenants and assign them to organizations (FDW-1581).
/// EventId ranges: 72820 (a change was made but not recorded in the audit, 500), 102820-102821 (the caller may not, 403).
/// </summary>
[MessageLoggingTypeCode("ENDPOINTS6")]
public static partial class ConnectionStoreEndpointLog
{
    /// <summary>Logs that a change was made but could not be recorded in the audit.</summary>
    [MessageLogging(EventId = 72820, Level = LogLevel.Error, Message = "The {changeKind} of '{itemId}' was made but could not be recorded in the administrative audit: {reason}")]
    public static partial IGenericMessage ChangeNotAudited(ILogger logger, string changeKind, string itemId, string? reason);

    /// <summary>Logs that a grant change was refused because the caller is not a platform administrator.</summary>
    [MessageLogging(EventId = 102820, Level = LogLevel.Warning, Message = "A connection grant change for tenant '{tenantId}' was refused: the caller is not a platform administrator")]
    public static partial IGenericMessage PlatformAdministratorRequired(ILogger logger, Guid tenantId);

    /// <summary>Logs that a read of a tenant's grants was refused because the caller acts in another tenant.</summary>
    [MessageLogging(EventId = 102821, Level = LogLevel.Warning, Message = "Reading the connection grants of tenant '{tenantId}' was refused: the caller does not act in it and is not a platform administrator")]
    public static partial IGenericMessage OtherTenant(ILogger logger, Guid tenantId);

    /// <summary>Logs that The request to grant connection was refused: the caller is not allowed.</summary>
    [MessageLogging(EventId = 102822, Level = LogLevel.Warning, Message = "The request to grant connection was refused: the caller is not allowed", ClientMessage = "You are not allowed to grant connection")]
    public static partial IGenericMessage GrantConnectionForbidden(ILogger logger);

    /// <summary>Logs that The request to revoke connection grant was refused: the caller is not allowed.</summary>
    [MessageLogging(EventId = 102823, Level = LogLevel.Warning, Message = "The request to revoke connection grant was refused: the caller is not allowed", ClientMessage = "You are not allowed to revoke connection grant")]
    public static partial IGenericMessage RevokeConnectionGrantForbidden(ILogger logger);

    /// <summary>Logs that The request to list connection grants was refused: the caller is not allowed.</summary>
    [MessageLogging(EventId = 102824, Level = LogLevel.Warning, Message = "The request to list connection grants was refused: the caller is not allowed", ClientMessage = "You are not allowed to list connection grants")]
    public static partial IGenericMessage ListConnectionGrantsForbidden(ILogger logger);
}
