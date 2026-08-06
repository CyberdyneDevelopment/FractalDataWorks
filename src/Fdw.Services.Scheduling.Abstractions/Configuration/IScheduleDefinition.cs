using System;
using Fdw.Configuration;

namespace Fdw.Services.Scheduling.Abstractions.Configuration;

/// <summary>
/// Interface for schedule definition configurations that define WHEN a process should run.
/// This is distinct from <see cref="IGenericConfiguration"/> which configures the scheduler service itself.
/// </summary>
/// <remarks>
/// <para>
/// Schedule definitions are persisted to ConfigurationDb and loaded at startup
/// to configure the scheduler with jobs and their timing.
/// </para>
/// <para>
/// Each schedule definition references a pipeline by name and specifies the trigger
/// configuration (cron expression, interval, etc.).
/// </para>
/// </remarks>
public interface IScheduleDefinition : IGenericConfiguration
{
    /// <summary>
    /// Gets the discriminator that determines which schedule type implementation to use.
    /// </summary>
    /// <value>A string identifying the schedule type (e.g., "Cron", "Interval", "Once").</value>
    string ScheduleType { get; }

    /// <summary>
    /// Gets or sets the name of the pipeline to execute when this schedule triggers.
    /// </summary>
    /// <value>The pipeline name that matches a configured pipeline definition.</value>
    string PipelineName { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether this schedule is active.
    /// </summary>
    /// <value><c>true</c> if the schedule should be evaluated; <c>false</c> to skip.</value>
    bool IsEnabled { get; set; }

    /// <summary>
    /// Gets or sets the next scheduled run time in UTC.
    /// </summary>
    /// <value>The next execution time, or <c>null</c> if not calculated or schedule is disabled.</value>
    DateTimeOffset? NextRunTime { get; set; }

    /// <summary>
    /// Gets or sets the last run time in UTC.
    /// </summary>
    /// <value>The last execution time, or <c>null</c> if never run.</value>
    DateTimeOffset? LastRunTime { get; set; }

    /// <summary>
    /// Gets or sets the last run status.
    /// </summary>
    /// <value>Status of the last execution (e.g., "Success", "Failed", "Running").</value>
    string? LastRunStatus { get; set; }

    /// <summary>
    /// Gets the tenant identifier for tenant-scoped schedules.
    /// Null means system-wide (visible to all tenants).
    /// </summary>
    Guid? TenantId { get; }
}
