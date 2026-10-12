using System;
using System.Collections.Generic;
using System.Linq;
using Fdw.Services.Workspaces.Lineage;

namespace Fdw.Services.Workspaces.Endpoints;

/// <summary>
/// The data sets, data stores and pipelines the caller can see, and the steps data takes between them. A step through a
/// pipeline joins the data set the pipeline reads to the one it writes and names the pipeline; so a pipeline is a label on
/// an edge, not a node to walk.
/// </summary>
internal sealed class LineageGraph
{
    private readonly Dictionary<string, LineageNode> _nodes = new(StringComparer.Ordinal);
    private readonly Dictionary<string, LineagePipeline> _pipelines = new(StringComparer.Ordinal);
    private readonly List<LineageFlow> _flows = [];
    private readonly Dictionary<string, List<LineageFlow>> _into = new(StringComparer.Ordinal);
    private readonly Dictionary<string, List<LineageFlow>> _outOf = new(StringComparer.Ordinal);

    /// <summary>Initializes a new instance of the <see cref="LineageGraph"/> class; a step whose end is not a node is left out.</summary>
    public LineageGraph(List<LineageNode> nodes, List<LineagePipeline> pipelines, List<LineageFlow> flows)
    {
        foreach (var node in nodes) _nodes[node.Id] = node;
        foreach (var pipeline in pipelines) _pipelines[pipeline.Id] = pipeline;
        foreach (var flow in flows)
        {
            if (!_nodes.ContainsKey(flow.From) || !_nodes.ContainsKey(flow.To)) continue;

            _flows.Add(flow);
            Index(_outOf, flow.From, flow);
            Index(_into, flow.To, flow);
        }
    }

    /// <summary>Finds a node by id; null when the caller cannot see it, exactly as when it does not exist.</summary>
    public LineageNode? Node(string id) => _nodes.GetValueOrDefault(id);

    /// <summary>Finds a pipeline by its node id.</summary>
    public LineagePipeline? Pipeline(string id) => _pipelines.GetValueOrDefault(id);

    /// <summary>Finds the nodes of the kinds asked for whose names contain the text, the closest names first.</summary>
    public List<LineageNode> Search(string text, LineageKindSelection kinds, int take)
        => _nodes.Values
            .Where(node => kinds.Admits(node.Kind) && node.Name.Contains(text, StringComparison.OrdinalIgnoreCase))
            .OrderBy(node => Closeness(node.Name, text))
            .ThenBy(node => node.Name, StringComparer.OrdinalIgnoreCase)
            .ThenBy(node => node.Kind, StringComparer.Ordinal)
            .Take(take)
            .ToList();

    /// <summary>Walks from the focus toward what feeds it, what it feeds, or both, to the depth asked for, up to the node cap.</summary>
    public LineageWalk Neighbourhood(string focusId, ILineageDirection direction, int depth, LineageKindSelection kinds)
    {
        var traversal = new LineageTraversal(LineageLimits.MaxNodes);
        var upstream = new List<string>();
        var downstream = new List<string>();
        var firstHop = 1;

        if (_nodes[focusId].Kind == MapKinds.Pipeline)
        {
            SeedFromPipeline(traversal, focusId, direction, kinds, upstream, downstream);
            firstHop = 2;
        }
        else
        {
            traversal.TryPlace(focusId, 0);
            upstream.Add(focusId);
            downstream.Add(focusId);
        }

        if (direction.IncludesUpstream) Expand(traversal, upstream, firstHop, depth, true, kinds);
        if (direction.IncludesDownstream) Expand(traversal, downstream, firstHop, depth, false, kinds);
        return Collect(traversal);
    }

    /// <summary>Finds the fewest steps data takes from one node to another; null when there is none within the depth.</summary>
    public LineageWalk? Path(string fromId, string toId, int maxDepth)
    {
        var reachedBy = new Dictionary<string, LineageFlow?>(StringComparer.Ordinal) { [fromId] = null };
        var steps = new Dictionary<string, int>(StringComparer.Ordinal) { [fromId] = 0 };
        var queue = new Queue<string>();
        queue.Enqueue(fromId);

        while (queue.TryDequeue(out var id))
        {
            if (string.Equals(id, toId, StringComparison.Ordinal)) return Trace(toId, reachedBy, steps);
            if (steps[id] >= maxDepth) continue;

            foreach (var flow in OutOf(id))
            {
                if (!steps.TryAdd(flow.To, steps[id] + 1)) continue;

                reachedBy[flow.To] = flow;
                queue.Enqueue(flow.To);
            }
        }

        return null;
    }

