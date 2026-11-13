using System;
using System.Collections.Generic;
using System.Linq;

namespace Fdw.Services.Workspaces.Endpoints;

/// <summary>Turns what the map reads (nodes and edges) into the lineage graph: data sets, data stores and the steps between them.</summary>
internal static class LineageProjection
{
    private const string EnabledKey = "isEnabled";
    private const string StoreKey = "dataStore";
    private const string StoreIdKey = "dataStoreId";
    private const string PathKey = "pathName";

    /// <summary>Builds the graph from the map's nodes and edges; the workspace is the scope the map was read for, when it was scoped.</summary>
    public static LineageGraph Of(List<MapNodeDto> nodes, List<MapEdgeDto> edges, string? workspace)
    {
        var traced = nodes.Where(node => node.Kind is MapKinds.DataSet or MapKinds.DataStore or MapKinds.Container or MapKinds.Pipeline).ToList();
        var graphNodes = traced.Select(node => new LineageNode(node.Id, node.Kind, node.Label, workspace, PlacementOf(node), StoreOf(node))).ToList();
        var pipelines = traced
            .Where(node => string.Equals(node.Kind, MapKinds.Pipeline, StringComparison.Ordinal))
            .Select(node => new LineagePipeline(node.Id, node.Label, EnabledOf(node), node.CanEdit))
            .ToList();
        return new LineageGraph(graphNodes, pipelines, Flows(edges));
    }

    // A data set sits in its category; a container sits in its data store and path. The path is its name within the store, which is how a pipeline names it.
    private static string? PlacementOf(MapNodeDto node)
    {
        if (string.Equals(node.Kind, MapKinds.DataSet, StringComparison.Ordinal)) return node.Category;
        if (!string.Equals(node.Kind, MapKinds.Container, StringComparison.Ordinal)) return null;
        if (!node.Metadata.TryGetValue(StoreKey, out var store) || store is not string storeName) return null;

        return node.Metadata.TryGetValue(PathKey, out var path) && path is string pathName && pathName.Length > 0
            ? storeName + "/" + pathName
            : storeName;
    }

    // A container names the data store it is in by id; no other node does.
    private static Guid? StoreOf(MapNodeDto node)
        => string.Equals(node.Kind, MapKinds.Container, StringComparison.Ordinal)
            && node.Metadata.TryGetValue(StoreIdKey, out var value)
            && value is string text
            && Guid.TryParseExact(text, "D", out var id)
                ? id
                : null;

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
                    flows.Add(new LineageFlow(edge.Target, edge.Source, LineageRelations.DerivedFrom, null, IntendedOf(edge), GraphOf(edge)));
                    break;
                case MapRelations.StoredIn:
                    flows.Add(new LineageFlow(edge.Target, edge.Source, LineageRelations.StoredIn, null, IntendedOf(edge), GraphOf(edge)));
                    break;
            }
        }

        flows.AddRange(Through(reads, writes));
        return flows;
    }

    // An edge a Graph data set draws says so in its metadata (FDW-5456).
    private static bool GraphOf(MapEdgeDto edge)
        => edge.Metadata.TryGetValue(MapEdges.GraphKey, out var value) && value is true;

    // An edge a Graph data set draws for a source it only intends to read says so in its metadata (FDW-5296).
    private static bool IntendedOf(MapEdgeDto edge)
        => edge.Metadata.TryGetValue(MapEdges.IntendedKey, out var value) && value is true;

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
