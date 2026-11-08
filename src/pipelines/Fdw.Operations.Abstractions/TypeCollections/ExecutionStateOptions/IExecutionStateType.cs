using System.Collections.Generic;
using Fdw.Collections;

namespace Fdw.Operations.Abstractions.TypeCollections.ExecutionStateOptions;

/// <summary>
/// Interface for execution state types in the state machine.
/// </summary>
public interface IExecutionStateType : ITypeOption<int, ExecutionStateTypeBase>
{
    /// <summary>
    /// Gets a value indicating whether this is a terminal state.
    /// </summary>
    bool IsTerminal { get; }

    /// <summary>
    /// Gets a value indicating whether this state represents success.
    /// </summary>
    bool IsSuccess { get; }

    /// <summary>
    /// Gets a value indicating whether this state represents a failure.
    /// </summary>
    bool IsFailure { get; }

    /// <summary>
    /// Gets a value indicating whether a run in this state is executing: it has taken its turn and is neither waiting (Scheduled, AwaitingApproval) nor finished (FDW-4488).
    /// </summary>
    bool IsExecuting { get; }

    /// <summary>
    /// Gets a value indicating whether this state is terminal and neither a success nor a failure: the run was cancelled, timed out or interrupted (FDW-4488).
    /// </summary>
    bool IsStopped { get; }

    /// <summary>
    /// Gets the valid transitions from this state.
    /// </summary>
    IReadOnlyList<string> ValidTransitions { get; }

    /// <summary>
    /// Gets a value indicating whether escalation can be triggered from this state.
    /// </summary>
    bool CanTriggerEscalation { get; }

    /// <summary>
    /// Determines whether a transition to the specified state is valid.
    /// </summary>
    /// <param name="targetState">The target state name.</param>
    /// <returns>True if the transition is valid; otherwise, false.</returns>
    bool CanTransitionTo(string targetState);
}
