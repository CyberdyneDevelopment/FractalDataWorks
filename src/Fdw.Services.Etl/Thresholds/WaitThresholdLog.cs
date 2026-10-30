using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Etl.Thresholds;

/// <summary>
/// MessageLogging for the thresholds on waiting pipeline runs (FDW-3161): a threshold that fires or clears, and thresholds that are stated wrongly.
/// EventId range: 22140-22157 (the leading digit of an id that surfaces as an answer names its category: 2 a refusal, 3 a missing owner, 6 a configuration fault, 9 an internal one, 4 a conflict), 32159, 42160, 62155, 92158, 92160.
/// </summary>
[MessageLoggingTypeCode("WAITTHRESHOLD")]
public static partial class WaitThresholdLog
{
    /// <summary>Logs a threshold that began to fire.</summary>
    [MessageLogging(EventId = 22140, Level = LogLevel.Warning,
        Message = "The threshold '{threshold}' of {owner} fires: {count} waiting runs, {comparison} {number}")]
    public static partial IGenericMessage Fired(ILogger logger, string threshold, string owner, int count, string comparison, int number);

    /// <summary>Logs a threshold that stopped firing.</summary>
    [MessageLogging(EventId = 22141, Level = LogLevel.Information, Message = "The threshold '{key}' no longer fires")]
    public static partial IGenericMessage Cleared(ILogger logger, string key);

    /// <summary>Logs a server that states that it takes the thresholds of a level above it.</summary>
    [MessageLogging(EventId = 22142, Level = LogLevel.Error,
        Message = "The server states its thresholds on waiting runs as '{mode}', but the server has no level above it: it states its own thresholds or states none")]
    public static partial IGenericMessage ServerInherits(ILogger logger, string mode);

    /// <summary>Logs a level that states thresholds of its own and states none.</summary>
    [MessageLogging(EventId = 22143, Level = LogLevel.Error,
        Message = "{owner} states thresholds of its own on waiting runs but states none")]
    public static partial IGenericMessage OwnWithoutThresholds(ILogger logger, string owner);

    /// <summary>Logs a level that states that it has none and lists thresholds.</summary>
    [MessageLogging(EventId = 22144, Level = LogLevel.Error,
        Message = "{owner} states that it has no thresholds on waiting runs but lists {count}")]
    public static partial IGenericMessage NoneWithThresholds(ILogger logger, string owner, int count);

    /// <summary>Logs a threshold whose notification rule could not be sent.</summary>
    [MessageLogging(EventId = 22146, Level = LogLevel.Error,
        Message = "The threshold '{threshold}' fired, but its notification rule '{rule}' was not sent: {reason}")]
    public static partial IGenericMessage NotificationNotSent(ILogger logger, string threshold, string rule, string? reason);

    /// <summary>Logs thresholds that could not be read while the waiting runs were judged.</summary>
    [MessageLogging(EventId = 22147, Level = LogLevel.Error,
        Message = "The thresholds on waiting runs could not be read, so the waiting runs were not judged: {reason}")]
    public static partial IGenericMessage ConfigurationNotRead(ILogger logger, string? reason);

    /// <summary>Logs a run whose pipeline states nothing about thresholds.</summary>
    [MessageLogging(EventId = 22148, Level = LogLevel.Error,
        Message = "The pipeline '{pipeline}' has no level of thresholds on waiting runs, so whether a run of it is stopped cannot be decided")]
    public static partial IGenericMessage PipelineHasNoLevel(ILogger logger, string pipeline);

    /// <summary>Logs a run whose schedule states nothing about thresholds.</summary>
    [MessageLogging(EventId = 22149, Level = LogLevel.Error,
        Message = "The schedule '{schedule}' has no level of thresholds on waiting runs, so whether a run it starts is stopped cannot be decided")]
    public static partial IGenericMessage ScheduleHasNoLevel(ILogger logger, string schedule);

    /// <summary>Logs a run that was not taken because a threshold stops new runs.</summary>
    [MessageLogging(EventId = 22150, Level = LogLevel.Warning,
        Message = "A run of '{pipeline}' is not taken: the threshold '{threshold}' of {owner} on waiting runs stops new runs")]
    public static partial IGenericMessage RunStopped(ILogger logger, string pipeline, string threshold, string owner);

