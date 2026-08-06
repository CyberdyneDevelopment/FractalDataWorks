using System;
using System.Collections.Generic;
using Fdw.Results;

namespace Fdw.Services.Scheduling.Abstractions;

/// <summary>
/// Represents a schedule definition that defines WHEN a process should be executed.
/// </summary>
/// <remarks>
/// A schedule contains the timing information and metadata needed to trigger process execution.
/// It is separate from the execution logic itself, following the separation of concerns between
/// scheduling (WHEN) and execution (HOW).
/// </remarks>
public interface IGenericSchedule
{
    /// <summary>
    /// Gets the unique identifier for this schedule.
    /// </summary>
    /// <value>A unique identifier for the schedule instance.</value>
    string ScheduleId { get; }

    /// <summary>
    /// Gets the name of the schedule for display and logging purposes.
    /// </summary>
    /// <value>A human-readable name for the schedule.</value>
    string ScheduleName { get; }

    /// <summary>
    /// Gets the identifier of the process that should be executed when this schedule triggers.
    /// </summary>
    /// <value>The process identifier to execute.</value>
    string ProcessId { get; }

    /// <summary>
    /// Gets the cron expression that defines when this schedule should trigger.
    /// </summary>
    /// <value>A valid cron expression string.</value>
    string CronExpression { get; }

    /// <summary>
    /// Gets the next scheduled execution time based on the cron expression.
    /// </summary>
    /// <value>The next time this schedule will trigger, or null if it won't trigger again.</value>
    DateTime? NextExecution { get; }

    /// <summary>
    /// Gets a value indicating whether this schedule is currently active.
    /// </summary>
    /// <value><c>true</c> if the schedule is active and can trigger; otherwise, <c>false</c>.</value>
    bool IsActive { get; }

    /// <summary>
    /// Gets the time zone identifier for interpreting the cron expression.
    /// </summary>
    /// <value>A valid time zone identifier, defaults to UTC if not specified.</value>
    string TimeZoneId { get; }

    /// <summary>
    /// Gets optional metadata associated with this schedule.
    /// </summary>
    /// <value>A dictionary of key-value pairs containing schedule metadata.</value>
    IReadOnlyDictionary<string, object>? Metadata { get; }
}