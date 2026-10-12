using System;
using System.Collections.Generic;

namespace Fdw.UI.Pipelines.Clients.Models;

/// <summary>
/// Full detail for a pipeline, including the visual graph (tasks and connections).
/// </summary>
public sealed class PipelineDetailPayload
{
    /// <summary>
    /// Gets or sets the unique pipeline identifier.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the pipeline name.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the pipeline description.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Gets or sets the pipeline status.
    /// </summary>
    public IPipelineStatus Status { get; set; } = PipelineStatuses.Draft;

    /// <summary>
    /// Gets or sets the task nodes in this pipeline graph.
    /// </summary>
    public IList<TaskPayload> Tasks { get; set; } = new List<TaskPayload>();

    /// <summary>
    /// Gets or sets the connections between task nodes.
    /// </summary>
    public IList<TaskConnectionPayload> Connections { get; set; } = new List<TaskConnectionPayload>();

    /// <summary>
    /// Gets or sets when the pipeline was created.
    /// </summary>
    public DateTimeOffset? CreatedAt { get; set; }

    /// <summary>
    /// Gets or sets when the pipeline was last modified.
    /// </summary>
    public DateTimeOffset? ModifiedAt { get; set; }
}
