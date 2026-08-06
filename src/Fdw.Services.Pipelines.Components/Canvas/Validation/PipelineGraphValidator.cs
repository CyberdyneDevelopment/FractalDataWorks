using System;
using System.Collections.Generic;
using System.Linq;
using Fdw.UI.Abstractions.Canvas;
using Fdw.UI.Abstractions.Components;

namespace Fdw.Services.Pipelines.Components.Canvas.Validation;

/// <summary>
/// Pure, render-agnostic validator for a <see cref="PipelineCanvasModel"/>.
/// </summary>
/// <remarks>
/// <para>
/// Run client-side for live UI feedback and re-run server-side before Save. Contains no Blazor or
/// ASP.NET types — safe to reference from any layer.
/// </para>
/// <para>
/// Rules enforced:
/// <list type="bullet">
/// <item>Exactly one source DataSet node (<c>DataSetRole = "Source"</c>).</item>
/// <item>Exactly one sink DataSet node (<c>DataSetRole = "Sink"</c>).</item>
/// <item>A single connected Flow path from source → (zero or more transforms) → sink.</item>
/// <item>No cycles in the Flow edge subgraph.</item>
/// <item>No orphan nodes (every non-source node reachable from the source via Flow edges).</item>
/// <item>Every Transform node has a non-empty <c>OperationType</c> metadata key.</item>
/// </list>
/// </para>
/// </remarks>
public static class PipelineGraphValidator
{
    /// <summary>
    /// Validates <paramref name="model"/> and returns a structured result.
    /// </summary>
    /// <param name="model">The pipeline canvas model to validate.</param>
    /// <returns>A <see cref="PipelineGraphValidationResult"/> listing all issues found.</returns>
    public static PipelineGraphValidationResult Validate(PipelineCanvasModel model)
    {
        var issues = new List<PipelineGraphValidationIssue>();

        var dataSetNodes = model.Nodes
            .Where(n => string.Equals(n.NodeType.Name, "DataSet", StringComparison.Ordinal))
            .ToList();

        var sourceNodes = dataSetNodes
            .Where(n => n.Metadata.TryGetValue(PipelineCanvasMetadataKeys.DataSetRole, out var role)
                        && string.Equals(role, PipelineCanvasMetadataKeys.RoleSource, StringComparison.Ordinal))
            .ToList();

        var sinkNodes = dataSetNodes
            .Where(n => n.Metadata.TryGetValue(PipelineCanvasMetadataKeys.DataSetRole, out var role)
                        && string.Equals(role, PipelineCanvasMetadataKeys.RoleSink, StringComparison.Ordinal))
            .ToList();

        // ── Rule 1: exactly one source DataSet node ───────────────────────────
        if (sourceNodes.Count == 0)
            issues.Add(new PipelineGraphValidationIssue(
                ValidationSeverities.Error,
                "The pipeline must have exactly one source DataSet node."));
        else if (sourceNodes.Count > 1)
            issues.Add(new PipelineGraphValidationIssue(
                ValidationSeverities.Error,
                $"The pipeline has {sourceNodes.Count} source DataSet nodes; exactly one is required."));

        // ── Rule 2: exactly one sink DataSet node ─────────────────────────────
        if (sinkNodes.Count == 0)
            issues.Add(new PipelineGraphValidationIssue(
                ValidationSeverities.Error,
                "The pipeline must have exactly one sink DataSet node."));
        else if (sinkNodes.Count > 1)
            issues.Add(new PipelineGraphValidationIssue(
                ValidationSeverities.Error,
                $"The pipeline has {sinkNodes.Count} sink DataSet nodes; exactly one is required."));

        // ── Rule 3: transform nodes have a resolved OperationType ─────────────
        var transformNodes = model.Nodes
            .Where(n => string.Equals(n.NodeType.Name, "Transform", StringComparison.Ordinal)
                        || string.Equals(n.NodeType.Name, "Calculation", StringComparison.Ordinal))
            .ToList();

        foreach (var t in transformNodes)
        {
            if (!t.Metadata.TryGetValue(PipelineCanvasMetadataKeys.OperationType, out var opType)
                || string.IsNullOrEmpty(opType))
            {
                issues.Add(new PipelineGraphValidationIssue(
                    ValidationSeverities.Error,
                    $"Transform node '{t.Label}' has no OperationType set.",
                    t.Id));
            }
        }

        // ── Rules 4+5: connected path and no cycles (only when source+sink present) ──
        if (sourceNodes.Count == 1 && sinkNodes.Count == 1)
        {
            var flowEdges = model.Edges
                .Where(e => string.Equals(e.EdgeType.Name, "Flow", StringComparison.Ordinal))
                .ToList();

            CheckFlowPath(model, sourceNodes[0], sinkNodes[0], flowEdges, issues);
        }

        return new PipelineGraphValidationResult(issues);
    }

