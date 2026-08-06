namespace Fdw.Operations.Clients.Models;

using System.Collections.Generic;

/// <summary>
/// Represents a lineage graph for visualization.
/// </summary>
public sealed class LineageGraphPayload
{
    /// <summary>Gets or sets the nodes in the lineage graph.</summary>
    public IReadOnlyList<LineageNodePayload> Nodes { get; set; } = [];
    /// <summary>Gets or sets the edges in the lineage graph.</summary>
    public IReadOnlyList<LineageEdgePayload> Edges { get; set; } = [];
}
