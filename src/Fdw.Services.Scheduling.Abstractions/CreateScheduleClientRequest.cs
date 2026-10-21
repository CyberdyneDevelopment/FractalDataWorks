using Fdw.Services.Pipelines.Clients.Abstractions;
using System.Collections.Generic;
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
    /// Gets or sets the logical id of the pipeline to schedule (FDW-1088), resolved in the caller's view.
    /// </summary>
    public Guid PipelineId { get; set; }

    /// <summary>
    /// Gets or sets the pipeline's name as it was resolved, stored by the scheduler for display only.
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
    /// Gets or sets the time zone identifier the schedule is evaluated in. Required: it is not defaulted, and the scheduler refuses a create that leaves it out.
    /// </summary>
    public string TimeZoneId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets whether the schedule is enabled.
    /// </summary>
    public bool IsEnabled { get; set; } = true;

    /// <summary>
    /// Gets or sets the run parameter values the scheduler passes to the pipeline on each run (contract v2 item 9),
    /// validated against the pipeline's declared parameters exactly like a trigger.
    /// </summary>
    public IList<PipelineParameterValue>? Parameters { get; set; }

    /// <summary>
    /// Gets or sets the roles the schedule's runs act with (FDW-1161), by name. Required: at least one, only roles the caller
    /// holds in the tenant it acts in. Every fire runs with exactly these roles and is judged, when it fires, for what the pipeline
    /// then reads and writes; a pipeline carries no roles of its own.
    /// </summary>
    public IList<string>? RunRoles { get; set; }
}
