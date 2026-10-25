using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Settings.Endpoints.Logging;

/// <summary>
/// MessageLogging for the home page endpoints (FDW-2471).
/// EventId range: 72920 (a change was made but not recorded in the audit, 500).
/// </summary>
[MessageLoggingTypeCode("ENDPOINTS12")]
public static partial class HomePageEndpointLog
{
    /// <summary>Logs that a home page change was made but could not be recorded in the administrative audit.</summary>
    [MessageLogging(EventId = 72920, Level = LogLevel.Error, Message = "The {changeKind} of home page '{itemId}' was made but could not be recorded in the administrative audit: {reason}")]
    public static partial IGenericMessage ChangeNotAudited(ILogger logger, string changeKind, string itemId, string? reason);
}
