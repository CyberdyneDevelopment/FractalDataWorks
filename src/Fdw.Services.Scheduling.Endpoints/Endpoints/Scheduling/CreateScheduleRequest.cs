using System;
using System.ComponentModel.DataAnnotations;
using Fdw.Web.Endpoints.Contracts;

namespace Fdw.Services.Scheduling.Endpoints;

/// <summary>
/// Request DTO for creating a new schedule configuration.
/// </summary>
public class CreateScheduleRequest : ResourceCreateRequest
{
    /// <summary>Gets or sets the pipeline name associated with this schedule.</summary>
    [Required]
    public string PipelineName { get; set; } = string.Empty;

    /// <summary>Gets or sets the scheduler type (e.g., Cron, Interval, Manual).</summary>
    [Required]
    public string SchedulerType { get; set; } = string.Empty;

    /// <summary>Gets or sets the cron expression for cron-based schedules.</summary>
    public string? CronExpression { get; set; }

    /// <summary>Gets or sets the interval in seconds for interval-based schedules.</summary>
    public int? IntervalSeconds { get; set; }

    /// <summary>Gets or sets the one-time execution date and time for Once-type schedules.</summary>
    public DateTimeOffset? OneTimeDateTime { get; set; }

    /// <summary>Gets or sets the triggering event name for Event-type schedules.</summary>
    public string? EventName { get; set; }

    /// <summary>Gets or sets the IANA timezone ID for schedule evaluation (e.g., "America/New_York").</summary>
    public string TimeZoneId { get; set; } = "UTC";

    /// <summary>Gets or sets whether the schedule is enabled.</summary>
    public bool IsEnabled { get; set; }
}
