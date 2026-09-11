using System;
using System.Diagnostics.CodeAnalysis;
using Fdw.Configuration;
using Fdw.Data;

namespace Fdw.Services.Scheduling.Abstractions.Configuration;

/// <summary>
/// What a schedule IS: when it fires, what it runs, and how a failed run is retried.
/// </summary>
/// <remarks>
/// Why Schedule has a single implementation: the trigger kinds (Cron, Interval, Once, Manual) are
/// discriminated by <c>Implementation</c> and share one <c>sched.Schedule</c> row rather than each
/// having a table of its own, so there is one implementation record and the kind selects which of
/// its fields apply. That makes Schedule a single-implementation domain, which takes the
/// <c>[Domain]Implementation</c> name.
/// </remarks>
[ExcludeFromCodeCoverage]
[GenerateMapper]
[ManagedConfiguration(ServiceCategory = "Schedule", ServiceType = "Schedule")]
public sealed partial class ScheduleImplementationConfiguration : IScheduleImplementationConfiguration
{
    /// <summary>Gets or sets the identifier assigned by the store.</summary>
    public Guid Id { get; set; }

    /// <summary>Gets or sets the domain record's durable id.</summary>
    public Guid ScheduleId { get; set; }


    /// <summary>Gets or sets the name this configuration is resolved by.</summary>
    // Why it maps with no column of its own: the name is the domain's, and there is one name for a
    // configured member. The domain provider joins the domain row to this one, and the join's result
    // set is what carries it in.
    public string Name { get; set; } = string.Empty;

    /// <inheritdoc/>
    /// <remarks>Set by the provider from the domain row; never persisted.</remarks>
    public string Domain { get; set; } = string.Empty;

    /// <inheritdoc/>
    /// <remarks>Set by the provider from the domain row; never persisted.</remarks>
    public string Implementation { get; set; } = string.Empty;

    /// <inheritdoc/>
    public string ScheduleType => Implementation;

    /// <inheritdoc/>
    public string PipelineName { get; set; } = string.Empty;

    /// <inheritdoc/>
    public string? Description { get; set; }

    /// <inheritdoc/>
    public bool IsEnabled { get; set; } = true;

    /// <inheritdoc/>
    public DateTimeOffset? NextRunTime { get; set; }

    /// <inheritdoc/>
    public DateTimeOffset? LastRunTime { get; set; }

    /// <inheritdoc/>
    public string? LastRunStatus { get; set; }

    /// <inheritdoc/>
    public Guid? TenantId { get; set; }

    /// <inheritdoc/>
    public string? CronExpression { get; set; }

    /// <inheritdoc/>
    public int? IntervalSeconds { get; set; }

    /// <inheritdoc/>
    public DateTimeOffset? OneTimeDateTime { get; set; }

    /// <inheritdoc/>
    public string? EventName { get; set; }

    /// <inheritdoc/>
    public string TimeZoneId { get; set; } = "UTC";

    /// <inheritdoc/>
    public int MaxRetries { get; set; }

    /// <inheritdoc/>
    public int RetryDelaySeconds { get; set; }

    /// <inheritdoc/>
    public int TimeoutSeconds { get; set; }

    /// <inheritdoc/>
    public bool IsCurrent { get; set; } = true;

    /// <inheritdoc/>
    public bool IsDeleted { get; set; }

    /// <inheritdoc/>
    public DateTimeOffset? SrcCreateDate { get; set; }

    /// <inheritdoc/>
    public DateTimeOffset CreateDate { get; set; }

    /// <inheritdoc/>
    public string CreateBy { get; set; } = string.Empty;

    /// <inheritdoc/>
    public string CreateOnBehalfOf { get; set; } = string.Empty;

    /// <inheritdoc/>
    public DateTimeOffset ModifyDate { get; set; }

    /// <inheritdoc/>
    public string ModifyBy { get; set; } = string.Empty;

    /// <inheritdoc/>
    public string ModifyOnBehalfOf { get; set; } = string.Empty;
}
