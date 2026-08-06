using System;

namespace Fdw.StateCollections;

/// <summary>
/// Event record describing a successful transition. Emitted by the engine after persistence
/// and dispatched to every registered <see cref="IStateTransitionHandler{TState}"/>.
/// </summary>
/// <typeparam name="TState">The state interface for this machine's domain.</typeparam>
public interface IStateTransition<TState>
    where TState : IStateOption<TState>
{
    /// <summary>The state the engine exited.</summary>
    TState From { get; }

    /// <summary>The state the engine entered.</summary>
    TState To { get; }

    /// <summary>Correlation id of the firing call.</summary>
    Guid CorrelationId { get; }

    /// <summary>UTC instant the transition completed.</summary>
    DateTimeOffset OccurredAt { get; }
}
