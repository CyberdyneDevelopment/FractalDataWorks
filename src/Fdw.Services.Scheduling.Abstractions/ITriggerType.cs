using System;
using Fdw.Collections;
using Fdw.Results;

namespace Fdw.Services.Scheduling.Abstractions;

/// <summary>
/// Interface for trigger type enhanced enums.
/// Defines the contract for scheduling trigger implementations.
/// </summary>
public interface ITriggerType : ITypeOption<int>
{
    /// <summary>
    /// Gets the interval for periodic triggers.
    /// Returns null for non-periodic trigger types.
    /// </summary>
    TimeSpan? Interval { get; }

    /// <summary>
    /// Gets the cron expression for cron-based triggers.
    /// Returns null for non-cron trigger types.
    /// </summary>
    string? CronExpression { get; }

    /// <summary>
    /// Gets a value indicating whether this trigger type is recurring.
    /// </summary>
    bool IsRecurring { get; }

    /// <summary>
    /// Gets a value indicating whether this trigger type supports multiple schedules.
    /// </summary>
    bool SupportsMultipleSchedules { get; }

    /// <summary>
    /// Gets the priority level for this trigger type.
    /// Higher values indicate higher priority.
    /// </summary>
    int Priority { get; }

    /// <summary>
    /// Calculates the next run time for the specified trigger configuration.
    /// </summary>
    /// <param name="trigger">The trigger configuration containing parameters for execution calculation.</param>
    /// <param name="lastExecution">The timestamp of the last execution, or null if never executed.</param>
    /// <returns>
    /// A result containing the next execution time in UTC, or a failure result if the trigger
    /// does not support automatic scheduling (e.g., Manual triggers).
    /// </returns>
    IGenericResult<DateTimeOffset> GetNextRunTime(IGenericTrigger trigger, DateTime? lastExecution);
}