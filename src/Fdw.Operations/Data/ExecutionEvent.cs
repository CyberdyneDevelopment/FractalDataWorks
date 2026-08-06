using System;
using System.Diagnostics.CodeAnalysis;
using Fdw.Data;

namespace Fdw.Operations.Data;

/// <summary>
/// Represents an execution event tracked in the ops.ExecutionEvent table.
/// Captures state transitions and notable events during execution item lifecycle.
/// </summary>
/// <remarks>
/// <para>
/// ExecutionEvent provides an append-only audit trail of events for each ExecutionItem:
/// <list type="bullet">
///   <item><description><strong>State Changes</strong> - Tracks transitions (Scheduled → Running → Completed)</description></item>
///   <item><description><strong>Warnings</strong> - Non-fatal issues during execution</description></item>
///   <item><description><strong>Errors</strong> - Failures requiring attention</description></item>
///   <item><description><strong>Milestones</strong> - Notable progress markers</description></item>
///   <item><description><strong>User Actions</strong> - Manual interventions (pause, resume, cancel)</description></item>
/// </list>
/// </para>
/// <para>
/// Each event records:
/// <list type="bullet">
///   <item><description>What happened (EventType, Message)</description></item>
///   <item><description>When it happened (Timestamp)</description></item>
///   <item><description>Who caused it (Actor - user or system)</description></item>
///   <item><description>State context (PreviousState, NewState)</description></item>
///   <item><description>Additional context (Data as JSON)</description></item>
/// </list>
/// </para>
/// </remarks>
[ExcludeFromCodeCoverage]
[GenerateMapper]
public sealed class ExecutionEvent
{
    /// <summary>
    /// Gets or sets the unique identifier for this event.
    /// </summary>
    public Guid Id { get; set; } = Guid.CreateVersion7();

    /// <summary>
    /// Gets or sets the identifier of the execution item this event belongs to.
    /// Foreign key to ops.ExecutionItem(Id).
    /// </summary>
    public Guid ExecutionItemId { get; set; }

    /// <summary>
    /// Gets or sets the sequence number within the execution item.
    /// Used for ordering events chronologically within an item.
    /// </summary>
    public int SequenceNumber { get; set; }

    /// <summary>
    /// Gets or sets the type of event.
    /// </summary>
    /// <value>
    /// Common types: "StateChange", "Warning", "Error", "Milestone", "UserAction", "Retry".
    /// </value>
    /// <remarks>
    /// Used for filtering and categorizing events in queries and dashboards.
    /// </remarks>
    public string EventType { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the execution state before this event (for StateChange events).
    /// Null for non-state-change events.
    /// </summary>
    /// <remarks>
    /// References ExecutionStateTypes. Examples: "Scheduled", "Running", "Paused".
    /// Only populated when EventType is "StateChange".
    /// </remarks>
    public string? PreviousState { get; set; }

    /// <summary>
    /// Gets or sets the execution state after this event (for StateChange events).
    /// Null for non-state-change events.
    /// </summary>
    /// <remarks>
    /// References ExecutionStateTypes. Examples: "Running", "Completed", "Failed".
    /// Only populated when EventType is "StateChange".
    /// </remarks>
    public string? NewState { get; set; }

    /// <summary>
    /// Gets or sets the human-readable event message.
    /// </summary>
    /// <remarks>
    /// Should be concise but descriptive. Examples:
    /// - "Execution started by scheduler"
    /// - "Step completed successfully in 1.2 seconds"
    /// - "Retrying after connection timeout"
    /// - "Paused by user admin@example.com"
    /// </remarks>
    public string? Message { get; set; }

    /// <summary>
    /// Gets or sets additional event context as JSON.
    /// </summary>
    /// <remarks>
    /// Stores structured data relevant to the event:
    /// - Error stack traces
    /// - Performance metrics
    /// - Resource identifiers
    /// - User action details
    /// Parse as needed at runtime.
    /// </remarks>
    public string? Data { get; set; }

    /// <summary>
    /// Gets or sets the actor (user or system) that caused this event.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Format recommendations:
    /// <list type="bullet">
    ///   <item><description>User actions: "user:email@example.com" or "user:UserId"</description></item>
    ///   <item><description>System actions: "system:SchedulerService" or "system:WebhookHandler"</description></item>
    ///   <item><description>API actions: "api:ClientName" or "api:ApiKey"</description></item>
    /// </list>
    /// </para>
    /// Null if actor is unknown or not applicable.
    /// </remarks>
    public string? Actor { get; set; }

    /// <summary>
    /// Gets or sets the timestamp when this event occurred.
    /// </summary>
    /// <remarks>
    /// Always UTC. Used for chronological ordering and time-based queries.
    /// </remarks>
    public DateTimeOffset Timestamp { get; set; } = DateTimeOffset.UtcNow;
}
