namespace Fdw.Services.Scheduling.Clients.Abstractions;

/// <summary>
/// Request to update an existing schedule.
/// </summary>
public class UpdateScheduleClientRequest
{
    /// <summary>
    /// Gets or sets the pipeline name.
    /// </summary>
    public string PipelineName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the scheduler type (e.g., Cron, Interval).
    /// </summary>
    public string? SchedulerType { get; set; }

    /// <summary>
    /// Gets or sets the cron expression.
    /// </summary>
    public string? CronExpression { get; set; }

    /// <summary>
    /// Gets or sets the interval in seconds.
    /// </summary>
    public int? IntervalSeconds { get; set; }

    /// <summary>
    /// Gets or sets the time zone identifier.
    /// </summary>
    public string? TimeZoneId { get; set; }

    /// <summary>
    /// Gets or sets whether the schedule is enabled.
    /// </summary>
    public bool? IsEnabled { get; set; }
}
