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
    public string? PipelineName { get; set; }

    /// <summary>Gets or sets the scheduler type.</summary>
    public string? SchedulerType { get; set; }

    /// <summary>Gets or sets the cron expression.</summary>
    public string? CronExpression { get; set; }

    /// <summary>Gets or sets the interval in seconds.</summary>
    public int? IntervalSeconds { get; set; }

    /// <summary>Gets or sets whether the schedule is enabled.</summary>
    public bool? IsEnabled { get; set; }
}
