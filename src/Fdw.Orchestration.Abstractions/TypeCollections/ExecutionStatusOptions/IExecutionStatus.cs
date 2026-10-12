using Fdw.Collections;

namespace Fdw.Orchestration.Abstractions.TypeCollections.ExecutionStatusOptions;

/// <summary>
/// Interface for execution status TypeOptions.
/// </summary>
/// <remarks>
/// Execution statuses track the current state of an orchestration or step execution.
/// Statuses indicate whether execution is pending, running, completed, or in an error state.
/// </remarks>
public interface IExecutionStatus : ITypeOption<int, ExecutionStatusBase>
{
    /// <summary>
    /// Gets whether this is a terminal status (execution has completed, no more transitions).
    /// </summary>
    bool IsTerminal { get; }

    /// <summary>
    /// Gets whether this status represents a successful execution.
    /// </summary>
    bool IsSuccess { get; }

    /// <summary>
    /// Gets whether this status represents a failure.
    /// </summary>
    bool IsFailure { get; }

    /// <summary>
    /// Gets whether the execution can be retried from this status.
    /// </summary>
    bool AllowsRetry { get; }

    /// <summary>
    /// Gets whether the execution can be resumed from this status.
    /// </summary>
    bool AllowsResume { get; }

    /// <summary>
    /// Gets whether the execution is currently in progress.
    /// </summary>
    bool IsInProgress { get; }

    /// <summary>
    /// Gets whether this status indicates warnings occurred during execution.
    /// </summary>
    bool HasWarnings { get; }
}
