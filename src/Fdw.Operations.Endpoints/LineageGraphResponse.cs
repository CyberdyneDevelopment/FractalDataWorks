using System.Collections.Generic;

namespace Fdw.Operations.Endpoints;

/// <summary>
/// Response containing the lineage graph with nodes and edges.
/// </summary>
public class LineageGraphResponse
{
    /// <summary>Gets or sets the nodes in the lineage graph.</summary>
    public IList<LineageGraphNodeResponse> Nodes { get; set; } = [];

    /// <summary>Gets or sets the edges (relationships) in the lineage graph.</summary>
    public IList<LineageGraphEdgeResponse> Edges { get; set; } = [];
}
