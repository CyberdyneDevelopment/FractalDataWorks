using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Data.DataSets.Abstractions;
using Fdw.Results;
using Fdw.Services.Connections;
using Fdw.Services.Workspaces.Logging;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Workspaces.Endpoints;

/// <summary>
/// Adds the containers pipelines read and write directly to the lineage read (FDW-3824), and those their SQL steps' scripts read and write (FDW-5457): a container node, and the edge that joins it to the
/// pipeline, so a step from a container to a data set or the other way is a flow like any other.
/// </summary>
/// <remarks>
/// A pipeline names a container by data store, path and name. The map holds data stores as records, without their containers, so each store
/// a pipeline names is read once with its paths and containers. A store outside the scope is left off, as the scope leaves off any
/// reference outside it; a container the store does not hold, or that more than one path of it holds when the pipeline names no path, is
/// counted as unresolved and logged, and draws nothing.
/// </remarks>
internal static class LineageContainerEnds
{
    private const int PathsAndContainers = 2;

    /// <summary>Reads the containers the pipelines of the stream name and appends their nodes and edges.</summary>
    /// <param name="state">What the stream recorded: the pipelines' links, the data stores in scope and the nodes sent. A failed read is recorded as its outcome.</param>
    /// <param name="sources">The providers.</param>
    /// <param name="nodes">The nodes sent; a container node is appended.</param>
    /// <param name="edges">The edges sent; a container's edge to its pipeline is appended.</param>
    /// <param name="logger">The logger.</param>
    /// <param name="ct">The cancellation token.</param>
    public static async Task Add(
        MapState state, MapSources sources, List<MapNodeDto> nodes, List<MapEdgeDto> edges, ILogger logger, CancellationToken ct)
    {
        var named = state.Pipelines
            .SelectMany(pipeline => pipeline.SourceContainers.Select(container => (pipeline.Id, Container: container, Reads: true))
                .Concat(pipeline.DestinationContainers.Select(container => (pipeline.Id, Container: container, Reads: false))))
            .Where(end => state.DataStoresByName.ContainsKey(end.Container.Store))
            .ToList();
        var scripts = state.Pipelines
            .SelectMany(pipeline => pipeline.Scripts.Select(script => (pipeline.Id, Script: script)))
            .Where(step => state.DataStoresByName.ContainsKey(step.Script.DataStore))
            .ToList();
        var stores = new Dictionary<string, IDataStoreImplementationConfiguration>(StringComparer.OrdinalIgnoreCase);
        foreach (var storeName in named.Select(end => end.Container.Store).Concat(scripts.Select(step => step.Script.DataStore)).Distinct(StringComparer.OrdinalIgnoreCase))
        {
            var read = await sources.DataStores.Get(storeName, PathsAndContainers, ct).ConfigureAwait(false);
            var store = MapSources.Resolve(read, MapKinds.DataStore, storeName, "a pipeline", state, logger);
            if (store is null)
            {
                if (state.Outcome.IsFailure) return;
                continue;
            }

            stores[storeName] = store;
        }

        foreach (var end in named)
            AddEnd(state, stores, end.Id, end.Container, end.Reads, nodes, edges, logger);

        // A SQL step's script names the tables it reads and writes (FDW-5457); each is an end like a container the pipeline names.
        foreach (var step in scripts.Where(step => stores.ContainsKey(step.Script.DataStore)))
        {
            foreach (var end in LineageScriptEnds.Of(step.Id, step.Script, stores[step.Script.DataStore], logger))
                AddEnd(state, stores, step.Id, end.Container, end.Reads, nodes, edges, logger);
        }
    }

    private static void AddEnd(
        MapState state,
        Dictionary<string, IDataStoreImplementationConfiguration> stores,
        Guid pipelineId,
        PipelineContainerRef reference,
        bool reads,
        List<MapNodeDto> nodes,
        List<MapEdgeDto> edges,
        ILogger logger)
    {
        if (!stores.TryGetValue(reference.Store, out var store)) return;

        var found = Find(store, reference);
        if (found is not { } container)
        {
            state.NoteUnresolved();
            WorkspaceMapLog.ReferenceUnresolved(
                logger, MapKinds.Container, reference.Name, $"a pipeline, in data store '{reference.Store}' path '{reference.Path}'");
            return;
        }

        var containerNode = MapNodeIds.Container(container.Container.Id);
        if (state.AddNode(containerNode))
            nodes.Add(MapNodes.Container(container.Container, store.Id, store.Name, container.Path));

        var pipelineNode = MapNodeIds.Pipeline(pipelineId);
        if (!state.HasNode(pipelineNode)) return;

        var edge = reads
            ? MapEdges.Between(MapRelations.ConsumedBy, containerNode, pipelineNode)
            : MapEdges.Between(MapRelations.Produces, pipelineNode, containerNode);
        if (state.AddEdge(edge.Id)) edges.Add(edge);
    }

    // A path the pipeline names narrows the search to it; with none named the container must be in exactly one path, never the first of several.
    internal static (DataPathConfiguration Path, DataContainerConfiguration Container)? Find(
        IDataStoreImplementationConfiguration store, PipelineContainerRef reference)
    {
        var matches = store.Paths
            .Where(path => string.IsNullOrEmpty(reference.Path) || string.Equals(path.Name, reference.Path, StringComparison.OrdinalIgnoreCase))
            .SelectMany(path => path.Containers
                .Where(container => string.Equals(container.Name, reference.Name, StringComparison.OrdinalIgnoreCase))
                .Select(container => (path, container)))
            .Take(2)
            .ToList();
        return matches.Count == 1 ? matches[0] : null;
    }
}
