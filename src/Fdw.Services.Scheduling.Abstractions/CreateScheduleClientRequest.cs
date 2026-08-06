using System;

namespace Fdw.Services.Scheduling.Clients.Abstractions;

/// <summary>
/// Request to create a new schedule.
/// </summary>
public class CreateScheduleClientRequest
{
    /// <summary>
    /// Gets or sets the schedule name.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the pipeline name to schedule.
    /// </summary>
    public string PipelineName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the scheduler type (e.g., Cron, Interval, OneTime, Event).
    /// </summary>
    public string SchedulerType { get; set; } = "Cron";

    /// <summary>
    /// Gets or sets the cron expression for cron-based schedules.
    /// </summary>
    public string? CronExpression { get; set; }

    /// <summary>
    /// Gets or sets the interval in seconds for interval-based schedules.
    /// </summary>
    public int? IntervalSeconds { get; set; }

    /// <summary>
    /// Gets or sets the one-time execution date and time for one-time schedules.
    /// </summary>
    public DateTimeOffset? OneTimeDateTime { get; set; }

    /// <summary>
    /// Gets or sets the event name that triggers execution for event-driven schedules.
    /// </summary>
    public string? EventName { get; set; }

    /// <summary>
    /// Gets or sets the time zone identifier.
    /// </summary>
    public string TimeZoneId { get; set; } = "UTC";

    /// <summary>
    /// Gets or sets whether the schedule is enabled.
    /// </summary>
    public bool IsEnabled { get; set; } = true;
}
