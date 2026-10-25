using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Multitenancy.Endpoints.Logging;

/// <summary>
/// MessageLogging for the endpoints over retention policies, legal holds and the retention job (FDW-1575).
/// EventId ranges: 73110 (a change was made but not recorded in the audit, 500), 103111 (the caller may not, 403).
/// </summary>
[MessageLoggingTypeCode("TENANTENDPOINTS")]
public static partial class RetentionEndpointLog
{
    /// <summary>Logs that a retention change was made but could not be recorded in the administrative audit.</summary>
    [MessageLogging(EventId = 73110, Level = LogLevel.Error, Message = "The change '{changeKind}' of '{itemId}' was made but could not be recorded in the administrative audit: {reason}")]
    public static partial IGenericMessage ChangeNotAudited(ILogger logger, string changeKind, string itemId, string? reason);

    /// <summary>Logs that a hold was refused because the caller's token names no user.</summary>
    [MessageLogging(EventId = 103111, Level = LogLevel.Warning, Message = "A legal hold change was refused: the caller's token names no user")]
    public static partial IGenericMessage CallerNotIdentified(ILogger logger);
}
