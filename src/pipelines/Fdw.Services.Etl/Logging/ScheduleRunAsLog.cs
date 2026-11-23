using System;
using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Etl.Logging;

/// <summary>
/// MessageLogging for who a schedule's fire acts as (FDW-6144): the refusals of the check that reads the schedule's stored grant, each with its own number, and the line that says a fire acts as a person.
/// EventId range: 10841, 20843, 30842, 70845, 70846, 100847 and 100848.
/// </summary>
[MessageLoggingTypeCode("SCHEDULERUNAS")]
public static partial class ScheduleRunAsLog
{
    /// <summary>Logs a fire that acts as the person its schedule's grant names.</summary>
    [MessageLogging(EventId = 10841, Level = LogLevel.Information,
        Message = "The fire of schedule {scheduleId} for pipeline {pipeline} (run {executionId}) acts as person {personId} under grant {grantId}")]
    public static partial IGenericMessage ActsAsPerson(ILogger logger, Guid scheduleId, string pipeline, Guid executionId, Guid personId, Guid grantId);

    /// <summary>Logs a fire for a schedule that does not exist.</summary>
    [MessageLogging(EventId = 30842, Level = LogLevel.Error,
        Message = "The fire for pipeline {pipeline} (run {executionId}) names schedule {scheduleId}, which is not a schedule of the run's tenant, so it is refused",
        ClientMessage = "That schedule does not exist")]
    public static partial IGenericMessage ScheduleNotFound(ILogger logger, Guid scheduleId, string pipeline, Guid executionId);

    /// <summary>Logs a fire that is not for the pipeline and tenant its schedule has.</summary>
    [MessageLogging(EventId = 20843, Level = LogLevel.Error,
        Message = "The fire (run {executionId}) names schedule {scheduleId}, which runs pipeline {schedulePipeline} in tenant {scheduleTenant}, but the fire is for pipeline {pipeline} in tenant {tenant}, so it is refused",
        ClientMessage = "The fire does not match the schedule it names")]
    public static partial IGenericMessage ScheduleGrantMismatch(ILogger logger, Guid scheduleId, Guid executionId, Guid schedulePipeline, Guid scheduleTenant, string pipeline, Guid tenant);

    /// <summary>Logs a schedule that could not be read.</summary>
    [MessageLogging(EventId = 70845, Level = LogLevel.Error,
        Message = "Schedule {scheduleId} could not be read for the fire (run {executionId}): {reason}")]
    public static partial IGenericMessage ScheduleNotRead(ILogger logger, Guid scheduleId, Guid executionId, string? reason);

    /// <summary>Logs a grant that is not there or could not be read: the reason is the grant service's own message.</summary>
    [MessageLogging(EventId = 70846, Level = LogLevel.Error,
        Message = "Grant {grantId} of schedule {scheduleId} could not be read for the fire (run {executionId}): {reason}")]
    public static partial IGenericMessage ScheduleGrantNotRead(ILogger logger, Guid scheduleId, Guid grantId, Guid executionId, string? reason);

    /// <summary>Logs a fire refused because its schedule's grant has ended or was revoked.</summary>
    [MessageLogging(EventId = 100847, Level = LogLevel.Error,
        Message = "The fire (run {executionId}) of schedule {scheduleId} is refused: its grant {grantId} {ended}",
        ClientMessage = "The grant this schedule runs under has ended")]
    public static partial IGenericMessage ScheduleGrantExpired(ILogger logger, Guid scheduleId, Guid grantId, Guid executionId, string ended);

    /// <summary>Logs a fire refused because the grant allows less than the run's roles carry.</summary>
    [MessageLogging(EventId = 100848, Level = LogLevel.Error,
        Message = "The fire (run {executionId}) of schedule {scheduleId} is refused: its grant {grantId} does not allow {notAllowed}, which the run's roles carry",
        ClientMessage = "The grant this schedule runs under does not allow everything its run roles carry")]
    public static partial IGenericMessage ScheduleGrantNotCovering(ILogger logger, Guid scheduleId, Guid grantId, Guid executionId, string notAllowed);
}
