using System;

namespace Fdw.Services.Scheduling.Abstractions;

/// <summary>
/// Command to trigger immediate execution of a schedule, bypassing its normal timing. Translated
/// by each implementation's own translator into that implementation's native call (e.g. Quartz's
/// <c>TriggerJob</c>).
/// </summary>
public sealed class TriggerScheduleCommand : SchedulingCommandBase
{
    /// <summary>Initializes a new instance of the <see cref="TriggerScheduleCommand"/> class.</summary>
    /// <param name="scheduleId">The identifier of the schedule to trigger.</param>
    /// <param name="dataStoreName">The connection the owning scheduler reads and writes.</param>
    /// <param name="pathName">The schema the owning scheduler reads and writes.</param>
    /// <param name="scheduleContainerName">The container the owning scheduler's schedules live in.</param>
    public TriggerScheduleCommand(string scheduleId, string dataStoreName, string pathName, string scheduleContainerName)
        : base("Trigger", dataStoreName, pathName, scheduleContainerName)
    {
        ScheduleId = scheduleId;
    }

    /// <summary>Initializes a new instance of the <see cref="TriggerScheduleCommand"/> class for the catch-up run of a fire time the scheduler missed (FDW-3788).</summary>
    /// <param name="scheduleId">The identifier of the schedule to trigger.</param>
    /// <param name="dataStoreName">The connection the owning scheduler reads and writes.</param>
    /// <param name="pathName">The schema the owning scheduler reads and writes.</param>
    /// <param name="scheduleContainerName">The container the owning scheduler's schedules live in.</param>
    /// <param name="catchupDueTime">The fire time the run is for; the run is recorded as a Catchup that was due then.</param>
    public TriggerScheduleCommand(string scheduleId, string dataStoreName, string pathName, string scheduleContainerName, DateTimeOffset catchupDueTime)
        : this(scheduleId, dataStoreName, pathName, scheduleContainerName)
    {
        CatchupDueTime = catchupDueTime;
    }

    /// <summary>Gets the fire time a catch-up run is for; none for an ordinary manual trigger.</summary>
    public DateTimeOffset? CatchupDueTime { get; }

    /// <summary>Gets the identifier of the schedule to trigger.</summary>
    public string ScheduleId { get; }
}
