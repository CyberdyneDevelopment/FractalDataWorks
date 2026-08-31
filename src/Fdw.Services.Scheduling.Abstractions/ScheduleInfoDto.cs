using System;

namespace Fdw.Services.Scheduling.Clients.Abstractions;

/// <summary>
/// Represents schedule information returned from the scheduling service.
/// </summary>
public class ScheduleInfoDto
{
    /// <summary>
    /// Gets or sets the schedule name.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the pipeline name associated with this schedule.
    /// </summary>
    public string PipelineName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the scheduler type (e.g., Cron, Interval).
    /// </summary>
    public string SchedulerType { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the cron expression for cron-based schedules.
    /// </summary>
    public string? CronExpression { get; set; }

    /// <summary>
    /// Gets or sets the interval in seconds for interval-based schedules.
    /// </summary>
    public int? IntervalSeconds { get; set; }

    /// <summary>
    /// Gets or sets the time zone identifier.
    /// </summary>
    public string TimeZoneId { get; set; } = "UTC";

    /// <summary>
    /// Gets or sets a value indicating whether the schedule is enabled.
    /// </summary>
    public bool IsEnabled { get; set; }

    /// <summary>
    /// Gets or sets the last execution time.
    /// </summary>
    public DateTimeOffset? LastRunTime { get; set; }

    /// <summary>
    /// Gets or sets the next scheduled execution time.
    /// </summary>
    public DateTimeOffset? NextRunTime { get; set; }
}
