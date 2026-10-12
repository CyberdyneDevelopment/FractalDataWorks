using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Data.DataSets.Abstractions;
using Fdw.Services.Workspaces.Logging;
using Fdw.Services.Etl;
using Fdw.Services.Pipelines.Abstractions;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Workspaces.Endpoints;

/// <summary>The map of a scope: its pipelines, data sets and data stores, and how they relate.</summary>
/// <remarks>
/// The nodes arrive as they are read, so a client can draw while the rest loads. The read order is fixed: the variations
/// first (one query), then pipelines, data sets and data stores, each member sent before the next is composed.
/// </remarks>
internal sealed class OverviewMapStream : MapStream
{
    private static readonly IReadOnlyDictionary<Guid, IReadOnlyList<WorkspaceMapVariationCountDto>> NoVariations =
        new Dictionary<Guid, IReadOnlyList<WorkspaceMapVariationCountDto>>();

    private readonly MapSelection _selection;
    private readonly MapSources _sources;
    private readonly bool _readVariations;
    private readonly ILogger _logger;

    /// <summary>Initializes the stream of a scope.</summary>
    /// <param name="selection">Which members the scope reads.</param>
    /// <param name="sources">The domain providers.</param>
    /// <param name="editing">What the caller may edit.</param>
    /// <param name="logger">The logger the stream reports through.</param>
    /// <param name="readVariations">
    /// Whether the data sets' variations are read and counted on their nodes. A caller that draws no variations (the lineage
    /// views) states <see langword="false"/> and does not pay for the read: the nodes then carry no variations, which means
    /// "not read", not "has none".
    /// </param>
    public OverviewMapStream(MapSelection selection, MapSources sources, MapEditing editing, ILogger logger, bool readVariations = true)
        : base(selection.Describe(), editing)
    {
        _selection = selection;
        _sources = sources;
        _readVariations = readVariations;
        _logger = logger;
    }

    /// <inheritdoc />
    public override async IAsyncEnumerable<MapNodeDto> Nodes([EnumeratorCancellation] CancellationToken ct = default)
    {
        WorkspaceMapLog.MapStarted(_logger, Scope.Kind);

        var counts = NoVariations;
        if (_readVariations)
        {
            var variations = await _selection.VariationCounts(_sources, ct).ConfigureAwait(false);
            if (variations.IsFailure)
            {
                State.Fail(variations);
                yield break;
            }

            counts = variations.Value!;
        }

        await foreach (var pipeline in _selection.Pipelines(_sources, State, ct).WithCancellation(ct).ConfigureAwait(false))
        {
            if (!State.AddNode(MapNodeIds.Pipeline(pipeline.Id))) continue;

            State.Pipelines.Add(LinksOf(pipeline));
            yield return Editing.Apply(_selection.Place(MapNodes.Pipeline(pipeline), pipeline.Id));
        }

        await foreach (var dataSet in _selection.DataSets(_sources, State, ct).WithCancellation(ct).ConfigureAwait(false))
        {
            if (!State.AddNode(MapNodeIds.DataSet(dataSet.Id))) continue;

            State.DataSetsByName[dataSet.Name] = dataSet.Id;
            State.DataSets.Add(LinksOf(dataSet));
            yield return Editing.Apply(_selection.Place(MapNodes.DataSet(dataSet, VariationsOf(counts, dataSet.Id)), dataSet.Id));
        }

        await foreach (var dataStore in _selection.DataStores(_sources, State, ct).WithCancellation(ct).ConfigureAwait(false))
        {
            if (!State.AddNode(MapNodeIds.DataStore(dataStore.Id))) continue;

            State.DataStoresByName[dataStore.Name] = dataStore.Id;
            yield return Editing.Apply(_selection.Place(MapNodes.DataStore(dataStore), dataStore.Id));
        }

        foreach (var other in _selection.UnreadNodes())
            if (State.AddNode(other.Id))
                yield return Editing.Apply(other);
    }

    /// <inheritdoc />
    public override IEnumerable<MapEdgeDto> Edges()
    {
        var drawn = MapEdges.ForDataSets(State).Concat(MapEdges.ForPipelines(State)).Concat(_selection.Declared());
        foreach (var edge in drawn)
            if (State.HasNode(edge.Source) && State.HasNode(edge.Target) && State.AddEdge(edge.Id))
                yield return Editing.Apply(edge);

        WorkspaceMapLog.MapStreamed(_logger, State.NodeCount, State.EdgeCount, State.Unresolved);
    }

    private static IReadOnlyList<WorkspaceMapVariationCountDto> VariationsOf(
        IReadOnlyDictionary<Guid, IReadOnlyList<WorkspaceMapVariationCountDto>> counts, Guid dataSetId)
        => counts.TryGetValue(dataSetId, out var found) ? found : [];

    private static MapPipelineLinks LinksOf(IPipelineImplementationConfiguration pipeline)
        => pipeline is IEtlPipelineImplementationConfiguration engine
            ? new MapPipelineLinks(pipeline.Id, engine.SourceDataSet, engine.DestinationDataSet)
            : new MapPipelineLinks(pipeline.Id, null, null);

    private static MapDataSetLinks LinksOf(IDataSetImplementationConfiguration dataSet) => new(
        dataSet.Id,
        dataSet.Sources
            .Select(source => source.DataStoreName)
            .Where(name => !string.IsNullOrEmpty(name))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList(),
        dataSet.Sources
            .Where(source => source.SourceDataSetId.HasValue)
            .Select(source => source.SourceDataSetId!.Value)
            .Distinct()
            .ToList(),
        dataSet.Sources
            .Select(source => source.SourceDataSetName)
            .Append(dataSet.SourceDataSetName)
            .Where(name => !string.IsNullOrEmpty(name))
            .Select(name => name!)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList());
}
