using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Data.Abstractions;
using Fdw.Data.DataSets.Abstractions;
using Fdw.Results;
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
    private readonly bool _readCategories;
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
    /// <param name="readCategories">
    /// Whether the DataSet category tree is read and each data set node names its category path. A caller that draws no category (the
    /// lineage views) states <see langword="false"/> and does not pay for the read, nor fail because of it: the nodes then carry no category.
    /// </param>
    public OverviewMapStream(MapSelection selection, MapSources sources, MapEditing editing, ILogger logger, bool readVariations = true, bool readCategories = true)
        : base(selection.Describe(), editing)
    {
        _readCategories = readCategories;
        _selection = selection;
        _sources = sources;
        _readVariations = readVariations;
        _logger = logger;
    }

    /// <inheritdoc />
    public override async IAsyncEnumerable<MapNodeDto> Nodes([EnumeratorCancellation] CancellationToken ct = default)
    {
        WorkspaceMapLog.MapStarted(_logger, Scope.Kind);

        var variations = await ReadVariations(ct).ConfigureAwait(false);
        if (variations.IsFailure)
        {
            State.Fail(variations);
            yield break;
        }

        var counts = variations.Value!;

        await foreach (var pipeline in _selection.Pipelines(_sources, State, ct).WithCancellation(ct).ConfigureAwait(false))
        {
            if (!State.AddNode(MapNodeIds.Pipeline(pipeline.Id))) continue;

            // Why the pipeline is read whole here: a graph pipeline names its data sets in its steps, which the scope's record read leaves out.
            var whole = await _sources.Whole(pipeline, ct).ConfigureAwait(false);
            if (whole.IsFailure)
            {
                State.Fail(whole);
                yield break;
            }

            State.Pipelines.Add(LinksOf(whole.Value!));
            yield return Editing.Apply(_selection.Place(MapNodes.Pipeline(pipeline), pipeline.Id));
        }

        // The DataSet category tree is read once for the whole map, so each data set node names its category path without a read of its own.
        var categories = await ReadCategories(ct).ConfigureAwait(false);
        if (categories.IsFailure)
        {
            State.Fail(categories);
            yield break;
        }

        await foreach (var dataSet in _selection.DataSets(_sources, State, ct).WithCancellation(ct).ConfigureAwait(false))
        {
            // The node is built before the id is claimed: a data set that fails here is not left in the state as a node that was never sent.
            var node = DataSetNode(dataSet, counts, categories.Value);
            if (node.IsFailure)
            {
                State.Fail(node);
                yield break;
            }

            if (!State.AddNode(MapNodeIds.DataSet(dataSet.Id))) continue;

            State.DataSetsByName[dataSet.Name] = dataSet.Id;
            State.DataSets.Add(LinksOf(dataSet));
            yield return Editing.Apply(_selection.Place(node.Value!, dataSet.Id));
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

    // The variations of the data sets, or none when this stream is one that does not read them (the nodes then say "not read", not "has none").
    private async Task<IGenericResult<IReadOnlyDictionary<Guid, IReadOnlyList<WorkspaceMapVariationCountDto>>>> ReadVariations(CancellationToken ct)
        => _readVariations
            ? await _selection.VariationCounts(_sources, ct).ConfigureAwait(false)
            : GenericResult<IReadOnlyDictionary<Guid, IReadOnlyList<WorkspaceMapVariationCountDto>>>.Success(NoVariations);

    // The DataSet category tree, or nothing when this stream is one that does not read it (the lineage views draw no category): the nodes then
    // carry no category, which means "not read", not "sits under none".
    private async Task<IGenericResult<CategoryTree?>> ReadCategories(CancellationToken ct)
    {
        if (!_readCategories)
            return GenericResult<CategoryTree?>.Success(null);

        var tree = await _sources.DataSetCategories(_logger, ct).ConfigureAwait(false);
        return tree.IsFailure ? tree.ToNewResult<CategoryTree?>() : GenericResult<CategoryTree?>.Success(tree.Value);
    }

    // A data set that cites a category that is not in the tree is a fault in the store, and the label reports it as one.
    private IGenericResult<MapNodeDto> DataSetNode(
        IDataSetImplementationConfiguration dataSet,
        IReadOnlyDictionary<Guid, IReadOnlyList<WorkspaceMapVariationCountDto>> counts,
        CategoryTree? categories)
    {
        if (categories is null)
            return GenericResult<MapNodeDto>.Success(MapNodes.DataSet(dataSet, VariationsOf(counts, dataSet.Id), null));

        var label = CategoryLinks.Label(categories, DataSetCategoryConfiguration.ImplementationName, dataSet.CategoryId, _logger);
        return label.IsFailure || label.Value is null
            ? label.ToNewResult<MapNodeDto>()
            : GenericResult<MapNodeDto>.Success(MapNodes.DataSet(dataSet, VariationsOf(counts, dataSet.Id), label.Value.Path));
    }

    private static IReadOnlyList<WorkspaceMapVariationCountDto> VariationsOf(
        IReadOnlyDictionary<Guid, IReadOnlyList<WorkspaceMapVariationCountDto>> counts, Guid dataSetId)
        => counts.TryGetValue(dataSetId, out var found) ? found : [];

    private static MapPipelineLinks LinksOf(IPipelineImplementationConfiguration pipeline)
        => new(pipeline.Id, PipelineDataSets.Reads(pipeline), PipelineDataSets.Writes(pipeline));

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
