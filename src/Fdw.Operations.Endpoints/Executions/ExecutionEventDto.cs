using System;
using System.Collections.Generic;

namespace Fdw.Operations.Endpoints.Executions;

/// <summary>
/// Execution event response.
/// </summary>
public class ExecutionEventDto
{
    /// <summary>
    /// Gets or sets the event ID.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the execution item ID.
    /// </summary>
    public Guid ExecutionItemId { get; set; }

    /// <summary>
    /// Gets or sets the sequence number.
    /// </summary>
    public int SequenceNumber { get; set; }

    /// <summary>
    /// Gets or sets the event type.
    /// </summary>
    public required string EventType { get; set; }

    /// <summary>
    /// Gets or sets the previous state.
    /// </summary>
    public string? PreviousState { get; set; }

    /// <summary>
    /// Gets or sets the new state.
    /// </summary>
    public string? NewState { get; set; }

    /// <summary>
    /// Gets or sets the message.
    /// </summary>
    public string? Message { get; set; }

    /// <summary>
    /// Gets or sets the event data.
    /// </summary>
    public IReadOnlyDictionary<string, object?>? Data { get; set; }

    /// <summary>
    /// Gets or sets the actor.
    /// </summary>
    public string? Actor { get; set; }

    /// <summary>
    /// Gets or sets the timestamp.
    /// </summary>
    public DateTimeOffset Timestamp { get; set; }
}
