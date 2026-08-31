namespace Fdw.Services.Scheduling.Abstractions;

/// <summary>
/// Command to create a new schedule. Translated by each implementation's own translator into that
/// implementation's native call (e.g. Quartz's <c>ScheduleJob</c>, Hangfire's <c>AddOrUpdate</c>).
/// </summary>
public sealed class CreateScheduleCommand : SchedulingCommandBase
{
    /// <summary>Initializes a new instance of the <see cref="CreateScheduleCommand"/> class.</summary>
    /// <param name="schedule">The schedule to create.</param>
    public CreateScheduleCommand(IGenericSchedule schedule)
        : base("Create")
    {
        Schedule = schedule;
    }

    /// <summary>Gets the schedule to create.</summary>
    public IGenericSchedule Schedule { get; }
}
