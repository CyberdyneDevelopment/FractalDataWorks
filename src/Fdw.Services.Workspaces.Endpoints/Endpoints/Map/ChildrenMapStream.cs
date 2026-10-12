using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Data.DataSets.Abstractions;
using Fdw.Results;
using Fdw.Services.Abstractions;
using Fdw.Services.Connections;
using Fdw.Services.Workspaces.Logging;
using Fdw.Services.Workspaces.Results;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Workspaces.Endpoints;

/// <summary>The children of one node: what an explode adds to a map the client already holds.</summary>
/// <remarks>
/// Only the children, and the edges that join them to the node: everything else on the map stays as it is, and the client
/// merges by id. A data store's children are its containers. A data set's are the containers it is stored in. A
/// pipeline has no children, and a container has none either, so exploding one answers with nothing to add. A data set's
/// fields are children too, but only when asked for. Children come in kind then name order, and may be searched and paged. What
/// a data set derives from is not a child: the overview already draws it.
/// <para>
/// The node's own object is read before anything is sent, so a node that no longer exists answers 404, not an empty set
/// of children. A pipeline or container is not read, since nothing is drawn from it.
/// </para>
/// </remarks>
internal sealed class ChildrenMapStream : MapStream
{
    // A data store's paths are one level under it and their containers two (ConfigurationLevels).
    private const int PathsAndContainers = 2;

    private readonly string _parentNodeId;
    private readonly IDataStoreImplementationConfiguration? _dataStore;
    private readonly IDataSetImplementationConfiguration? _dataSet;
    private readonly MapSources _sources;
    private readonly ILogger _logger;
    private readonly MapChildrenQuery _query;
    private readonly List<MapEdgeDto> _edges = [];
    private MapPageDto? _page;

    private ChildrenMapStream(
        string parentNodeId,
        IDataStoreImplementationConfiguration? dataStore,
        IDataSetImplementationConfiguration? dataSet,
        MapSources sources,
        MapEditing editing,
        MapChildrenQuery query,
        ILogger logger)
        : base(new MapScopeDto { Kind = MapScopeKinds.Children, ParentNodeId = parentNodeId }, editing)
    {
        _parentNodeId = parentNodeId;
        _dataStore = dataStore;
        _dataSet = dataSet;
        _sources = sources;
        _query = query;
        _logger = logger;
    }

    /// <summary>Reads the node's own object, so the children can be streamed from it.</summary>
    /// <param name="nodeId">The node's id, as the map issued it.</param>
    /// <param name="kind">The node's kind, from its id.</param>
    /// <param name="id">The object's logical id, from the node's id.</param>
    /// <param name="sources">The domain providers.</param>
    /// <param name="editing">What the caller may edit.</param>
    /// <param name="query">Which children, which of them match and which page of those.</param>
    /// <param name="logger">The logger the stream reports through.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>The stream, a failure when the read failed, or <see cref="MapNodeNotFoundCode"/> when the object is not there.</returns>
    public static async Task<IGenericResult<ChildrenMapStream>> Open(
        string nodeId, string kind, Guid id, MapSources sources, MapEditing editing, MapChildrenQuery query, ILogger logger, CancellationToken ct)
    {
        WorkspaceMapLog.NodeExploding(logger, nodeId);

        if (string.Equals(kind, MapKinds.DataStore, StringComparison.Ordinal))
        {
            var read = await sources.DataStores.Get(id, PathsAndContainers, ct).ConfigureAwait(false);
            if (read.IsFailure) return read.ToNewResult<ChildrenMapStream>();
            return read.Value is null
                ? NotFound(nodeId, logger)
                : GenericResult<ChildrenMapStream>.Success(new ChildrenMapStream(nodeId, read.Value, null, sources, editing, query, logger));
        }

        if (string.Equals(kind, MapKinds.DataSet, StringComparison.Ordinal))
        {
            var read = await sources.DataSets.Get(id, ConfigurationLevels.Children, ct).ConfigureAwait(false);
            if (read.IsFailure) return read.ToNewResult<ChildrenMapStream>();
            return read.Value is null
                ? NotFound(nodeId, logger)
                : GenericResult<ChildrenMapStream>.Success(new ChildrenMapStream(nodeId, null, read.Value, sources, editing, query, logger));
        }

        WorkspaceMapLog.NodeHasNoChildren(logger, nodeId, kind);
        return GenericResult<ChildrenMapStream>.Success(new ChildrenMapStream(nodeId, null, null, sources, editing, query, logger));
    }

    /// <inheritdoc />
    public override MapPageDto? Page => _page;

