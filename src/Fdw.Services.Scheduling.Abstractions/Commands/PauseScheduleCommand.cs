namespace Fdw.Services.Scheduling.Abstractions;

/// <summary>
/// Command to pause a schedule. Translated by each implementation's own translator into that
/// implementation's native call.
/// </summary>
public sealed class PauseScheduleCommand : SchedulingCommandBase
{
    /// <summary>Initializes a new instance of the <see cref="PauseScheduleCommand"/> class.</summary>
    /// <param name="scheduleId">The identifier of the schedule to pause.</param>
    /// <param name="dataStoreName">The connection the owning scheduler reads and writes.</param>
    /// <param name="pathName">The schema the owning scheduler reads and writes.</param>
    /// <param name="scheduleContainerName">The container the owning scheduler's schedules live in.</param>
    public PauseScheduleCommand(string scheduleId, string dataStoreName, string pathName, string scheduleContainerName)
        : base("Pause", dataStoreName, pathName, scheduleContainerName)
    {
        ScheduleId = scheduleId;
    }

    /// <summary>Gets the identifier of the schedule to pause.</summary>
    public string ScheduleId { get; }
}
