using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Multitenancy.Endpoints.Logging;

/// <summary>
/// MessageLogging for the endpoints over the feature switches (FDW-1580).
/// EventId ranges: 72750 (a switch was made but not recorded in the audit, 500).
/// </summary>
[MessageLoggingTypeCode("TENANTENDPOINTS")]
public static partial class FeatureEndpointLog
{
    /// <summary>Logs that a feature switch was made but could not be recorded in the administrative audit.</summary>
    [MessageLogging(EventId = 72750, Level = LogLevel.Error, Message = "The switch of feature '{feature}' for '{itemId}' was made but could not be recorded in the administrative audit: {reason}")]
    public static partial IGenericMessage SwitchNotAudited(ILogger logger, string feature, string itemId, string? reason);
}
