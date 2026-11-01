using System;
using System.Collections.Generic;
using System.Linq;

namespace Fdw.UI.Lineage.Clients.Models;

/// <summary>
/// Represents a complete data lineage graph with nodes and directed edges.
/// </summary>
public sealed class LineageGraph
{
    /// <summary>
    /// Gets or sets the collection of nodes in the graph.
    /// </summary>
    public IList<LineageNode> Nodes { get; set; } = new List<LineageNode>();

    /// <summary>
    /// Gets or sets the collection of edges in the graph.
    /// </summary>
    public IList<LineageEdge> Edges { get; set; } = new List<LineageEdge>();

    /// <summary>
    /// Gets an empty lineage graph instance.
    /// </summary>
    public static LineageGraph Empty => new();

    /// <summary>
    /// Finds a node by its identifier.
    /// </summary>
    /// <param name="id">The node identifier to search for.</param>
    /// <returns>The matching <see cref="LineageNode"/>, or <c>null</c> if not found.</returns>
    public LineageNode? FindNode(string id) =>
        Nodes.FirstOrDefault(n => string.Equals(n.Id, id, StringComparison.Ordinal));

    /// <summary>
    /// Returns all edges connected to the specified node (as source or target).
    /// </summary>
    /// <param name="nodeId">The node identifier.</param>
    /// <returns>All edges where the node is the source or target.</returns>
    public IEnumerable<LineageEdge> GetEdgesForNode(string nodeId) =>
        Edges.Where(e =>
            string.Equals(e.SourceId, nodeId, StringComparison.Ordinal) ||
            string.Equals(e.TargetId, nodeId, StringComparison.Ordinal));

    /// <summary>
    /// Returns all upstream nodes that have edges targeting the specified node.
    /// </summary>
    /// <param name="nodeId">The node identifier.</param>
    /// <returns>The upstream nodes.</returns>
    public IEnumerable<LineageNode> GetUpstream(string nodeId) =>
        Edges.Where(e => string.Equals(e.TargetId, nodeId, StringComparison.Ordinal))
             .Select(e => FindNode(e.SourceId))
             .Where(n => n != null)!;

    /// <summary>
    /// Returns all downstream nodes that the specified node has edges targeting.
    /// </summary>
    /// <param name="nodeId">The node identifier.</param>
    /// <returns>The downstream nodes.</returns>
    public IEnumerable<LineageNode> GetDownstream(string nodeId) =>
        Edges.Where(e => string.Equals(e.SourceId, nodeId, StringComparison.Ordinal))
             .Select(e => FindNode(e.TargetId))
             .Where(n => n != null)!;
}
