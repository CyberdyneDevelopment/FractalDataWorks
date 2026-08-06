using System;
using System.Collections.Generic;

namespace Fdw.Operations.Abstractions.Execution;

/// <summary>
/// Represents an append-only event in the execution log.
/// </summary>
public interface IExecutionEvent
{
    /// <summary>
    /// Gets the unique identifier for this event.
    /// </summary>
    Guid Id { get; }

    /// <summary>
    /// Gets the execution item ID this event belongs to.
    /// </summary>
    Guid ExecutionItemId { get; }

    /// <summary>
    /// Gets the sequence number within the execution item.
    /// </summary>
    int SequenceNumber { get; }

    /// <summary>
    /// Gets the UTC timestamp when this event occurred.
    /// </summary>
    DateTimeOffset Timestamp { get; }

    /// <summary>
    /// Gets the event type (e.g., "StateChanged", "ProgressUpdated", "MessageLogged").
    /// </summary>
    string EventType { get; }

    /// <summary>
    /// Gets the previous state (for state transitions).
    /// </summary>
    string? PreviousState { get; }

    /// <summary>
    /// Gets the new state (for state transitions).
    /// </summary>
    string? NewState { get; }

    /// <summary>
    /// Gets the event message.
    /// </summary>
    string? Message { get; }

    /// <summary>
    /// Gets the event data as key-value pairs.
    /// </summary>
    IReadOnlyDictionary<string, object?>? Data { get; }

    /// <summary>
    /// Gets the user or system that caused this event.
    /// </summary>
    string? Actor { get; }
}