    /// <inheritdoc />
    public override async IAsyncEnumerable<MapNodeDto> Nodes([EnumeratorCancellation] CancellationToken ct = default)
    {
        var candidates = new List<MapChild>();
        if (_query.Containers) candidates.AddRange(await ContainerChildren(ct).ConfigureAwait(false));
        if (State.Outcome.IsFailure) yield break;
        if (_query.DataSetFields && _dataSet is not null) candidates.AddRange(FieldChildren(_dataSet));

        var matching = _query.Match(candidates.DistinctBy(child => child.Node.Id, StringComparer.Ordinal));
        var sent = _query.Slice(matching);
        _page = _query.Describe(matching, sent);
        foreach (var child in sent)
        {
            _edges.Add(child.Edge);
            if (State.AddNode(child.Node.Id))
                yield return Editing.Apply(child.Node);
        }
    }

    /// <inheritdoc />
    public override IEnumerable<MapEdgeDto> Edges()
    {
        foreach (var edge in _edges)
            if (State.AddEdge(edge.Id))
                yield return Editing.Apply(edge);

        WorkspaceMapLog.NodeExploded(_logger, _parentNodeId, State.NodeCount, State.EdgeCount, State.Unresolved);
    }

    // A data store's containers, or the containers a data set is stored in: every one, before any is filtered or paged.
    private async Task<List<MapChild>> ContainerChildren(CancellationToken ct)
    {
        var children = new List<MapChild>();
        if (_dataStore is not null)
            children.AddRange(StoreContainers(_dataStore).Select(found => new MapChild(
                MapNodes.Container(found.Container, _dataStore.Id, found.Path),
                MapEdges.Between(MapRelations.PartOf, MapNodeIds.Container(found.Container.Id), _parentNodeId))));

        if (_dataSet is null) return children;

        foreach (var storeName in StoreNames(_dataSet))
        {
            var read = await _sources.DataStores.Get(storeName, PathsAndContainers, ct).ConfigureAwait(false);
            var store = MapSources.Resolve(read, MapKinds.DataStore, storeName, DataSetReference(_dataSet), State, _logger);
            if (store is null)
            {
                if (State.Outcome.IsFailure) return children;
                continue;
            }

            children.AddRange(ContainersOf(store));
        }

        return children;
    }

    // A data set's fields, each joined to the data set that holds it.
    private IEnumerable<MapChild> FieldChildren(IDataSetImplementationConfiguration dataSet)
        => dataSet.Fields.Select(field => new MapChild(
            MapNodes.DataSetField(field, dataSet.Id),
            MapEdges.Between(MapRelations.PartOf, MapNodeIds.DataSetField(field.Id), _parentNodeId)));

    // The containers of one data store that this data set's sources name, each with the edge from the data set.
    private IEnumerable<MapChild> ContainersOf(IDataStoreImplementationConfiguration store)
    {
        var inStore = StoreContainers(store).ToList();
        foreach (var source in _dataSet!.Sources.Where(source => string.Equals(source.DataStoreName, store.Name, StringComparison.OrdinalIgnoreCase)))
        {
            // A source with no container reference is stored in the data store as a whole: nothing to draw under it.
            if (source.ContainerId is null && string.IsNullOrEmpty(source.ContainerName)) continue;

            var found = inStore.FirstOrDefault(candidate => source.ContainerId is { } containerId
                ? candidate.Container.Id == containerId
                : string.Equals(candidate.Container.Name, source.ContainerName, StringComparison.OrdinalIgnoreCase));
            if (found.Container is null)
            {
                State.NoteUnresolved();
                WorkspaceMapLog.ReferenceUnresolved(
                    _logger, MapKinds.Container, ContainerReference(source), DataSetReference(_dataSet) + " in data store '" + store.Name + "'");
                continue;
            }

            yield return new MapChild(
                MapNodes.Container(found.Container, store.Id, found.Path),
                MapEdges.Between(MapRelations.StoredIn, _parentNodeId, MapNodeIds.Container(found.Container.Id)));
        }
    }

    private static IEnumerable<(DataPathConfiguration Path, DataContainerConfiguration Container)> StoreContainers(
        IDataStoreImplementationConfiguration store)
        => store.Paths.SelectMany(path => path.Containers.Select(container => (path, container)));

    private static IEnumerable<string> StoreNames(IDataSetImplementationConfiguration dataSet)
        => dataSet.Sources
            .Select(source => source.DataStoreName)
            .Where(name => !string.IsNullOrEmpty(name))
            .Distinct(StringComparer.OrdinalIgnoreCase);

    // How the source names its container: by id when it holds one, otherwise by name.
    private static string ContainerReference(DataSetSourceConfiguration source)
        => source.ContainerId is { } containerId
            ? containerId.ToString("D", System.Globalization.CultureInfo.InvariantCulture)
            : source.ContainerName;

    private static string DataSetReference(IDataSetImplementationConfiguration dataSet)
        => string.Create(System.Globalization.CultureInfo.InvariantCulture, $"data set '{dataSet.Name}'");

    private static IGenericResult<ChildrenMapStream> NotFound(string nodeId, ILogger logger)
        => GenericResult<ChildrenMapStream>.Failure(
            WorkspacesResultCodes.ByName("MapNodeNotFound"), logger, ResultDetails.Create("node", nodeId));
}
