using Fdw.Services.Pipelines.Clients.Abstractions;
using System.Collections.Generic;
using System;
using System.ComponentModel.DataAnnotations;
using Fdw.Web.Endpoints.Contracts;

namespace Fdw.Services.Scheduling.Endpoints;

/// <summary>
/// Request DTO for creating a new schedule configuration.
/// </summary>
public class CreateScheduleRequest : ResourceCreateRequest
{
    /// <summary>
    /// Gets or sets the logical id of the pipeline the schedule runs (FDW-1088). The API proxy resolves it in its
    /// caller's view before relaying it; the scheduler does not look it up again.
    /// </summary>
    [Required]
    public Guid PipelineId { get; set; }

    /// <summary>
    /// Gets or sets the pipeline's name as the proxy resolved it, stored for display only.
    /// </summary>
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

    /// <summary>
    /// Gets or sets the IANA timezone ID the schedule is evaluated in (e.g., "America/New_York"). Required: a create that does not state it, or states
    /// one the server does not know, is refused. A schedule that wants UTC says "UTC".
    /// </summary>
    public string TimeZoneId { get; set; } = string.Empty;

    /// <summary>Gets or sets whether the schedule is enabled.</summary>
    public bool IsEnabled { get; set; }

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
