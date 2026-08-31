namespace Fdw.Services.Scheduling.Abstractions;

/// <summary>
/// Command to resume a paused schedule. Translated by each implementation's own translator into
/// that implementation's native call.
/// </summary>
public sealed class ResumeScheduleCommand : SchedulingCommandBase
{
    /// <summary>Initializes a new instance of the <see cref="ResumeScheduleCommand"/> class.</summary>
    /// <param name="scheduleId">The identifier of the schedule to resume.</param>
    public ResumeScheduleCommand(string scheduleId)
        : base("Resume")
    {
        ScheduleId = scheduleId;
    }

    /// <summary>Gets the identifier of the schedule to resume.</summary>
    public string ScheduleId { get; }
}
