namespace Fdw.Services.Scheduling.Abstractions;

/// <summary>
/// Command to create a new schedule. Translated by each implementation's own translator into that
/// implementation's native call (e.g. Quartz's <c>ScheduleJob</c>, Hangfire's <c>AddOrUpdate</c>).
/// </summary>
public sealed class CreateScheduleCommand : SchedulingCommandBase
{
    /// <summary>Initializes a new instance of the <see cref="CreateScheduleCommand"/> class.</summary>
    /// <param name="schedule">The schedule to create.</param>
    /// <param name="dataStoreName">The connection the owning scheduler reads and writes.</param>
    /// <param name="pathName">The schema the owning scheduler reads and writes.</param>
    /// <param name="scheduleContainerName">The container the owning scheduler's schedules live in.</param>
    public CreateScheduleCommand(IGenericSchedule schedule, string dataStoreName, string pathName, string scheduleContainerName)
        : base("Create", dataStoreName, pathName, scheduleContainerName)
    {
        Schedule = schedule;
    }

    /// <summary>Gets the schedule to create.</summary>
    public IGenericSchedule Schedule { get; }
}
