namespace Fdw.Services.Scheduling.Abstractions;

/// <summary>
/// Command to delete a schedule. Translated by each implementation's own translator into that
/// implementation's native call.
/// </summary>
public sealed class DeleteScheduleCommand : SchedulingCommandBase
{
    /// <summary>Initializes a new instance of the <see cref="DeleteScheduleCommand"/> class.</summary>
    /// <param name="scheduleId">The identifier of the schedule to delete.</param>
    public DeleteScheduleCommand(string scheduleId)
        : base("Delete")
    {
        ScheduleId = scheduleId;
    }

    /// <summary>Gets the identifier of the schedule to delete.</summary>
    public string ScheduleId { get; }
}
