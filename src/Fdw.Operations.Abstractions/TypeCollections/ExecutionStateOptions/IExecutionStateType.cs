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
