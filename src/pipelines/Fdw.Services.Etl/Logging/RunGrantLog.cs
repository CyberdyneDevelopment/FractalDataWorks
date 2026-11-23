using System;
using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Etl.Logging;

/// <summary>
/// MessageLogging for the stored grants of runs (FDW-6144): the refusals of the grant service. Every refusal is logged and returned in the failure result, numbered in the category of what it says: a
/// request that is not valid (2), a grant that does not exist (3) and a configuration row that cannot be read or written (7). EventId range: 21293, 21294, 21295, 31296, 71297 and 71298.
/// </summary>
[MessageLoggingTypeCode("RUNGRANT")]
public static partial class RunGrantLog
{
    /// <summary>Logs a grant asked for by a caller that is no person.</summary>
    [MessageLogging(EventId = 21293, Level = LogLevel.Error,
        Message = "A run grant was asked for by a caller that is no person, so there is nobody for the runs to act as",
        ClientMessage = "Only a signed-in person can give a grant")]
    public static partial IGenericMessage PersonMissing(ILogger logger);

    /// <summary>Logs a grant whose end is not in the future.</summary>
    [MessageLogging(EventId = 21294, Level = LogLevel.Error,
        Message = "A run grant for person {personId} was asked to end at {expiresAt}, which is not after {now}",
        ClientMessage = "A grant must end later than now")]
    public static partial IGenericMessage ExpiryNotFuture(ILogger logger, Guid personId, DateTimeOffset expiresAt, DateTimeOffset now);

    /// <summary>Logs a grant that allows nothing.</summary>
    [MessageLogging(EventId = 21295, Level = LogLevel.Error,
        Message = "A run grant for person {personId} states no permission, so it would allow nothing",
        ClientMessage = "A grant must state at least one permission")]
    public static partial IGenericMessage PermissionsMissing(ILogger logger, Guid personId);

    /// <summary>Logs a grant asked for, or revoked, that does not exist in the caller's tenant.</summary>
    [MessageLogging(EventId = 31296, Level = LogLevel.Error,
        Message = "Run grant {grantId} is not a grant of the caller's tenant",
        ClientMessage = "That grant does not exist")]
    public static partial IGenericMessage NotFound(ILogger logger, Guid grantId);

    /// <summary>Logs a grant that could not be read.</summary>
    [MessageLogging(EventId = 71297, Level = LogLevel.Error,
        Message = "Run grant {grantId} could not be read: {reason}")]
    public static partial IGenericMessage NotRead(ILogger logger, Guid grantId, string? reason);

    /// <summary>Logs a grant that could not be written or revoked.</summary>
    [MessageLogging(EventId = 71298, Level = LogLevel.Error,
        Message = "Run grant {grantId} could not be written or revoked: {reason}")]
    public static partial IGenericMessage NotWritten(ILogger logger, Guid grantId, string? reason);
}
