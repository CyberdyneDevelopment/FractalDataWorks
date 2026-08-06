using System;

namespace Fdw.StateCollections;

/// <summary>
/// Default <see cref="IStateTransition{TState}"/> record. Domains can replace with their own
/// implementation if they need additional fields, but most use this directly.
/// </summary>
/// <typeparam name="TState">The state interface for this machine's domain.</typeparam>
public sealed class StateTransition<TState> : IStateTransition<TState>
    where TState : IStateOption<TState>
{
    /// <summary>Initializes a new transition record.</summary>
    public StateTransition(TState from, TState to, Guid correlationId, DateTimeOffset occurredAt)
    {
        From = from;
        To = to;
        CorrelationId = correlationId;
        OccurredAt = occurredAt;
    }

    /// <inheritdoc />
    public TState From { get; }

    /// <inheritdoc />
    public TState To { get; }

    /// <inheritdoc />
    public Guid CorrelationId { get; }

    /// <inheritdoc />
    public DateTimeOffset OccurredAt { get; }
}
