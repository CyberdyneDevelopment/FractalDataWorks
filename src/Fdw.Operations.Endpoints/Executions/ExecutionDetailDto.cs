using System;
using System.Collections.Generic;

namespace Fdw.Operations.Endpoints.Executions;

/// <summary>
/// Detailed execution response.
/// </summary>
public class ExecutionDetailDto
{
    /// <summary>
    /// Gets or sets the execution ID.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the parent execution ID.
    /// </summary>
    public Guid? ParentId { get; set; }

    /// <summary>
    /// Gets or sets the root execution ID.
    /// </summary>
    public Guid RootId { get; set; }

    /// <summary>
    /// Gets or sets the item type (Workflow, Job, Stage, Step, Task).
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
    /// Gets or sets the trigger source.
    /// </summary>
    public string? TriggerSource { get; set; }

    /// <summary>
    /// Gets or sets the parameters.
    /// </summary>
    public IReadOnlyDictionary<string, object?>? Parameters { get; set; }

    /// <summary>
    /// Gets or sets when the execution was created.
    /// </summary>
    public DateTimeOffset CreatedAt { get; set; }

    /// <summary>
    /// Gets or sets when the execution started.
    /// </summary>
    public DateTimeOffset? StartedAt { get; set; }

    /// <summary>
    /// Gets or sets when the execution completed.
    /// </summary>
    public DateTimeOffset? CompletedAt { get; set; }

    /// <summary>
    /// Gets or sets the result code.
    /// </summary>
    public string? ResultCode { get; set; }

    /// <summary>
    /// Gets or sets the result message.
    /// </summary>
    public string? ResultMessage { get; set; }
}
