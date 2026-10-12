using Fdw.Services.Pipelines.Clients.Abstractions;
using System.Collections.Generic;
using System;

namespace Fdw.Services.Scheduling.Clients.Abstractions;

/// <summary>
/// Request to update an existing schedule.
/// </summary>
public class UpdateScheduleClientRequest
{
    /// <summary>
    /// Gets or sets the logical id of the pipeline (FDW-1088), resolved in the caller's view. Omitted (null) leaves
    /// it unchanged; given, <see cref="PipelineName"/> comes with it.
    /// </summary>
    public Guid? PipelineId { get; set; }

    /// <summary>
    /// Gets or sets the pipeline's name as it was resolved, for display; sent only with <see cref="PipelineId"/>.
    /// </summary>
    public string? PipelineName { get; set; }

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

    /// <summary>
    /// Gets or sets the run parameter values the scheduler passes to the pipeline on each run (contract v2 item 9),
    /// validated against the pipeline's declared parameters exactly like a trigger.
    /// </summary>
    public IList<PipelineParameterValue>? Parameters { get; set; }

    /// <summary>
    /// Gets or sets the roles the schedule's runs act with (FDW-1161), by name. Null leaves the recorded roles standing; a list
    /// replaces them, must name at least one role, and only roles the caller holds in the tenant it acts in.
    /// </summary>
    public IList<string>? RunRoles { get; set; }
}
