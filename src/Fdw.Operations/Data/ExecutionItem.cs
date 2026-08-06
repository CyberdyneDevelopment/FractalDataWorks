using System;
using System.Diagnostics.CodeAnalysis;
using Fdw.Configuration;
using Fdw.Data;
using Fdw.Operations.Abstractions.TypeCollections.Execution;
using Fdw.Operations.Abstractions.TypeCollections.ExecutionStateOptions;

namespace Fdw.Operations.Data;

/// <summary>
/// Represents an execution item tracked in the ops.ExecutionItem table.
/// Supports self-referential hierarchy for workflow → job → stage → step → task decomposition.
/// </summary>
/// <remarks>
/// <para>
/// ExecutionItem represents any trackable unit of work in the system:
/// <list type="bullet">
///   <item><description><strong>Workflow</strong> - Top-level orchestration container</description></item>
///   <item><description><strong>Job</strong> - Schedulable unit of execution</description></item>
///   <item><description><strong>Stage</strong> - Logical phase within a job</description></item>
///   <item><description><strong>Step</strong> - Atomic operation</description></item>
///   <item><description><strong>Task</strong> - Sub-unit of a step</description></item>
/// </list>
/// </para>
/// <para>
/// Flexible containment: any type can contain any type at a lower hierarchy level.
/// For example, a Workflow can directly contain Tasks without intermediate levels.
/// </para>
/// <para>
/// Each item maintains:
/// <list type="bullet">
///   <item><description>Self-referential parent link (ParentExecutionItemId)</description></item>
///   <item><description>Root workflow link (RootExecutionItemId)</description></item>
///   <item><description>State tracking (Scheduled, Running, Completed, Failed, Cancelled)</description></item>
///   <item><description>Timing information (CreatedAt, StartedAt, CompletedAt, DurationMs)</description></item>
///   <item><description>Result tracking (ResultCode, ResultMessage)</description></item>
/// </list>
/// </para>
/// </remarks>
[ExcludeFromCodeCoverage]
[GenerateMapper]
public sealed class ExecutionItem
{
    /// <summary>
    /// Gets or sets the unique identifier for this execution item.
    /// </summary>
    public Guid Id { get; set; } = Guid.CreateVersion7();

    /// <summary>
    /// Gets or sets the identifier of the parent execution item.
    /// Null for root-level items (workflows).
    /// </summary>
    /// <remarks>
    /// Self-referential foreign key to ops.ExecutionItem(Id).
    /// Forms the hierarchical structure with flexible containment.
    /// </remarks>
    public Guid? ParentExecutionItemId { get; set; }

    /// <summary>
    /// Gets or sets the identifier of the root execution item (workflow).
    /// Always points to the top-level workflow for efficient root queries.
    /// </summary>
    /// <remarks>
    /// For root items, RootExecutionItemId == Id.
    /// For all descendants, points to the workflow ancestor.
    /// </remarks>
    public Guid RootExecutionItemId { get; set; }

    /// <summary>
    /// Gets or sets the type of execution item.
    /// </summary>
    /// <value>
    /// Valid values: "Workflow", "Job", "Stage", "Step", "Task".
    /// </value>
    /// <remarks>
    /// References ExecutionItemTypes TypeCollection for validation.
    /// </remarks>
    [ValuesFrom(typeof(ExecutionItemTypes))]
    public string ItemType { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the name of this execution item.
    /// </summary>
    /// <remarks>
    /// Should be descriptive and unique within the parent context.
    /// Example: "Import NFL Stats", "Load Player Data", "Validate Records".
    /// </remarks>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the current execution state.
    /// </summary>
    /// <value>
    /// States from ExecutionStateTypes: "Scheduled", "Triggered", "Initialized",
    /// "Running", "Paused", "Retrying", "Compensating", "Completed", "Failed", "Cancelled".
    /// </value>
    /// <remarks>
    /// State transitions are validated against ExecutionStateTypes.
    /// </remarks>
    [ValuesFrom(typeof(ExecutionStateTypes))]
    public string State { get; set; } = "Scheduled";

    /// <summary>
    /// Gets or sets the correlation identifier for tracing related operations.
    /// </summary>
    /// <remarks>
    /// Used to correlate distributed operations across services.
    /// Typically propagated from the initiating request.
    /// </remarks>
    public string? CorrelationId { get; set; }

    /// <summary>
    /// Gets or sets the source that triggered this execution.
    /// </summary>
    /// <remarks>
    /// Examples: "Manual", "Schedule:Daily", "Webhook:GitHub", "API:User123".
    /// Useful for audit trails and debugging.
    /// </remarks>
    public string? TriggerSource { get; set; }

    /// <summary>
    /// Gets or sets the execution parameters as JSON.
    /// </summary>
    /// <remarks>
    /// Stores configuration, inputs, or context specific to this execution.
    /// Serialized as JSON for flexibility. Parse as needed at runtime.
    /// </remarks>
    public string? Parameters { get; set; }

    /// <summary>
    /// Gets or sets the timestamp when this execution item was created.
    /// </summary>
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// Gets or sets the timestamp when execution started.
    /// Null if not yet started.
    /// </summary>
    public DateTimeOffset? StartedAt { get; set; }

    /// <summary>
    /// Gets or sets the timestamp when execution completed.
    /// Null if still running or not yet started.
    /// </summary>
    public DateTimeOffset? CompletedAt { get; set; }

    /// <summary>
    /// Gets or sets the execution duration in milliseconds.
    /// Calculated as CompletedAt - StartedAt when execution finishes.
    /// </summary>
    public long? DurationMs { get; set; }

    /// <summary>
    /// Gets or sets the result code from execution.
    /// </summary>
    /// <remarks>
    /// Corresponds to OperationsResultCodes or domain-specific ResultCodes.
    /// </remarks>
    public string? ResultCode { get; set; }

    /// <summary>
    /// Gets or sets the human-readable result message.
    /// </summary>
    /// <remarks>
    /// Provides context for the ResultCode. May include error details or success summary.
    /// </remarks>
    public string? ResultMessage { get; set; }
}
