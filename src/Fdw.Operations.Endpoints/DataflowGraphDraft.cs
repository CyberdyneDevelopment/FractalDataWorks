using System;
using System.Collections.Generic;
using System.Linq;

namespace Fdw.Operations.Endpoints;

/// <summary>
/// A graph under construction. A node or edge already present by id is not added twice, so a data
/// set with two sources in one container draws that container once.
/// </summary>
internal sealed class DataflowGraphDraft
{
    private readonly HashSet<string> _nodeIds = new(StringComparer.Ordinal);
    private readonly HashSet<string> _edgeIds = new(StringComparer.Ordinal);

    /// <summary>The nodes added so far.</summary>
    public IList<DataflowNodeDto> Nodes { get; } = new List<DataflowNodeDto>();

    /// <summary>The edges added so far.</summary>
    public IList<DataflowEdgeDto> Edges { get; } = new List<DataflowEdgeDto>();

    /// <summary>Adds the node unless one with its id is already present.</summary>
    public void AddNode(DataflowNodeDto node)
    {
        if (_nodeIds.Add(node.Id))
            Nodes.Add(node);
    }

    /// <summary>Adds the edge unless one with its id is already present.</summary>
    public void AddEdge(DataflowEdgeDto edge)
    {
        if (_edgeIds.Add(edge.Id))
            Edges.Add(edge);
    }

    /// <summary>How many nodes of the kind the draft holds.</summary>
    public int Count(string nodeType)
        => Nodes.Count(n => string.Equals(n.NodeType, nodeType, StringComparison.Ordinal));

    /// <summary>The finished response, its edge count taken from the draft.</summary>
    public DataflowGraphResponse ToResponse(DataflowStatsDto stats)
    {
        stats.EdgeCount = Edges.Count;
        return new DataflowGraphResponse { Nodes = Nodes, Edges = Edges, Stats = stats };
    }
}
