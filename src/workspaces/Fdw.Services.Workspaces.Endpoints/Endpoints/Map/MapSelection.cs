using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Data.DataSets.Abstractions;
using Fdw.Results;
using Fdw.Services.Connections;
using Fdw.Services.Pipelines.Abstractions;

namespace Fdw.Services.Workspaces.Endpoints;

/// <summary>Which members one scope of the map reads, and how it reads them.</summary>
/// <remarks>
/// Each scope is a way to pick data stores, pipelines and data sets; what is drawn from them, and how, is the same for
/// all of them. The pipelines are read first, because an organization's data sets are the ones its pipelines name.
/// </remarks>
internal abstract class MapSelection
{
    /// <summary>What the response says it covers.</summary>
    public abstract MapScopeDto Describe();

    /// <summary>Streams the pipelines in scope.</summary>
    /// <param name="sources">The domain providers.</param>
    /// <param name="state">Receives a failure or an unresolved reference.</param>
    /// <param name="ct">The cancellation token.</param>
    public abstract IAsyncEnumerable<IPipelineImplementationConfiguration> Pipelines(MapSources sources, MapState state, CancellationToken ct);

    /// <summary>Streams the data sets in scope, each with its sources read.</summary>
    /// <param name="sources">The domain providers.</param>
    /// <param name="state">Receives a failure or an unresolved reference.</param>
    /// <param name="ct">The cancellation token.</param>
    public abstract IAsyncEnumerable<IDataSetImplementationConfiguration> DataSets(MapSources sources, MapState state, CancellationToken ct);

    /// <summary>Streams the data stores in scope.</summary>
    /// <param name="sources">The domain providers.</param>
    /// <param name="state">Receives a failure or an unresolved reference.</param>
    /// <param name="ct">The cancellation token.</param>
    public abstract IAsyncEnumerable<IDataStoreImplementationConfiguration> DataStores(MapSources sources, MapState state, CancellationToken ct);

    /// <summary>Reads the variations in scope, counted by kind for each data set they vary.</summary>
    /// <param name="sources">The domain providers.</param>
    /// <param name="ct">The cancellation token.</param>
    public abstract Task<IGenericResult<IReadOnlyDictionary<Guid, IReadOnlyList<WorkspaceMapVariationCountDto>>>> VariationCounts(
        MapSources sources, CancellationToken ct);

    /// <summary>Adds what the scope knows of an object a node stands for: where a workspace placed it.</summary>
    /// <param name="node">The node.</param>
    /// <param name="resourceId">The object's logical id.</param>
    public virtual MapNodeDto Place(MapNodeDto node, Guid resourceId) => node;

    /// <summary>Whether the caller may change what the scope holds and how it is joined. Only a workspace holds anything to change.</summary>
    public virtual bool CanEditScope => false;

    /// <summary>Gets the logical id of the object the scope is, when it is one: a workspace. Nothing for a scope that is not an object.</summary>
    public virtual Guid? ScopeId => null;

    /// <summary>Gets the logical ids of the objects of a kind the scope is limited to, or nothing when it is not limited by id.</summary>
    /// <param name="kind">The kind of object: <see cref="MapKinds.DataSet"/>, <see cref="MapKinds.DataStore"/> or <see cref="MapKinds.Pipeline"/>.</param>
    /// <remarks>
    /// A workspace holds the objects it has attached, and that list is the scope: a search over it asks for those ids, one
    /// domain read each, and never scans the estate. The other scopes pick their members another way (everything, or by a
    /// pipeline's organization), so they answer nothing here.
    /// </remarks>
    public virtual IReadOnlyCollection<Guid>? Held(string kind) => null;

    /// <summary>The nodes for objects the scope holds but the map does not read.</summary>
    public virtual IEnumerable<MapNodeDto> UnreadNodes() => [];

    /// <summary>The edges the scope declares itself.</summary>
    public virtual IEnumerable<MapEdgeDto> Declared() => [];
}
