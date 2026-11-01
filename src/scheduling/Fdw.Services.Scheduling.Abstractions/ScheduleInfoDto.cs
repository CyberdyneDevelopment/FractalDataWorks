using Fdw.Services.Pipelines.Clients.Abstractions;
using System.Collections.Generic;
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
    /// Gets or sets the pipeline's name as it was when the schedule was saved, for display.
    /// </summary>
    public string PipelineName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the logical id of the pipeline the schedule runs (FDW-1088).
    /// </summary>
    public Guid PipelineId { get; set; }

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
    /// Gets or sets the time zone identifier the schedule is evaluated in, as it was saved.
    /// </summary>
    public string TimeZoneId { get; set; } = string.Empty;

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

    /// <summary>
    /// Gets or sets the run parameter values the scheduler passes to the pipeline on each run (contract v2 item 9),
    /// validated against the pipeline's declared parameters exactly like a trigger.
    /// </summary>
    public IList<PipelineParameterValue>? Parameters { get; set; }

    /// <summary>Gets or sets the names of the roles the schedule's runs act with (FDW-1161).</summary>
    public IList<string>? RunRoles { get; set; }
}
