using System;
using System.Diagnostics.CodeAnalysis;
using Fdw.Data;

namespace Fdw.Services.Scheduling.Data;

/// <summary>
/// POCO record type for inserting schedules into the database.
/// Used with DataGateway for schedule creation operations.
/// </summary>
[ExcludeFromCodeCoverage]
[GenerateMapper]
public sealed class ScheduleInsertRecord
{
    /// <summary>Gets or sets the logical identifier (uuid v7). Required — sched.Schedule.Id
    /// is uniqueidentifier NOT NULL with no DB-side default, so the caller must mint it.</summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the schedule name.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the pipeline name to execute.
    /// </summary>
    public string PipelineName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the service option type (e.g., "Cron", "Interval").
    /// </summary>
    public string ServiceOptionType { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the cron expression for cron-based schedules.
    /// </summary>
    public string? CronExpression { get; set; }

    /// <summary>
    /// Gets or sets the interval in seconds for interval-based schedules.
    /// </summary>
    public int? IntervalSeconds { get; set; }

    /// <summary>
    /// Gets or sets the IANA timezone ID for schedule evaluation.
    /// Defaults to UTC.
    /// </summary>
    public string TimeZoneId { get; set; } = "UTC";

    /// <summary>
    /// Gets or sets whether the schedule is enabled.
    /// </summary>
    public bool IsEnabled { get; set; }

    /// <summary>
    /// Gets or sets the tenant ID for multi-tenant isolation.
    /// Null indicates a system-wide schedule.
    /// </summary>
    public Guid? TenantId { get; set; }
}
