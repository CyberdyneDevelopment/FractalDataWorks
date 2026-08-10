using System;

namespace Fdw.Services.Scheduling.Endpoints;

/// <summary>
/// Database record representing a schedule configuration row.
/// </summary>
public class ScheduleConfigRecord
{
    /// <summary>Gets or sets the schedule ID.</summary>
    public Guid Id { get; set; }

    /// <summary>Gets or sets the schedule name.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets or sets the pipeline name.</summary>
    public string? PipelineName { get; set; }

    /// <summary>Gets or sets the scheduler type.</summary>
    public string? SchedulerType { get; set; }

    /// <summary>Gets or sets the cron expression.</summary>
    public string? CronExpression { get; set; }

    /// <summary>Gets or sets the interval in seconds.</summary>
    public int? IntervalSeconds { get; set; }

    /// <summary>Gets or sets whether the schedule is enabled.</summary>
    public bool IsEnabled { get; set; }

    /// <summary>Gets or sets the last run time.</summary>
    public DateTimeOffset? LastRunTime { get; set; }

    /// <summary>Gets or sets the next scheduled run time.</summary>
    public DateTimeOffset? NextRunTime { get; set; }

    /// <summary>Gets or sets the tenant ID.</summary>
    public Guid? TenantId { get; set; }

    /// <summary>Gets or sets the creation date.</summary>
    public DateTimeOffset CreateDate { get; set; }

    /// <summary>Gets or sets the last modification date.</summary>
    public DateTimeOffset? ModifyDate { get; set; }
}
