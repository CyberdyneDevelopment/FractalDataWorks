using System;
using System.Collections.Generic;
using Fdw.Collections;

namespace Fdw.Operations.Abstractions.TypeCollections.ExecutionStateOptions;

/// <summary>
/// Base class for execution state types using the CRTP pattern.
/// Defines the state machine: Scheduled → Triggered → Initialized → Running → Completed/Failed
/// with additional states: Paused, Compensating, Cancelled.
/// </summary>
public abstract class ExecutionStateTypeBase : TypeOptionBase<int, ExecutionStateTypeBase>, IExecutionStateType
{
    /// <summary>
    /// Initializes a new instance for the Empty sentinel.
    /// </summary>
    protected ExecutionStateTypeBase()
        : base(0, "NotFound", "TypeOptions:NotFound", "Not Found", "Unknown execution state type", null)
    {
        IsTerminal = true;
        IsSuccess = false;
        IsFailure = false;
        CanTriggerEscalation = false;
        ValidTransitions = Array.Empty<string>();
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ExecutionStateTypeBase"/> class.
    /// </summary>
    /// <param name="id">Unique identifier for this state.</param>
    /// <param name="name">Name of the state (must match TypeOption attribute).</param>
    /// <param name="displayName">Display name for UI presentation.</param>
    /// <param name="isTerminal">Whether this is a terminal state.</param>
    /// <param name="isSuccess">Whether this state represents success.</param>
    /// <param name="isFailure">Whether this state represents failure.</param>
    /// <param name="canTriggerEscalation">Whether escalation can be triggered from this state.</param>
    /// <param name="validTransitions">Valid state transitions from this state.</param>
    protected ExecutionStateTypeBase(
        int id,
        string name,
        string displayName,
        bool isTerminal,
        bool isSuccess,
        bool isFailure,
        bool canTriggerEscalation,
        IReadOnlyList<string> validTransitions)
        : base(id, name, $"TypeOptions:{name}", displayName, $"Execution state: {name}", null)
    {
        IsTerminal = isTerminal;
        IsSuccess = isSuccess;
        IsFailure = isFailure;
        CanTriggerEscalation = canTriggerEscalation;
        ValidTransitions = validTransitions;
    }

    /// <inheritdoc />
    public bool IsTerminal { get; }

    /// <inheritdoc />
    public bool IsSuccess { get; }

    /// <inheritdoc />
    public bool IsFailure { get; }

    /// <inheritdoc />
    public bool CanTriggerEscalation { get; }

    /// <inheritdoc />
    public IReadOnlyList<string> ValidTransitions { get; }

    /// <summary>
    /// Determines whether a transition to the specified state is valid.
    /// </summary>
    /// <param name="targetState">The target state name.</param>
    /// <returns>True if the transition is valid; otherwise, false.</returns>
    public bool CanTransitionTo(string targetState)
    {
        if (IsTerminal || string.IsNullOrEmpty(targetState))
        {
            return false;
        }

        foreach (var validTransition in ValidTransitions)
        {
            if (string.Equals(validTransition, targetState, StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
    }
}