    /// <summary>How close a name is to the text searched for: 0 an exact match, 1 a name that starts with it, 2 any other name that holds it.</summary>
    internal static int Closeness(string name, string text)
    {
        if (name.Equals(text, StringComparison.OrdinalIgnoreCase)) return 0;
        return name.StartsWith(text, StringComparison.OrdinalIgnoreCase) ? 1 : 2;
    }

    private static void Index(Dictionary<string, List<LineageFlow>> index, string node, LineageFlow flow)
    {
        if (!index.TryGetValue(node, out var flows))
        {
            flows = [];
            index[node] = flows;
        }

        flows.Add(flow);
    }

    private List<LineageFlow> OutOf(string id)
    {
        if (_outOf.TryGetValue(id, out var found)) return found;
        return [];
    }

    private List<LineageFlow> Into(string id)
    {
        if (_into.TryGetValue(id, out var found)) return found;
        return [];
    }

    // A pipeline is not a node: its focus is the data sets it reads (one step upstream) and writes (one step downstream).
    private void SeedFromPipeline(
        LineageTraversal traversal, string pipelineId, ILineageDirection direction, LineageKindSelection kinds, List<string> upstream, List<string> downstream)
    {
        foreach (var flow in _flows.Where(candidate => string.Equals(candidate.PipelineId, pipelineId, StringComparison.Ordinal)))
        {
            if (direction.IncludesUpstream && kinds.Admits(_nodes[flow.From].Kind) && traversal.TryPlace(flow.From, -1))
                upstream.Add(flow.From);

            if (direction.IncludesDownstream && kinds.Admits(_nodes[flow.To].Kind) && traversal.TryPlace(flow.To, 1))
                downstream.Add(flow.To);
        }
    }

    private void Expand(LineageTraversal traversal, List<string> frontier, int firstHop, int depth, bool upstream, LineageKindSelection kinds)
    {
        var current = frontier;
        for (var hop = firstHop; hop <= depth && current.Count > 0; hop++)
        {
            var next = new List<string>();
            foreach (var id in current)
            {
                foreach (var flow in upstream ? Into(id) : OutOf(id))
                {
                    var neighbour = upstream ? flow.From : flow.To;
                    if (!kinds.Admits(_nodes[neighbour].Kind)) continue;

                    var placed = traversal.TryPlace(neighbour, upstream ? -hop : hop);
                    if (traversal.Truncated) return;
                    if (placed) next.Add(neighbour);
                }
            }

            current = next;
        }
    }

    private LineageWalk Collect(LineageTraversal traversal)
    {
        var nodes = traversal.Depths
            .Select(entry => new LineagePlacedNode(_nodes[entry.Key], entry.Value))
            .OrderBy(placed => placed.Depth)
            .ThenBy(placed => placed.Node.Name, StringComparer.OrdinalIgnoreCase)
            .ThenBy(placed => placed.Node.Id, StringComparer.Ordinal)
            .ToList();
        var flows = _flows.Where(flow => traversal.Has(flow.From) && traversal.Has(flow.To)).ToList();
        return new LineageWalk(nodes, flows, traversal.Truncated);
    }

    private LineageWalk Trace(string toId, Dictionary<string, LineageFlow?> reachedBy, Dictionary<string, int> steps)
    {
        var ids = new List<string> { toId };
        var flows = new List<LineageFlow>();
        var at = toId;
        while (reachedBy[at] is { } flow)
        {
            flows.Add(flow);
            at = flow.From;
            ids.Add(at);
        }

        ids.Reverse();
        flows.Reverse();
        var nodes = ids.Select(id => new LineagePlacedNode(_nodes[id], steps[id])).ToList();
        return new LineageWalk(nodes, flows, false);
    }
}
