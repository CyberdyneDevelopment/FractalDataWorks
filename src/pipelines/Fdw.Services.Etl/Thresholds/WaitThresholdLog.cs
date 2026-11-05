using System;
using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Etl.Thresholds;

/// <summary>
/// MessageLogging for the thresholds on waiting pipeline runs (FDW-3161, FDW-3975): the level of thresholds an owner states, the gate that holds new runs, and the waiting runs
/// reported to the threshold evaluator.
/// EventId range: 22140-22166 (the leading digit of an id that surfaces as an answer names its category: 2 a refusal, 3 a missing owner, 6 a configuration fault, 9 an internal one, 4 a conflict).
/// </summary>
[MessageLoggingTypeCode("WAITTHRESHOLD")]
public static partial class WaitThresholdLog
{
    /// <summary>Logs a server that states that it takes the thresholds of a level above it.</summary>
    [MessageLogging(EventId = 22142, Level = LogLevel.Error,
        Message = "The server states its thresholds on waiting runs as '{mode}', but the server has no level above it: it states its own thresholds or states none")]
    public static partial IGenericMessage ServerInherits(ILogger logger, string mode);

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

    /// <summary>Logs the level of an owner that is being deleted and could not be removed.</summary>
    [MessageLogging(EventId = 92160, Level = LogLevel.Error, Message = "The level of thresholds on waiting runs of {owner} could not be removed: {reason}")]
    public static partial IGenericMessage NotReleased(ILogger logger, string owner, string? reason);

    /// <summary>Logs a waiting run that names no tenant, so the thresholds of its pipeline cannot be judged for it (FDW-3975).</summary>
    [MessageLogging(EventId = 22161, Level = LogLevel.Warning,
        Message = "A waiting run of '{pipeline}' names no tenant, so it is not counted against the thresholds on waiting runs of the pipeline")]
    public static partial IGenericMessage WaitingRunHasNoTenant(ILogger logger, string pipeline);

    /// <summary>Logs a count of waiting runs that the evaluator could not judge (FDW-3975).</summary>
    [MessageLogging(EventId = 22162, Level = LogLevel.Warning,
        Message = "The waiting runs of '{pipeline}' were not judged against its thresholds: {reason}")]
    public static partial IGenericMessage WaitingRunsNotJudged(ILogger logger, string pipeline, string? reason);

    /// <summary>Logs a report of waiting runs that threw (FDW-3975).</summary>
    [MessageLogging(EventId = 92161, Level = LogLevel.Error, Message = "Reporting the waiting runs of '{pipeline}' to the threshold evaluator failed")]
    public static partial IGenericMessage WaitingRunsReportFailed(ILogger logger, Exception ex, string pipeline);

    /// <summary>Logs a run that names no tenant, so the thresholds on waiting runs cannot say whether it is held (FDW-3975).</summary>
    [MessageLogging(EventId = 22165, Level = LogLevel.Error,
        Message = "A run of '{pipeline}' names no tenant, so the thresholds on waiting runs cannot say whether it is held; it is not taken")]
    public static partial IGenericMessage HoldRunHasNoTenant(ILogger logger, string pipeline);

    /// <summary>Logs a run for which the thresholds of its owner were not read, so it is not taken (FDW-3975).</summary>
    [MessageLogging(EventId = 22167, Level = LogLevel.Error,
        Message = "The thresholds on waiting runs of the {kind} a run of '{pipeline}' is held by were not read, so it is not taken")]
    public static partial IGenericMessage HoldThresholdsNotRead(ILogger logger, string pipeline, string kind);

    /// <summary>Logs a waiting run that names a schedule but not its Id, so it is not counted against the thresholds of the schedule (FDW-3975).</summary>
    [MessageLogging(EventId = 32163, Level = LogLevel.Warning,
        Message = "A waiting run of '{pipeline}' names the schedule '{schedule}' but not its Id, so it is not counted against the thresholds of that schedule")]
    public static partial IGenericMessage WaitingRunHasNoScheduleId(ILogger logger, string pipeline, string schedule);

    /// <summary>Logs a run started by a schedule that carries no schedule Id, so the thresholds of that schedule cannot say whether it is held (FDW-3975).</summary>
    [MessageLogging(EventId = 32166, Level = LogLevel.Error,
        Message = "A run of '{pipeline}' names the schedule '{schedule}' but not its Id, so the thresholds of that schedule cannot say whether it is held; it is not taken")]
    public static partial IGenericMessage HoldScheduleIdMissing(ILogger logger, string pipeline, string schedule);
}
