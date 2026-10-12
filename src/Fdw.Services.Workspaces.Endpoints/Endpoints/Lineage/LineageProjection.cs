using System;
using System.Collections.Generic;
using System.Linq;

namespace Fdw.Services.Workspaces.Endpoints;

/// <summary>Turns what the map reads (nodes and edges) into the lineage graph: data sets, data stores and the steps between them.</summary>
internal static class LineageProjection
{
    private const string EnabledKey = "isEnabled";

    /// <summary>Builds the graph from the map's nodes and edges; the workspace is the scope the map was read for, when it was scoped.</summary>
    public static LineageGraph Of(List<MapNodeDto> nodes, List<MapEdgeDto> edges, string? workspace)
    {
        var traced = nodes.Where(node => node.Kind is MapKinds.DataSet or MapKinds.DataStore or MapKinds.Pipeline).ToList();
        var graphNodes = traced.Select(node => new LineageNode(node.Id, node.Kind, node.Label, workspace)).ToList();
        var pipelines = traced
            .Where(node => node.Kind == MapKinds.Pipeline)
            .Select(node => new LineagePipeline(node.Id, node.Label, EnabledOf(node), node.CanEdit))
            .ToList();
        return new LineageGraph(graphNodes, pipelines, Flows(edges));
    }

    private static bool? EnabledOf(MapNodeDto node)
    {
        if (!node.Metadata.TryGetValue(EnabledKey, out var value)) return null;
        return value is bool enabled ? enabled : null;
    }

    // Data moves with the edge for a derived data set or a store, but against the map's own arrows: the map draws "derived
    // from" and "stored in" toward the origin, lineage runs from the origin.
    private static List<LineageFlow> Flows(List<MapEdgeDto> edges)
    {
        var flows = new List<LineageFlow>();
        var reads = new Dictionary<string, List<string>>(StringComparer.Ordinal);
        var writes = new Dictionary<string, List<string>>(StringComparer.Ordinal);
        foreach (var edge in edges)
        {
            switch (edge.RelationType)
            {
                case MapRelations.ConsumedBy:
                    Add(reads, edge.Target, edge.Source);
                    break;
                case MapRelations.Produces:
                    Add(writes, edge.Source, edge.Target);
                    break;
                case MapRelations.DerivedFrom:
                    flows.Add(new LineageFlow(edge.Target, edge.Source, LineageRelations.DerivedFrom, null));
                    break;
                case MapRelations.StoredIn:
                    flows.Add(new LineageFlow(edge.Target, edge.Source, LineageRelations.StoredIn, null));
                    break;
            }
        }

        flows.AddRange(Through(reads, writes));
        return flows;
    }

    // A pipeline that reads several inputs and writes several outputs gives a step for each input and output pair.
    private static List<LineageFlow> Through(Dictionary<string, List<string>> reads, Dictionary<string, List<string>> writes)
    {
        var flows = new List<LineageFlow>();
        foreach (var (pipelineId, inputs) in reads)
        {
            if (!writes.TryGetValue(pipelineId, out var outputs)) continue;

            foreach (var input in inputs)
            {
                foreach (var output in outputs)
                    flows.Add(new LineageFlow(input, output, LineageRelations.Pipeline, pipelineId));
            }
        }

        return flows;
    }

    private static void Add(Dictionary<string, List<string>> index, string key, string value)
    {
        if (!index.TryGetValue(key, out var values))
        {
            values = [];
            index[key] = values;
        }

        values.Add(value);
    }
}
