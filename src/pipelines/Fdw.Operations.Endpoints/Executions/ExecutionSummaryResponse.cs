using System;
using Fdw.Operations.Abstractions.Execution;

namespace Fdw.Operations.Endpoints.Executions;

/// <summary>
/// Summary execution information for listing.
/// </summary>
/// <remarks>
/// FDW-932, FDW-933: a list row carries what an execution recorded about how it ended and what started it, so a screen that
/// lists failed runs can say why each failed without a second read per row. <see cref="Of"/> is the one place an execution
/// item becomes a summary; the list and the children reads both use it.
/// </remarks>
public class ExecutionSummaryResponse
{
    /// <summary>
    /// Gets or sets the execution ID.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the item type.
    /// </summary>
    public required string ItemType { get; set; }

    /// <summary>
    /// Gets or sets the execution name.
    /// </summary>
    public required string Name { get; set; }

    /// <summary>
    /// Gets or sets the current state.
    /// </summary>
    public required string State { get; set; }

    /// <summary>
    /// Gets or sets the correlation ID.
    /// </summary>
    public string? CorrelationId { get; set; }

    /// <summary>
    /// Gets or sets the identifier of the execution this one was started under; null for a root execution.
    /// </summary>
    public Guid? ParentId { get; set; }

    /// <summary>
    /// Gets or sets the identifier of the root execution this one belongs to.
    /// </summary>
    public Guid RootId { get; set; }

    /// <summary>
    /// Gets or sets what started the execution (for example Api or Scheduler); null when none was recorded.
    /// </summary>
    public string? TriggerSource { get; set; }

    /// <summary>
    /// Gets or sets when the execution was created.
    /// </summary>
    public DateTimeOffset CreatedAt { get; set; }

    /// <summary>
    /// Gets or sets when the execution started running; null while it has not.
    /// </summary>
    public DateTimeOffset? StartedAt { get; set; }

    /// <summary>
    /// Gets or sets when the execution completed.
    /// </summary>
    public DateTimeOffset? CompletedAt { get; set; }

    /// <summary>
    /// Gets or sets the code the execution ended with; null while it has not ended.
    /// </summary>
    public string? ResultCode { get; set; }

    /// <summary>
    /// Gets or sets the message the execution ended with, which is the reason when it failed; null when none was recorded.
    /// </summary>
    public string? ResultMessage { get; set; }

    /// <summary>
    /// Builds the summary of an execution item.
    /// </summary>
    /// <param name="item">The execution item.</param>
    /// <returns>The summary.</returns>
    public static ExecutionSummaryResponse Of(IExecutionItem item)
    {
        ArgumentNullException.ThrowIfNull(item);

        return new ExecutionSummaryResponse
        {
            Id = item.Id,
            ItemType = item.ItemType.Name,
            Name = item.Name,
            State = item.State.Name,
            CorrelationId = item.CorrelationId,
            ParentId = item.ParentId,
            RootId = item.RootId,
            TriggerSource = item.TriggerSource,
            CreatedAt = item.CreatedAt,
            StartedAt = item.StartedAt,
            CompletedAt = item.CompletedAt,
            ResultCode = item.ResultCode,
            ResultMessage = item.ResultMessage
        };
    }
}
