namespace Fdw.Services.Scheduling.Abstractions;

/// <summary>
/// Command to resume a paused schedule. Translated by each implementation's own translator into
/// that implementation's native call.
/// </summary>
public sealed class ResumeScheduleCommand : SchedulingCommandBase
{
    /// <summary>Initializes a new instance of the <see cref="ResumeScheduleCommand"/> class.</summary>
    /// <param name="scheduleId">The identifier of the schedule to resume.</param>
    /// <param name="dataStoreName">The connection the owning scheduler reads and writes.</param>
    /// <param name="pathName">The schema the owning scheduler reads and writes.</param>
    /// <param name="scheduleContainerName">The container the owning scheduler's schedules live in.</param>
    public ResumeScheduleCommand(string scheduleId, string dataStoreName, string pathName, string scheduleContainerName)
        : base("Resume", dataStoreName, pathName, scheduleContainerName)
    {
        ScheduleId = scheduleId;
    }

    /// <summary>Gets the identifier of the schedule to resume.</summary>
    public string ScheduleId { get; }
}
