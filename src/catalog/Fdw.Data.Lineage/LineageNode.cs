using System;
using System.Collections.Generic;

namespace Fdw.Data.Lineage;

/// <summary>
/// Represents a node in the lineage graph (pipeline, dataset, or connection).
/// </summary>
public sealed class LineageNode
{
    /// <summary>
    /// Unique identifier for the node.
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Type of node.
    /// </summary>
    public ILineageNodeType Type { get; set; } = LineageNodeTypes.NotFound;

    /// <summary>
    /// Display name of the node.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Optional description.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// X coordinate in the graph for visualization.
    /// </summary>
    public double X { get; set; }

    /// <summary>
    /// Y coordinate in the graph for visualization.
    /// </summary>
    public double Y { get; set; }

    /// <summary>
    /// Current status of the node.
    /// </summary>
    public ILineageNodeStatus Status { get; set; } = LineageNodeStatuses.NotFound;

    /// <summary>
    /// Additional metadata specific to the node type.
    /// </summary>
    public IDictionary<string, object?> Metadata { get; set; } = new Dictionary<string, object?>(StringComparer.Ordinal);
}
