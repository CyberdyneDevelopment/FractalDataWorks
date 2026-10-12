namespace Fdw.Operations.Clients.Models;

using System;
using System.Collections.Generic;

/// <summary>
/// Response from the dataflow graph endpoint.
/// </summary>
public sealed class DataflowGraphPayload
{
    /// <summary>Gets or sets the list of nodes in the graph.</summary>
    public IReadOnlyList<DataflowNodeResponse> Nodes { get; set; } = Array.Empty<DataflowNodeResponse>();
    /// <summary>Gets or sets the list of edges in the graph.</summary>
    public IReadOnlyList<DataflowEdgeResponse> Edges { get; set; } = Array.Empty<DataflowEdgeResponse>();
    /// <summary>Gets or sets summary statistics for the graph.</summary>
    public DataflowStatsResponse? Stats { get; set; }
}
