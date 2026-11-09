using System;
using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Scheduling.Logging;

/// <summary>Messages of recording a fire on its schedule (FDW-4598).</summary>
[MessageLoggingTypeCode("SCHEDULERUNS")]
public static partial class ScheduleRunRecordLog
{
    /// <summary>Logs that a fire was recorded on the schedule's current row.</summary>
    [MessageLogging(EventId = 11060, Level = LogLevel.Debug, Message = "Recorded the fire of schedule {scheduleId}: status {status}, next run {nextRunTime}")]
    public static partial IGenericMessage Recorded(ILogger logger, Guid scheduleId, string status, DateTimeOffset? nextRunTime);

    /// <summary>Logs that no current row of the schedule took the update.</summary>
    [MessageLogging(EventId = 14310, Level = LogLevel.Error, Message = "The fire of schedule {scheduleId} was not recorded: {rows} current row(s) took the update, and exactly one is expected")]
    public static partial IGenericMessage NotRecorded(ILogger logger, Guid scheduleId, int rows);

    /// <summary>Logs that the configuration store could not be reached to record a fire.</summary>
    [MessageLogging(EventId = 14311, Level = LogLevel.Error, Message = "The fire of schedule {scheduleId} was not recorded: the configuration store is not available: {reason}")]
    public static partial IGenericMessage StoreUnavailable(ILogger logger, Guid scheduleId, string reason);
}