    /// <summary>Logs a run that is closed as not executed because a threshold on waiting runs stops new runs, naming the threshold in the reason.</summary>
    [MessageLogging(EventId = 22151, Level = LogLevel.Warning, Message = "A run of '{pipeline}' is not taken: {reason}")]
    public static partial IGenericMessage RunNotTaken(ILogger logger, string pipeline, string reason);

    /// <summary>Logs a server that states no level of thresholds, or a mode it cannot take.</summary>
    [MessageLogging(EventId = 22152, Level = LogLevel.Error,
        Message = "The server states no level of thresholds on waiting runs, or a mode other than Own and None: its row in pipe.WaitThresholdLevel is missing or states '{mode}'")]
    public static partial IGenericMessage ServerLevelMissing(ILogger logger, string? mode);

    /// <summary>Logs a level that states a mode that is not an option of WaitThresholdModes.</summary>
    [MessageLogging(EventId = 22153, Level = LogLevel.Error, Message = "{owner} states the mode '{mode}' of thresholds on waiting runs, which is not one of Inherit, Own and None")]
    public static partial IGenericMessage ModeUnknown(ILogger logger, string owner, string mode);

    /// <summary>Logs a threshold that states a comparison or a severity that is not an option.</summary>
    [MessageLogging(EventId = 22154, Level = LogLevel.Error, Message = "The threshold '{threshold}' of {owner} states the {part} '{value}', which is not an option")]
    public static partial IGenericMessage OptionUnknown(ILogger logger, string threshold, string owner, string part, string value);

    /// <summary>Logs rows of the thresholds that could not be read.</summary>
    [MessageLogging(EventId = 62155, Level = LogLevel.Error, Message = "The {container} of thresholds on waiting runs could not be read: {reason}")]
    public static partial IGenericMessage RowsNotRead(ILogger logger, string container, string? reason);

    /// <summary>Logs a threshold whose owner is not a pipeline or a schedule that exists.</summary>
    [MessageLogging(EventId = 22156, Level = LogLevel.Error, Message = "The {what} is owned by {kind} '{owner}', which does not exist")]
    public static partial IGenericMessage OwnerUnknown(ILogger logger, string what, string kind, string owner);

    /// <summary>Logs a level that is refused when it is saved, naming the member and why.</summary>
    [MessageLogging(EventId = 22157, Level = LogLevel.Warning, Message = "The level of thresholds on waiting runs is refused at {member}: {reason}",
        ClientMessage = "The level of thresholds on waiting runs is refused at {member}: {reason}")]
    public static partial IGenericMessage Refused(ILogger logger, string member, string reason);

    /// <summary>Logs a level that could not be written.</summary>
    [MessageLogging(EventId = 92158, Level = LogLevel.Error, Message = "The level of thresholds on waiting runs of {owner} could not be written: {reason}")]
    public static partial IGenericMessage NotWritten(ILogger logger, string owner, string? reason);

    /// <summary>Logs an owner a level is read or written for that does not exist.</summary>
    [MessageLogging(EventId = 32159, Level = LogLevel.Warning, Message = "There is no {kind} '{owner}' to state thresholds on waiting runs",
        ClientMessage = "There is no {kind} '{owner}' to state thresholds on waiting runs")]
    public static partial IGenericMessage NoSuchOwner(ILogger logger, string kind, string owner);

    /// <summary>Logs a pipeline that cannot be deleted because a threshold of another owner counts it.</summary>
    [MessageLogging(EventId = 42160, Level = LogLevel.Warning,
        Message = "The pipeline '{pipeline}' is counted by the threshold '{threshold}' of {owner}, so it is not deleted",
        ClientMessage = "The pipeline '{pipeline}' is counted by the threshold '{threshold}' of {owner}; change or remove that threshold first")]
    public static partial IGenericMessage PipelineStillCounted(ILogger logger, string pipeline, string threshold, string owner);

    /// <summary>Logs the level of an owner that is being deleted and could not be removed.</summary>
    [MessageLogging(EventId = 92160, Level = LogLevel.Error, Message = "The level of thresholds on waiting runs of {owner} could not be removed: {reason}")]
    public static partial IGenericMessage NotReleased(ILogger logger, string owner, string? reason);

    /// <summary>Logs a level that inherits and lists thresholds.</summary>
    [MessageLogging(EventId = 22145, Level = LogLevel.Error,
        Message = "{owner} takes the thresholds of the level above it but lists {count} of its own")]
    public static partial IGenericMessage InheritWithThresholds(ILogger logger, string owner, int count);
}
