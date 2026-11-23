using System;
using System.Collections.Generic;
using System.Linq;
using Fdw.Results;
using Fdw.Services.Workspaces.Results;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Workspaces.Endpoints;

/// <summary>Turns what the map reads (nodes and edges) into the lineage graph: data sets, data stores and the steps between them.</summary>
internal static class LineageProjection
{
    private const string EnabledKey = "isEnabled";
    private const string StoreKey = "dataStore";
    private const string StoreIdKey = "dataStoreId";
    private const string PathKey = "pathName";

    /// <summary>Builds the graph from the map's nodes and edges; the workspace is the scope the map was read for, when it was scoped.</summary>
    public static IGenericResult<LineageGraph> Of(List<MapNodeDto> nodes, List<MapEdgeDto> edges, string? workspace, ILogger logger)
    {
        var traced = nodes.Where(node => node.Kind is MapKinds.DataSet or MapKinds.DataStore or MapKinds.Container or MapKinds.Pipeline).ToList();
        var graphNodes = traced.Select(node => new LineageNode(node.Id, node.Kind, node.Label, workspace, PlacementOf(node), StoreOf(node))).ToList();
        var pipelines = traced
            .Where(node => string.Equals(node.Kind, MapKinds.Pipeline, StringComparison.Ordinal))
            .Select(node => new LineagePipeline(node.Id, node.Label, EnabledOf(node), node.CanEdit))
            .ToList();
        var flows = Flows(edges, logger);
        return flows.IsFailure
            ? flows.ToNewResult<LineageGraph>()
            : GenericResult<LineageGraph>.Success(new LineageGraph(graphNodes, pipelines, flows.Value!));
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

    // Each edge gives the lineage what its relation contributes; an edge the map did not build a relation for is refused.
    private static IGenericResult<List<LineageFlow>> Flows(List<MapEdgeDto> edges, ILogger logger)
    {
        var collector = new LineageFlowCollector();
        foreach (var edge in edges)
        {
            var relation = MapRelationOptions.ByName(edge.RelationType);
            if (ReferenceEquals(relation, MapRelationOptions.NotFound))
            {
                return GenericResult<List<LineageFlow>>.Failure(
                    WorkspacesResultCodes.ByName("MapRelationUnknown"), logger, ResultDetails.Create("relation", edge.RelationType));
            }

            relation.Contribute(edge, collector);
        }

        return GenericResult<List<LineageFlow>>.Success(collector.Flows());
    }
}
