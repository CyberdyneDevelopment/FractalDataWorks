using System;
using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Multitenancy.Endpoints.Logging;

/// <summary>
/// MessageLogging for the endpoints over the feature switches (FDW-1580).
/// EventId ranges: 72750 (a switch was made but not recorded in the audit, 500), 102750-102751 (the caller may not, 403).
/// </summary>
[MessageLoggingTypeCode("TENANTENDPOINTS")]
public static partial class FeatureEndpointLog
{
    /// <summary>Logs that a feature switch was made but could not be recorded in the administrative audit.</summary>
    [MessageLogging(EventId = 72750, Level = LogLevel.Error, Message = "The switch of feature '{feature}' for '{itemId}' was made but could not be recorded in the administrative audit: {reason}")]
    public static partial IGenericMessage SwitchNotAudited(ILogger logger, string feature, string itemId, string? reason);

    /// <summary>Logs that switching a feature for a tenant was refused for a caller who is not a platform administrator.</summary>
    [MessageLogging(EventId = 102750, Level = LogLevel.Warning, Message = "Switching feature '{feature}' for tenant '{tenantId}' was refused: the caller is not a platform administrator")]
    public static partial IGenericMessage PlatformAdministratorRequired(ILogger logger, string feature, Guid tenantId);

    /// <summary>Logs that the features of another tenant were refused.</summary>
    [MessageLogging(EventId = 102751, Level = LogLevel.Warning, Message = "The features of tenant '{tenantId}' were refused: the caller does not act in it and is not a platform administrator")]
    public static partial IGenericMessage OtherTenant(ILogger logger, Guid tenantId);
}
