using System;
using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Settings.Endpoints.Logging;

/// <summary>
/// MessageLogging for the endpoints over inherited and locked settings (FDW-1579).
/// EventId ranges: 72910 (a change was made but not recorded in the audit, 500), 102910-102912 (the caller may not, 403).
/// </summary>
[MessageLoggingTypeCode("ENDPOINTS12")]
public static partial class InheritedSettingsEndpointLog
{
    /// <summary>Logs that a settings change was made but could not be recorded in the administrative audit.</summary>
    [MessageLogging(EventId = 72910, Level = LogLevel.Error, Message = "The {changeKind} of setting '{itemId}' was made but could not be recorded in the administrative audit: {reason}")]
    public static partial IGenericMessage ChangeNotAudited(ILogger logger, string changeKind, string itemId, string? reason);

    /// <summary>Logs that a platform setting change was refused for a caller who is not a platform administrator.</summary>
    [MessageLogging(EventId = 102910, Level = LogLevel.Warning, Message = "A change to the {level} settings was refused: the caller is not a platform administrator")]
    public static partial IGenericMessage PlatformAdministratorRequired(ILogger logger, string level);

    /// <summary>Logs that the settings of another tenant were refused.</summary>
    [MessageLogging(EventId = 102911, Level = LogLevel.Warning, Message = "The settings of tenant '{tenantId}' were refused: the caller does not act in it and is not a platform administrator")]
    public static partial IGenericMessage OtherTenant(ILogger logger, Guid tenantId);

    /// <summary>Logs that The request to list tenant inherited settings was refused: the caller is not allowed.</summary>
    [MessageLogging(EventId = 102912, Level = LogLevel.Warning, Message = "The request to list tenant inherited settings was refused: the caller is not allowed", ClientMessage = "You are not allowed to list tenant inherited settings")]
    public static partial IGenericMessage ListTenantInheritedSettingsForbidden(ILogger logger);

    /// <summary>Logs that The request to list platform inherited settings was refused: the caller is not allowed.</summary>
    [MessageLogging(EventId = 102913, Level = LogLevel.Warning, Message = "The request to list platform inherited settings was refused: the caller is not allowed", ClientMessage = "You are not allowed to list platform inherited settings")]
    public static partial IGenericMessage ListPlatformInheritedSettingsForbidden(ILogger logger);

    /// <summary>Logs that The request to set tenant inherited setting was refused: the caller is not allowed.</summary>
    [MessageLogging(EventId = 102914, Level = LogLevel.Warning, Message = "The request to set tenant inherited setting was refused: the caller is not allowed", ClientMessage = "You are not allowed to set tenant inherited setting")]
    public static partial IGenericMessage SetTenantInheritedSettingForbidden(ILogger logger);

    /// <summary>Logs that The request to set platform inherited setting was refused: the caller is not allowed.</summary>
    [MessageLogging(EventId = 102915, Level = LogLevel.Warning, Message = "The request to set platform inherited setting was refused: the caller is not allowed", ClientMessage = "You are not allowed to set platform inherited setting")]
    public static partial IGenericMessage SetPlatformInheritedSettingForbidden(ILogger logger);

    /// <summary>Logs that The request to lift tenant setting lock was refused: the caller is not allowed.</summary>
    [MessageLogging(EventId = 102916, Level = LogLevel.Warning, Message = "The request to lift tenant setting lock was refused: the caller is not allowed", ClientMessage = "You are not allowed to lift tenant setting lock")]
    public static partial IGenericMessage LiftTenantSettingLockForbidden(ILogger logger);
}