    // ── Private helpers ───────────────────────────────────────────────────────

    private static void CheckFlowPath(
        PipelineCanvasModel model,
        ICanvasNode source,
        ICanvasNode sink,
        IReadOnlyList<ICanvasEdge> flowEdges,
        List<PipelineGraphValidationIssue> issues)
    {
        // Build adjacency: nodeId → [reachable nodeIds] via Flow edges
        var adj = new Dictionary<string, List<string>>(StringComparer.Ordinal);
        foreach (var node in model.Nodes)
            adj[node.Id] = [];

        foreach (var edge in flowEdges)
        {
            if (!adj.ContainsKey(edge.SourceNodeId) || !adj.ContainsKey(edge.TargetNodeId))
                continue;

            adj[edge.SourceNodeId].Add(edge.TargetNodeId);
        }

        // ── Cycle detection (DFS colouring) ───────────────────────────────────
        var colour = new Dictionary<string, int>(StringComparer.Ordinal); // 0=white,1=grey,2=black
        foreach (var nodeId in adj.Keys)
            colour[nodeId] = 0;

        var hasCycle = false;
        foreach (var nodeId in adj.Keys)
        {
            if (colour[nodeId] == 0)
                DfsCycleDetect(nodeId, adj, colour, ref hasCycle);
        }

        if (hasCycle)
        {
            issues.Add(new PipelineGraphValidationIssue(
                ValidationSeverities.Error,
                "The pipeline graph contains a cycle in the Flow edges."));
            // Why: orphan/path checks are meaningless once a cycle exists — return early.
            return;
        }

        // ── Reachability from source to sink ──────────────────────────────────
        var reachable = BfsReachable(source.Id, adj);

        if (!reachable.Contains(sink.Id))
        {
            issues.Add(new PipelineGraphValidationIssue(
                ValidationSeverities.Error,
                "The sink DataSet node is not reachable from the source DataSet node via Flow edges.",
                sink.Id));
        }

        // ── Orphan nodes (reachable from source but not on a path to the sink) ──
        CheckOrphanNodes(model, source, sink, adj, reachable, issues);
    }

    // Why: extracted from CheckFlowPath to keep that method under the FDW007 complexity threshold.
    // Flags nodes that are not on any source→sink Flow path (ignored at execution).
    private static void CheckOrphanNodes(
        PipelineCanvasModel model,
        ICanvasNode source,
        ICanvasNode sink,
        Dictionary<string, List<string>> adj,
        HashSet<string> reachableFromSource,
        List<PipelineGraphValidationIssue> issues)
    {
        var reverseAdj = new Dictionary<string, List<string>>(StringComparer.Ordinal);
        foreach (var nodeId in adj.Keys)
            reverseAdj[nodeId] = [];

        foreach (var (from, tos) in adj)
        {
            foreach (var to in tos)
                reverseAdj[to].Add(from);
        }

        var reachableFromSinkBackward = BfsReachable(sink.Id, reverseAdj);

        foreach (var node in model.Nodes)
        {
            // Skip the source itself — it is always "before" the sink.
            if (string.Equals(node.Id, source.Id, StringComparison.Ordinal))
                continue;

            if (!reachableFromSource.Contains(node.Id) || !reachableFromSinkBackward.Contains(node.Id))
            {
                issues.Add(new PipelineGraphValidationIssue(
                    ValidationSeverities.Warning,
                    $"Node '{node.Label}' is not on the source-to-sink Flow path and will be ignored during execution.",
                    node.Id));
            }
        }
    }

    private static void DfsCycleDetect(
        string nodeId,
        Dictionary<string, List<string>> adj,
        Dictionary<string, int> colour,
        ref bool hasCycle)
    {
        colour[nodeId] = 1; // grey — in current path
        foreach (var neighbour in adj[nodeId])
        {
            if (colour[neighbour] == 1)
            {
                hasCycle = true;
                return;
            }
            if (colour[neighbour] == 0)
                DfsCycleDetect(neighbour, adj, colour, ref hasCycle);
        }
        colour[nodeId] = 2; // black — fully processed
    }

    private static HashSet<string> BfsReachable(string startId, Dictionary<string, List<string>> adj)
    {
        var visited = new HashSet<string>(StringComparer.Ordinal) { startId };
        var queue = new Queue<string>();
        queue.Enqueue(startId);

        while (queue.Count > 0)
        {
            var current = queue.Dequeue();
            if (!adj.TryGetValue(current, out var neighbours))
                continue;

            foreach (var neighbour in neighbours)
            {
                if (visited.Add(neighbour))
                    queue.Enqueue(neighbour);
            }
        }

        return visited;
    }
}
