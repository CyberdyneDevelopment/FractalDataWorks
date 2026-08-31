namespace Fdw.Services.Scheduling.Abstractions;

/// <summary>
/// Command to trigger immediate execution of a schedule, bypassing its normal timing. Translated
/// by each implementation's own translator into that implementation's native call (e.g. Quartz's
/// <c>TriggerJob</c>, Hangfire's <c>Trigger</c>).
/// </summary>
public sealed class TriggerScheduleCommand : SchedulingCommandBase
{
    /// <summary>Initializes a new instance of the <see cref="TriggerScheduleCommand"/> class.</summary>
    /// <param name="scheduleId">The identifier of the schedule to trigger.</param>
    public TriggerScheduleCommand(string scheduleId)
        : base("Trigger")
    {
        ScheduleId = scheduleId;
    }

    /// <summary>Gets the identifier of the schedule to trigger.</summary>
    public string ScheduleId { get; }
}
