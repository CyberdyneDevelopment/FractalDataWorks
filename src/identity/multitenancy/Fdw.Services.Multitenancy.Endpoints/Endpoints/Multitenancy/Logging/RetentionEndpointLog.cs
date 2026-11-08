using System;
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

    /// <summary>Logs that The retention run for organization organizationId could not record its end in the audit log.</summary>
    [MessageLogging(EventId = 90001, Level = LogLevel.Error, Message = "The retention run for organization {organizationId} could not record its end in the audit log", ClientMessage = "The retention run finished but its end could not be recorded")]
    public static partial IGenericMessage RunRetentionAuditNotEnded(ILogger logger, Guid organizationId);

    /// <summary>Logs that The request to lift legal hold was refused: the caller is not allowed.</summary>
    [MessageLogging(EventId = 103112, Level = LogLevel.Warning, Message = "The request to lift legal hold was refused: the caller is not allowed", ClientMessage = "You are not allowed to lift legal hold")]
    public static partial IGenericMessage LiftLegalHoldForbidden(ILogger logger);

    /// <summary>Logs that The request to place legal hold was refused: the caller is not allowed.</summary>
    [MessageLogging(EventId = 103113, Level = LogLevel.Warning, Message = "The request to place legal hold was refused: the caller is not allowed", ClientMessage = "You are not allowed to place legal hold")]
    public static partial IGenericMessage PlaceLegalHoldForbidden(ILogger logger);
}
