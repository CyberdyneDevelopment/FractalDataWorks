using Fdw.Services.Pipelines.Clients.Abstractions;
using System.Collections.Generic;
using System;
using System.ComponentModel.DataAnnotations;

namespace Fdw.Services.Scheduling.Endpoints;

/// <summary>
/// Request DTO for updating an existing schedule configuration.
/// </summary>
public class UpdateScheduleRequest
{
    /// <summary>Gets or sets the schedule name (bound from route).</summary>
    [Required]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the logical id of the pipeline the schedule runs (FDW-1088), resolved by the API proxy in its
    /// caller's view. Omitted leaves it unchanged; given, <see cref="PipelineName"/> comes with it.
    /// </summary>
    public Guid? PipelineId { get; set; }

    /// <summary>Gets or sets the pipeline's name as the proxy resolved it, for display; sent only with <see cref="PipelineId"/>.</summary>
    public string? PipelineName { get; set; }

    /// <summary>Gets or sets the scheduler type.</summary>
    public string? SchedulerType { get; set; }

    /// <summary>Gets or sets the cron expression.</summary>
    public string? CronExpression { get; set; }

    /// <summary>Gets or sets the interval in seconds.</summary>
    public int? IntervalSeconds { get; set; }

    /// <summary>
    /// Gets or sets the IANA time zone the schedule's times are evaluated in (FDW-1170), e.g. "America/Chicago". Omitted leaves it
    /// unchanged; a zone the server cannot resolve is refused naming the value.
    /// </summary>
    public string? TimeZoneId { get; set; }

    /// <summary>Gets or sets whether the schedule is enabled.</summary>
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
