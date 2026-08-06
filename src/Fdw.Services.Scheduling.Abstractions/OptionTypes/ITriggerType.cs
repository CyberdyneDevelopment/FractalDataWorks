using System;
using Fdw.Results;

namespace Fdw.Services.Scheduling.Abstractions.OptionTypes;

/// <summary>
/// Interface defining the contract for trigger type enum options in the scheduling system.
/// </summary>
public interface ITriggerType
{
    /// <summary>
    /// Gets a value indicating whether this trigger type requires schedule persistence.
    /// </summary>
    /// <value>
    /// <c>true</c> if the trigger type needs to store schedule state between executions;
    /// <c>false</c> if it's stateless or ephemeral.
    /// </value>
    /// <remarks>
    /// <list type="bullet">
    ///   <item><description><strong>Requires schedule</strong>: Cron, Interval, Once (need to track next execution)</description></item>
    ///   <item><description><strong>No schedule needed</strong>: Manual, Event (triggered externally)</description></item>
    /// </list>
    /// </remarks>
    bool RequiresSchedule { get; }

    /// <summary>
    /// Gets a value indicating whether this trigger type executes immediately upon creation.
    /// </summary>
    /// <value>
    /// <c>true</c> if the trigger should fire immediately when first created;
    /// <c>false</c> if it waits for its calculated next execution time.
    /// </value>
    /// <remarks>
    /// <list type="bullet">
    ///   <item><description><strong>Immediate execution</strong>: Manual, Immediate, Event-driven triggers</description></item>
    ///   <item><description><strong>Scheduled execution</strong>: Cron, Interval, Once (wait for proper time)</description></item>
    /// </list>
    /// </remarks>
    bool IsImmediate { get; }

    /// <summary>
    /// Gets the unique identifier for this enum value.
    /// </summary>
    int Id { get; }

    /// <summary>
    /// Gets the name of this enum value.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Calculates the next execution time for the specified trigger.
    /// </summary>
    /// <param name="trigger">The trigger configuration containing parameters for execution calculation.</param>
    /// <param name="lastExecution">The timestamp of the last execution, or null if never executed.</param>
    /// <returns>
    /// The next execution time in UTC, or null if the trigger will never execute again.
    /// </returns>
    /// <remarks>
    /// <para>
    /// This method implements the core scheduling logic for each trigger type:
    /// </para>
    /// <list type="bullet">
    ///   <item><description><strong>Cron triggers</strong>: Parse cron expression and calculate next occurrence</description></item>
    ///   <item><description><strong>Interval triggers</strong>: Add interval to last execution (or start time)</description></item>
    ///   <item><description><strong>Once triggers</strong>: Return configured time if not yet executed, null otherwise</description></item>
    ///   <item><description><strong>Manual triggers</strong>: Always return null (no automatic scheduling)</description></item>
    /// </list>
    /// <para>
    /// The method should handle timezone conversions, daylight saving time transitions,
    /// and edge cases like invalid dates or expired schedules.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // For a cron trigger with "0 9 * * MON-FRI" (9 AM weekdays)
    /// var nextExecution = cronTrigger.CalculateNextExecution(trigger, DateTime.UtcNow);
    /// // Returns next weekday at 9 AM in the trigger's configured timezone
    /// 
    /// // For an interval trigger with 30-minute intervals
    /// var nextExecution = intervalTrigger.CalculateNextExecution(trigger, lastRun);
    /// // Returns lastRun + 30 minutes
    /// 
    /// // For a manual trigger
    /// var nextExecution = manualTrigger.CalculateNextExecution(trigger, lastRun);
    /// // Always returns null - manual triggers don't auto-schedule
    /// </code>
    /// </example>
    // Abstract method - implementation coverage tested in derived classes
    DateTime? CalculateNextExecution(IGenericTrigger trigger, DateTime? lastExecution);

    /// <summary>
    /// Validates that the trigger configuration is valid for this trigger type.
    /// </summary>
    /// <param name="trigger">The trigger configuration to validate.</param>
    /// <returns>
    /// A result indicating whether the trigger is valid. Success result if valid,
    /// error result with validation messages if invalid.
    /// </returns>
    /// <remarks>
    /// <para>
    /// This method validates trigger-type-specific configuration parameters:
    /// </para>
    /// <list type="bullet">
    ///   <item><description><strong>Cron triggers</strong>: Validate cron expression syntax and timezone</description></item>
    ///   <item><description><strong>Interval triggers</strong>: Validate interval is positive, start delay is valid</description></item>
    ///   <item><description><strong>Once triggers</strong>: Validate execution time is in the future</description></item>
    ///   <item><description><strong>Manual triggers</strong>: Validate minimal configuration requirements</description></item>
    /// </list>
    /// <para>
    /// The validation should be comprehensive enough to catch configuration errors early,
    /// before the trigger is used in production scheduling.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // Validate a cron trigger
    /// var result = cronTrigger.ValidateTrigger(trigger);
    /// if (result.Error)
    /// {
    ///     // Handle validation errors - invalid cron expression, bad timezone, etc.
    ///     Logger.LogError("Cron trigger validation failed: {Messages}", result.Messages);
    /// }
    /// 
    /// // Validate an interval trigger
    /// var result = intervalTrigger.ValidateTrigger(trigger);
    /// if (result.Error)
    /// {
    ///     // Handle validation errors - negative interval, invalid configuration format
    ///     Logger.LogError("Interval trigger validation failed: {Messages}", result.Messages);
    /// }
    /// </code>
    /// </example>
    // Abstract method - implementation coverage tested in derived classes
    IGenericResult ValidateTrigger(IGenericTrigger trigger);

    /// <summary>
    /// Calculates the next run time for the specified trigger configuration, returning a structured result.
    /// </summary>
    /// <param name="trigger">The trigger configuration containing parameters for execution calculation.</param>
    /// <param name="lastExecution">The timestamp of the last execution, or null if never executed.</param>
    /// <returns>
    /// A result containing the next execution time in UTC, or a failure result if the trigger
    /// does not support automatic scheduling (e.g., Manual triggers) or configuration is invalid.
    /// </returns>
    IGenericResult<DateTimeOffset> GetNextRunTime(IGenericTrigger trigger, DateTime? lastExecution);

    /// <summary>
    /// Determines whether this trigger is due for execution at the specified time.
    /// </summary>
    /// <param name="trigger">The trigger configuration containing parameters for execution calculation.</param>
    /// <param name="lastExecution">The timestamp of the last execution, or null if never executed.</param>
    /// <param name="now">The current time to evaluate against.</param>
    /// <returns>
    /// <c>true</c> if the trigger should execute now based on its schedule and last execution;
    /// <c>false</c> if it is not yet due or does not support automatic scheduling.
    /// </returns>
    bool IsDue(IGenericTrigger trigger, DateTime? lastExecution, DateTimeOffset now);
}