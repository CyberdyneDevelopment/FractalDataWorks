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
}
