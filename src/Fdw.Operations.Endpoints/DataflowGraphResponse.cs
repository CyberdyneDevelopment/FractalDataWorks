using System;
using System.Collections.Generic;

namespace Fdw.Operations.Endpoints;

/// <summary>
/// Response containing the full dataflow graph.
/// </summary>
public class DataflowGraphResponse
{
    /// <summary>Gets or sets the nodes in the dataflow graph.</summary>
    public IList<DataflowNodeDto> Nodes { get; set; } = [];
    /// <summary>Gets or sets the edges (connections) in the dataflow graph.</summary>
    public IList<DataflowEdgeDto> Edges { get; set; } = [];
    /// <summary>Gets or sets the graph statistics.</summary>
    public DataflowStatsDto Stats { get; set; } = new();
}