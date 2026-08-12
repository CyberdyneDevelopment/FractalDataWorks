using System;
using System.Diagnostics.CodeAnalysis;
using Fdw.Configuration;
using Fdw.Data;
using Fdw.Services.Scheduling.Abstractions.Configuration;
using Fdw.Services.Scheduling.Abstractions.OptionTypes;

namespace Fdw.Services.Scheduling.Abstractions.Configuration;

/// <summary>
/// Configuration class for all schedule types.
/// Maps to <c>sched.Schedule</c> which contains all schedule fields including type-specific ones.
/// </summary>
[ExcludeFromCodeCoverage]
[GenerateMapper]
[ManagedConfiguration( ServiceCategory = "Schedule")]
// Why: Must be non-abstract so the [GenerateMapper] source generator can create a POCO mapper.
// The DataGateway queries sched.Schedule using this type — it needs to instantiate it.
// Type-specific behavior (Cron, Interval) is handled by ServiceOptionType dispatch, not by subclassing.
public partial class ScheduleConfiguration : IScheduleDefinition
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ScheduleConfiguration"/> class.
    /// Default constructor for IOptions binding and header lookups.
    /// </summary>
    public ScheduleConfiguration() : this("Schedule", null, "Schedules")
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ScheduleConfiguration"/> class.
    /// Protected constructor for derived classes to set their type identity.
    /// </summary>
    /// <param name="serviceType">The service type (domain) - always "Schedule".</param>
    /// <param name="serviceOptionType">The service option type (e.g., "Interval", "Cron").</param>
    /// <param name="sectionName">The configuration section name for binding.</param>
    public ScheduleConfiguration(string serviceType, string? serviceOptionType, string sectionName)
    {
        ServiceType = serviceType;
        ServiceOptionType = serviceOptionType;
        SectionName = sectionName;
    }

    /// <inheritdoc />
    public Guid Id { get; set; } = Guid.CreateVersion7();

    /// <inheritdoc />
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the section name for configuration binding.
    /// </summary>
    public string SectionName { get; set; }

    /// <summary>
    /// Gets or sets the service type (domain) - always "Schedule" for this configuration.
    /// </summary>
    public string ServiceType { get; set; }

    /// <summary>
    /// Gets or sets the service option type (implementation variant) this configuration is for.
    /// </summary>
    [ValuesFrom(typeof(TriggerTypes))]
    public string? ServiceOptionType { get; set; }

    /// <inheritdoc />
    public virtual string ScheduleType => ServiceOptionType ?? "Unknown";

    /// <inheritdoc />
    public string PipelineName { get; set; } = string.Empty;

    /// <inheritdoc />
    public bool IsEnabled { get; set; } = true;

    /// <inheritdoc />
    public DateTimeOffset? NextRunTime { get; set; }

    /// <inheritdoc />
    public DateTimeOffset? LastRunTime { get; set; }

    /// <inheritdoc />
    public string? LastRunStatus { get; set; }

    /// <summary>
    /// Gets or sets an optional description of the schedule.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Gets or sets the maximum number of retry attempts on failure.
    /// </summary>
    public int MaxRetries { get; set; } = 3;

    /// <summary>
    /// Gets or sets the delay in seconds between retry attempts.
    /// </summary>
    public int RetryDelaySeconds { get; set; } = 60;

    /// <summary>
    /// Gets or sets the execution timeout in seconds.
    /// </summary>
    public int TimeoutSeconds { get; set; } = 3600;

    /// <summary>
    /// Gets or sets the IANA timezone ID for schedule evaluation (e.g., "America/New_York").
    /// Null defaults to UTC.
    /// </summary>
    public string? TimeZoneId { get; set; }

    /// <summary>
    /// Gets or sets the cron expression for Cron-type schedules.
    /// Null for non-Cron schedules.
    /// </summary>
    public string? CronExpression { get; set; }

    /// <summary>
    /// Gets or sets the interval in seconds for Interval-type schedules.
    /// Null for non-Interval schedules.
    /// </summary>
    public int? IntervalSeconds { get; set; }

    /// <summary>
    /// Gets or sets the one-time execution date and time for Once-type schedules.
    /// Null for non-Once schedules.
    /// </summary>
    public DateTimeOffset? OneTimeDateTime { get; set; }

    /// <summary>
    /// Gets or sets the triggering event name for Event-type schedules.
    /// Null for non-Event schedules.
    /// </summary>
    public string? EventName { get; set; }

    /// <summary>
    /// Gets or sets the tenant identifier for tenant isolation.
    /// Null means system-wide (visible to all tenants).
    /// </summary>
    public Guid? TenantId { get; set; }

    /// <summary>
    /// Gets the timestamp when the record was created in this system.
    /// </summary>
    public DateTimeOffset CreateDate { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// Gets the database user who created the record.
    /// </summary>
    public string CreateBy { get; set; } = string.Empty;

    /// <summary>
    /// Gets the application user on whose behalf the record was created.
    /// </summary>
    public string CreateOnBehalfOf { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the timestamp when the record was last modified.
    /// </summary>
    public DateTimeOffset ModifyDate { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// Gets or sets the database user who last modified the record.
    /// </summary>
    public string ModifyBy { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the application user on whose behalf the record was last modified.
    /// </summary>
    public string ModifyOnBehalfOf { get; set; } = string.Empty;

}
