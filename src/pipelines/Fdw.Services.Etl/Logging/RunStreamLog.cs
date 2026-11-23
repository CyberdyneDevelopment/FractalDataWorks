using System;
using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Etl.Logging;

/// <summary>
/// MessageLogging for the pieces a run gives its person (FDW-6140). A reader who is not the person and not in the audience is refused (4); a store that is not named or a setting that is not set is a host that is
/// wrong (6); a piece that cannot be written or read is a dependency that failed (7). EventId range: 40850 to 40852, 60854, 60855, 70853, 70854 and 70856 (a warning).
/// </summary>
[MessageLoggingTypeCode("PIPERUNSTREAM")]
public static partial class RunStreamLog
{
    /// <summary>Logs a read of the pieces of a run by a person they are not addressed to.</summary>
    [MessageLogging(EventId = 40850, Level = LogLevel.Error,
        Message = "Person {person} asked for the pieces of run chain {claimRootId}, which are addressed to another person, and the host's audience does not name them",
        ClientMessage = "That run's pieces are not yours")]
    public static partial IGenericMessage NotYours(ILogger logger, Guid person, Guid claimRootId);

    /// <summary>Logs a read of the pieces of a run by a caller that is no person.</summary>
    [MessageLogging(EventId = 40851, Level = LogLevel.Error,
        Message = "The pieces of a run are read for a person, and the caller is not one",
        ClientMessage = "Only a person follows a run")]
    public static partial IGenericMessage CallerIsNoPerson(ILogger logger);

    /// <summary>Logs a read of the pieces of a run by a caller with no tenant.</summary>
    [MessageLogging(EventId = 40852, Level = LogLevel.Error,
        Message = "The pieces of a run are read in a tenant, and the caller has none",
        ClientMessage = "A run is followed in a tenant, and the caller has none")]
    public static partial IGenericMessage CallerHasNoTenant(ILogger logger);

    /// <summary>Logs a host that names no operational store.</summary>
    [MessageLogging(EventId = 60854, Level = LogLevel.Error,
        Message = "The pieces of a run cannot be kept: no operational store is named",
        ClientMessage = "The host cannot keep the pieces of a run")]
    public static partial IGenericMessage NoStore(ILogger logger);

    /// <summary>Logs a stream setting that is not set to a positive number.</summary>
    [MessageLogging(EventId = 60855, Level = LogLevel.Error,
        Message = "The setting {setting} is not set to a positive number, so the pieces of a run are not {action}",
        ClientMessage = "The host's stream settings are not set")]
    public static partial IGenericMessage SettingNotSet(ILogger logger, string setting, string action);

    /// <summary>Logs a piece that could not be written.</summary>
    [MessageLogging(EventId = 70853, Level = LogLevel.Error,
        Message = "A piece of kind {kind} of run {executionId} could not be written: {reason}",
        ClientMessage = "A piece of the run could not be written")]
    public static partial IGenericMessage NotWritten(ILogger logger, string kind, Guid executionId, string? reason);

    /// <summary>Logs a piece that was kept and could not be pushed: the person gets it by asking the stream from the last sequence they hold.</summary>
    [MessageLogging(EventId = 70856, Level = LogLevel.Warning,
        Message = "The piece {pieceId} of run {executionId} was kept and was not pushed to the person: {reason}",
        ClientMessage = "A piece of the run was kept and not pushed")]
    public static partial IGenericMessage NotPushed(ILogger logger, Guid pieceId, Guid executionId, string? reason);

    /// <summary>Logs the pieces of a run chain that could not be read.</summary>
    [MessageLogging(EventId = 70854, Level = LogLevel.Error,
        Message = "The pieces of run chain {claimRootId} could not be read: {reason}",
        ClientMessage = "The pieces of the run could not be read")]
    public static partial IGenericMessage NotRead(ILogger logger, Guid claimRootId, string? reason);
}
