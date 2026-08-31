using System;

namespace Fdw.Services.Scheduling.Endpoints;

/// <summary>
/// Summary DTO for a schedule, used in list views.
/// </summary>
public class ScheduleSummaryDto
{
    /// <summary>Gets or sets the schedule ID.</summary>
    public Guid Id { get; set; }

    /// <summary>Gets or sets the schedule name.</summary>
    public required string Name { get; set; }

    /// <summary>Gets or sets the pipeline name.</summary>
    public required string PipelineName { get; set; }

    /// <summary>Gets or sets the scheduler type.</summary>
    public required string SchedulerType { get; set; }

    /// <summary>Gets or sets whether the schedule is enabled.</summary>
    public bool IsEnabled { get; set; }

    /// <summary>Gets or sets the next scheduled run time.</summary>
    public DateTimeOffset? NextRunTime { get; set; }

    /// <summary>Gets or sets the last time this schedule ran.</summary>
    public DateTimeOffset? LastRunTime { get; set; }

    /// <summary>Gets or sets the cron expression for cron-based schedules.</summary>
    public string? CronExpression { get; set; }

    /// <summary>Gets or sets the interval in seconds for interval-based schedules.</summary>
    public int? IntervalSeconds { get; set; }

    /// <summary>Gets or sets the time zone the schedule's times are expressed in.</summary>
    public string? TimeZoneId { get; set; }

    /// <summary>Gets or sets the creation timestamp.</summary>
    public DateTimeOffset CreatedAt { get; set; }

    /// <summary>Gets or sets the user who created the record.</summary>
    public string CreatedBy { get; set; } = string.Empty;

    /// <summary>Gets or sets the user who last modified the record.</summary>
    public string ModifiedBy { get; set; } = string.Empty;

    /// <summary>Gets or sets the application user on whose behalf the record was created.</summary>
    public string CreatedOnBehalfOf { get; set; } = string.Empty;

    /// <summary>Gets or sets the application user on whose behalf the record was last modified.</summary>
    public string ModifiedOnBehalfOf { get; set; } = string.Empty;
}
