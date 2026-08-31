namespace Fdw.Services.Scheduling.Abstractions;

/// <summary>
/// Command to update an existing schedule. Translated by each implementation's own translator into
/// that implementation's native call.
/// </summary>
public sealed class UpdateScheduleCommand : SchedulingCommandBase
{
    /// <summary>Initializes a new instance of the <see cref="UpdateScheduleCommand"/> class.</summary>
    /// <param name="schedule">The schedule to update, identified by its <see cref="IGenericSchedule.ScheduleId"/>.</param>
    public UpdateScheduleCommand(IGenericSchedule schedule)
        : base("Update")
    {
        Schedule = schedule;
    }

    /// <summary>Gets the schedule to update.</summary>
    public IGenericSchedule Schedule { get; }
}
