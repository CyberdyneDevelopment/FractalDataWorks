using System;
using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Scheduling.Logging;

/// <summary>
/// MessageLogging for the failed fires of a schedule and the pause they bring (FDW-6144): the host that cannot judge them, the refusals of the reporter and the pauser, and the lines that say a count was
/// reported and a schedule was paused. EventId range: 10101, 10102, 20103, 20104, 30105, 60106, 70108, 70109 and 70110.
/// </summary>
[MessageLoggingTypeCode("SCHEDULEFAILS")]
public static partial class ScheduleFailureLog
{
    /// <summary>Logs a fire's count reported to the thresholds that judge the schedule.</summary>
    [MessageLogging(EventId = 10101, Level = LogLevel.Information,
        Message = "Schedule '{scheduleName}' ({scheduleId}) has had {consecutiveFailures} failed fire(s) in a row; {thresholds} threshold(s) judged it and {fired} rule(s) fired")]
    public static partial IGenericMessage Reported(ILogger logger, Guid scheduleId, string scheduleName, long consecutiveFailures, int thresholds, int fired);

    /// <summary>Logs a schedule that a threshold paused.</summary>
    [MessageLogging(EventId = 10102, Level = LogLevel.Warning,
        Message = "Schedule {scheduleId} is paused: {reason}")]
    public static partial IGenericMessage SchedulePaused(ILogger logger, Guid scheduleId, string reason);

    /// <summary>Logs a count that cannot be reported because the schedule has no tenant.</summary>
    [MessageLogging(EventId = 20103, Level = LogLevel.Error,
        Message = "Schedule '{scheduleName}' ({scheduleId}) states no tenant, so its failed fires cannot be judged by thresholds, which belong to a tenant",
        ClientMessage = "The schedule's failed fires could not be judged")]
    public static partial IGenericMessage TenantMissing(ILogger logger, Guid scheduleId, string scheduleName);

    /// <summary>Logs a pause asked for a firing whose subject is not a schedule.</summary>
    [MessageLogging(EventId = 20104, Level = LogLevel.Error,
        Message = "The rule of threshold '{threshold}' fired on {kind} '{name}', which is not a schedule, so nothing was paused",
        ClientMessage = "Only a schedule can be paused")]
    public static partial IGenericMessage SubjectNotSchedule(ILogger logger, string threshold, string kind, string name);

    /// <summary>Logs a pause of a schedule that is not there.</summary>
    [MessageLogging(EventId = 30105, Level = LogLevel.Error,
        Message = "Schedule {scheduleId} has no current row, so it was not paused: {reason}",
        ClientMessage = "That schedule does not exist")]
    public static partial IGenericMessage NotFoundToPause(ILogger logger, Guid scheduleId, string reason);

    /// <summary>Logs a host that records fires but registered no threshold evaluator.</summary>
    [MessageLogging(EventId = 60106, Level = LogLevel.Critical,
        Message = "The host schedules runs but registered no IThresholdEvaluator, so the failed fires of its schedules could not be judged; the host does not start")]
    public static partial IGenericMessage EvaluatorNotRegistered(ILogger logger);

    /// <summary>Logs the thresholds of a schedule that could not be read.</summary>
    [MessageLogging(EventId = 70108, Level = LogLevel.Error,
        Message = "The thresholds that judge the failed fires of schedule {scheduleId} could not be read: {reason}")]
    public static partial IGenericMessage ThresholdsNotRead(ILogger logger, Guid scheduleId, string? reason);

    /// <summary>Logs thresholds that could not be judged.</summary>
    [MessageLogging(EventId = 70109, Level = LogLevel.Error,
        Message = "The thresholds that judge the failed fires of schedule {scheduleId} could not be judged: {reason}")]
    public static partial IGenericMessage NotJudged(ILogger logger, Guid scheduleId, string? reason);

    /// <summary>Logs a pause that could not be written.</summary>
    [MessageLogging(EventId = 70110, Level = LogLevel.Error,
        Message = "Schedule {scheduleId} could not be paused: {reason}")]
    public static partial IGenericMessage PauseNotWritten(ILogger logger, Guid scheduleId, string? reason);
}
