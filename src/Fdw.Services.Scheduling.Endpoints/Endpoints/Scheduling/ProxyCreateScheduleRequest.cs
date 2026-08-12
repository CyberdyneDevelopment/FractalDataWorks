using System;

namespace Fdw.Services.Scheduling.Endpoints;

/// <summary>
/// Request for creating a schedule via proxy.
/// </summary>
public sealed class ProxyCreateScheduleRequest
{
    /// <summary>Gets or sets the schedule name.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets or sets the pipeline name.</summary>
    public string PipelineName { get; set; } = string.Empty;

    /// <summary>Gets or sets the scheduler type (Cron, Interval, Manual).</summary>
    public string SchedulerType { get; set; } = "Cron";

    /// <summary>Gets or sets the cron expression.</summary>
    public string? CronExpression { get; set; }

    /// <summary>Gets or sets the interval in seconds.</summary>
    public int? IntervalSeconds { get; set; }

    /// <summary>Gets or sets the one-time execution date and time for Once-type schedules.</summary>
    public DateTimeOffset? OneTimeDateTime { get; set; }

    /// <summary>Gets or sets the triggering event name for Event-type schedules.</summary>
    public string? EventName { get; set; }

    /// <summary>Gets or sets the IANA timezone ID for schedule evaluation.</summary>
    public string TimeZoneId { get; set; } = "UTC";

    /// <summary>Gets or sets whether the schedule is enabled.</summary>
    public bool IsEnabled { get; set; } = true;
}
