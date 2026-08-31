using System;
using Fdw.Web.Endpoints.Contracts;

namespace Fdw.Services.Scheduling.Endpoints;

/// <summary>
/// Detailed DTO for a schedule, including timing and audit information.
/// </summary>
public class ScheduleDetailDto : ResourceDetail
{
    /// <summary>Gets or sets the pipeline name associated with this schedule.</summary>
    public required string PipelineName { get; set; }

    /// <summary>Gets or sets the scheduler type (e.g., Cron, Interval, Manual).</summary>
    public required string SchedulerType { get; set; }

    /// <summary>Gets or sets the cron expression for cron-based schedules.</summary>
    public string? CronExpression { get; set; }

    /// <summary>Gets or sets the interval in seconds for interval-based schedules.</summary>
    public int? IntervalSeconds { get; set; }

    /// <summary>Gets or sets the time zone the schedule's times are expressed in.</summary>
    public string? TimeZoneId { get; set; }

    /// <summary>Gets or sets whether the schedule is enabled.</summary>
    public bool IsEnabled { get; set; }

    /// <summary>Gets or sets the last run time.</summary>
    public DateTimeOffset? LastRunTime { get; set; }

    /// <summary>Gets or sets the next scheduled run time.</summary>
    public DateTimeOffset? NextRunTime { get; set; }

    /// <summary>Gets or sets the creation timestamp.</summary>
    public DateTimeOffset CreatedAt { get; set; }

    /// <summary>Gets or sets the last update timestamp.</summary>
    public DateTimeOffset? UpdatedAt { get; set; }

    /// <summary>Gets or sets the user who created the record.</summary>
    public string CreatedBy { get; set; } = string.Empty;

    /// <summary>Gets or sets the user who last modified the record.</summary>
    public string ModifiedBy { get; set; } = string.Empty;

    /// <summary>Gets or sets the application user on whose behalf the record was created.</summary>
    public string CreatedOnBehalfOf { get; set; } = string.Empty;

    /// <summary>Gets or sets the application user on whose behalf the record was last modified.</summary>
    public string ModifiedOnBehalfOf { get; set; } = string.Empty;
}
