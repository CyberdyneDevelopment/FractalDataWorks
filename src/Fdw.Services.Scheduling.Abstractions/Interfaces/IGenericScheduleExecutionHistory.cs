using System;
using System.Collections.Generic;
using Fdw.Services.Execution.Abstractions.OptionTypes;

namespace Fdw.Services.Scheduling.Abstractions;

/// <summary>
/// Represents the execution history of a scheduled process.
/// </summary>
/// <remarks>
/// This interface provides detailed information about individual schedule executions,
/// including timing, results, and any errors that occurred during execution.
/// </remarks>
public interface IGenericScheduleExecutionHistory
{
    /// <summary>
    /// Gets the unique identifier for this execution instance.
    /// </summary>
    /// <value>A unique identifier for the execution.</value>
    string ExecutionId { get; }

    /// <summary>
    /// Gets the identifier of the schedule that triggered this execution.
    /// </summary>
    /// <value>The schedule identifier.</value>
    string ScheduleId { get; }

    /// <summary>
    /// Gets the time when this execution was triggered.
    /// </summary>
    /// <value>The execution trigger time.</value>
    DateTime TriggeredAt { get; }

    /// <summary>
    /// Gets the time when this execution started.
    /// </summary>
    /// <value>The execution start time, or null if execution hasn't started.</value>
    DateTime? StartedAt { get; }

    /// <summary>
    /// Gets the time when this execution completed.
    /// </summary>
    /// <value>The execution completion time, or null if still running or failed to start.</value>
    DateTime? CompletedAt { get; }

    /// <summary>
    /// Gets the duration of the execution.
    /// </summary>
    /// <value>The execution duration, or null if not yet completed.</value>
    TimeSpan? Duration { get; }

    /// <summary>
    /// Gets a value indicating whether this execution was triggered manually.
    /// </summary>
    /// <value><c>true</c> if triggered manually; <c>false</c> if triggered by schedule.</value>
    bool WasTriggeredManually { get; }

    /// <summary>
    /// Gets the result status of the execution.
    /// </summary>
    /// <value>The execution result status.</value>
    IProcessState Status { get; }

    /// <summary>
    /// Gets any error message associated with this execution.
    /// </summary>
    /// <value>The error message, or null if execution was successful.</value>
    string? ErrorMessage { get; }

    /// <summary>
    /// Gets additional metadata associated with this execution.
    /// </summary>
    /// <value>A dictionary of execution metadata.</value>
    IReadOnlyDictionary<string, object>? Metadata { get; }
}