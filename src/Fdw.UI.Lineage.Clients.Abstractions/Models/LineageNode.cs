using System;
using System.Collections.Generic;
using Fdw.UI.Pipelines.Clients.Models;

namespace Fdw.UI.Lineage.Clients.Models;

/// <summary>
/// Represents a node in the data lineage graph, such as a pipeline, data set, or connection.
/// </summary>
public sealed class LineageNode
{
    /// <summary>
    /// Gets or sets the unique identifier for this node.
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the type of entity this node represents.
    /// </summary>
    public ILineageNodeType Type { get; set; } = LineageNodeTypes.Pipeline;

    /// <summary>
    /// Gets or sets the display name of this node.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets an optional description of this node.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Gets or sets the visual position of this node in the graph layout.
    /// </summary>
    public Point Position { get; set; }

    /// <summary>
    /// Gets or sets the current operational status of this node.
    /// </summary>
    public ILineageNodeStatus Status { get; set; } = LineageNodeStatuses.Unknown;

    /// <summary>
    /// Gets or sets additional metadata associated with this node.
    /// </summary>
    public IDictionary<string, object?> Metadata { get; set; } = new Dictionary<string, object?>(StringComparer.Ordinal);
}
