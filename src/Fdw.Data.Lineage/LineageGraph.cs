using System;
using System.Collections.Generic;
using System.Linq;

namespace Fdw.Data.Lineage;

/// <summary>
/// Represents the complete lineage graph with nodes and edges.
/// </summary>
public sealed class LineageGraph
{
    /// <summary>
    /// All nodes in the graph.
    /// </summary>
    public IList<LineageNode> Nodes { get; set; } = new List<LineageNode>();

    /// <summary>
    /// All edges (relationships) in the graph.
    /// </summary>
    public IList<LineageEdge> Edges { get; set; } = new List<LineageEdge>();

    /// <summary>
    /// Creates an empty graph.
    /// </summary>
    public static LineageGraph Empty => new();

    /// <summary>
    /// Finds a node by ID.
    /// </summary>
    public LineageNode? FindNode(string id) =>
        Nodes.FirstOrDefault(n => string.Equals(n.Id, id, StringComparison.Ordinal));

    /// <summary>
    /// Gets all edges connected to a node.
    /// </summary>
    public IEnumerable<LineageEdge> GetEdgesForNode(string nodeId) =>
        Edges.Where(e => string.Equals(e.SourceId, nodeId, StringComparison.Ordinal) ||
                         string.Equals(e.TargetId, nodeId, StringComparison.Ordinal));

    /// <summary>
    /// Gets direct upstream nodes (sources feeding into this node).
    /// </summary>
    public IEnumerable<LineageNode> GetUpstream(string nodeId) =>
        Edges.Where(e => string.Equals(e.TargetId, nodeId, StringComparison.Ordinal))
             .Select(e => FindNode(e.SourceId))
             .Where(n => n != null)!;

    /// <summary>
    /// Gets direct downstream nodes (nodes consuming from this node).
    /// </summary>
    public IEnumerable<LineageNode> GetDownstream(string nodeId) =>
        Edges.Where(e => string.Equals(e.SourceId, nodeId, StringComparison.Ordinal))
             .Select(e => FindNode(e.TargetId))
             .Where(n => n != null)!;

    /// <summary>
    /// Gets all upstream nodes recursively (full ancestry), with cycle detection.
    /// </summary>
    /// <param name="nodeId">The node to start from.</param>
    /// <returns>All upstream nodes in breadth-first order, excluding duplicates.</returns>
    public IReadOnlyList<LineageNode> GetUpstreamAll(string nodeId)
    {
        var visited = new HashSet<string>(StringComparer.Ordinal);
        var result = new List<LineageNode>();
        var queue = new Queue<string>();
        queue.Enqueue(nodeId);
        visited.Add(nodeId);

        while (queue.Count > 0)
        {
            var current = queue.Dequeue();
            foreach (var upstream in GetUpstream(current))
            {
                if (visited.Add(upstream.Id))
                {
                    result.Add(upstream);
                    queue.Enqueue(upstream.Id);
                }
            }
        }

        return result;
    }

    /// <summary>
    /// Gets all downstream nodes recursively (full descendants), with cycle detection.
    /// </summary>
    /// <param name="nodeId">The node to start from.</param>
    /// <returns>All downstream nodes in breadth-first order, excluding duplicates.</returns>
    public IReadOnlyList<LineageNode> GetDownstreamAll(string nodeId)
    {
        var visited = new HashSet<string>(StringComparer.Ordinal);
        var result = new List<LineageNode>();
        var queue = new Queue<string>();
        queue.Enqueue(nodeId);
        visited.Add(nodeId);

        while (queue.Count > 0)
        {
            var current = queue.Dequeue();
            foreach (var downstream in GetDownstream(current))
            {
                if (visited.Add(downstream.Id))
                {
                    result.Add(downstream);
                    queue.Enqueue(downstream.Id);
                }
            }
        }

        return result;
    }

    /// <summary>
    /// Performs impact analysis: given a node that changed, returns all downstream nodes affected.
    /// </summary>
    /// <param name="nodeId">The node that changed.</param>
    /// <returns>All nodes downstream of the changed node (i.e., nodes that depend on it).</returns>
    public IReadOnlyList<LineageNode> GetImpact(string nodeId) => GetDownstreamAll(nodeId);

    /// <summary>
    /// Detects whether there is a cycle reachable from the given node.
    /// </summary>
    /// <param name="nodeId">The starting node ID.</param>
    /// <returns>True if a cycle exists reachable from the node; otherwise false.</returns>
    public bool HasCycle(string nodeId)
    {
        var visited = new HashSet<string>(StringComparer.Ordinal);
        var inStack = new HashSet<string>(StringComparer.Ordinal);
        return DetectCycle(nodeId, visited, inStack);
    }

    private bool DetectCycle(string nodeId, HashSet<string> visited, HashSet<string> inStack)
    {
        if (inStack.Contains(nodeId))
            return true;

        if (visited.Contains(nodeId))
            return false;

        visited.Add(nodeId);
        inStack.Add(nodeId);

        foreach (var downstream in GetDownstream(nodeId))
        {
            if (DetectCycle(downstream.Id, visited, inStack))
                return true;
        }

        inStack.Remove(nodeId);
        return false;
    }
}
