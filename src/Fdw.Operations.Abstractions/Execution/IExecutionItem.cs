using System;
using System.Collections.Generic;
using Fdw.Operations.Abstractions.TypeCollections.Execution;
using Fdw.Operations.Abstractions.TypeCollections.ExecutionStateOptions;

namespace Fdw.Operations.Abstractions.Execution;

/// <summary>
/// Represents an execution item in the tracking hierarchy.
/// </summary>
public interface IExecutionItem
{
    /// <summary>
    /// Gets the unique identifier for this execution item.
    /// </summary>
    Guid Id { get; }

    /// <summary>
    /// Gets the parent execution item ID, if any.
    /// </summary>
    Guid? ParentId { get; }

    /// <summary>
    /// Gets the root execution item ID (workflow ID).
    /// </summary>
    Guid RootId { get; }

    /// <summary>
    /// Gets the execution item type.
    /// </summary>
    IExecutionItemType ItemType { get; }

    /// <summary>
    /// Gets the current execution state.
    /// </summary>
    IExecutionStateType State { get; }

    /// <summary>
    /// Gets the name of this execution item.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Gets the correlation ID for distributed tracing.
    /// </summary>
    string? CorrelationId { get; }

    /// <summary>
    /// Gets the source that triggered this execution.
    /// </summary>
    string? TriggerSource { get; }

    /// <summary>
    /// Gets the UTC timestamp when this item was created.
    /// </summary>
    DateTimeOffset CreatedAt { get; }

    /// <summary>
    /// Gets the UTC timestamp when this item started executing.
    /// </summary>
    DateTimeOffset? StartedAt { get; }

    /// <summary>
    /// Gets the UTC timestamp when this item finished executing.
    /// </summary>
    DateTimeOffset? CompletedAt { get; }

    /// <summary>
    /// Gets the execution parameters.
    /// </summary>
    IReadOnlyDictionary<string, object?> Parameters { get; }

    /// <summary>
    /// Gets the result code if execution completed with a result.
    /// </summary>
    string? ResultCode { get; }

    /// <summary>
    /// Gets the result message if execution completed with a message.
    /// </summary>
    string? ResultMessage { get; }
}
