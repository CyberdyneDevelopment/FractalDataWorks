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

    /// <summary>Gets or sets the pipeline name.</summary>
    // Why: nullable so partial PUTs (e.g. {isEnabled:false}) don't fail [Required]/NotEmpty
    // validation. Endpoint merges only non-null fields into the existing schedule.
    public string? PipelineName { get; set; }

    /// <summary>Gets or sets the scheduler type.</summary>
    public string? SchedulerType { get; set; }

    /// <summary>Gets or sets the cron expression.</summary>
    public string? CronExpression { get; set; }

    /// <summary>Gets or sets the interval in seconds.</summary>
    public int? IntervalSeconds { get; set; }

    /// <summary>Gets or sets whether the schedule is enabled.</summary>
    // Why: nullable so the validator can distinguish "no IsEnabled supplied" from
    // "IsEnabled=false explicitly" — without this, every empty/rename-only body would
    // silently apply IsEnabled=false as a default.
    public bool? IsEnabled { get; set; }
}
