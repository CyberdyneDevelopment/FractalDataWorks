using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Data.DataSets.Abstractions;
using Fdw.Results;
using Fdw.Services.Abstractions;
using Fdw.Services.Connections;
using Fdw.Services.Pipelines.Abstractions;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Workspaces.Endpoints;

/// <summary>
/// The resources one workspace holds, and the joins it declares between its data sets.
/// </summary>
/// <remarks>
/// The members are read by the logical id the workspace's attachments hold, one at a time, so opening a small
/// workspace costs a few reads however large the estate is. The variations counted on its data sets are the ones the
/// workspace owns. A resource of a kind the map does not read (a data view, a calculation) is drawn from its attachment
/// alone, under its own kind name.
/// </remarks>
internal sealed class WorkspaceMapSelection : MapSelection
{
    private readonly IWorkspaceImplementationConfiguration _workspace;
    private readonly ILogger _logger;
    private readonly bool _mayEdit;
    private readonly Dictionary<Guid, WorkspaceResourceConfiguration> _attachments = [];

    /// <summary>Initializes a selection of one workspace.</summary>
    /// <param name="workspace">The workspace, with its resources and relationships loaded.</param>
    /// <param name="logger">Records what could not be resolved.</param>
    /// <param name="mayEdit">Whether the caller may write to the workspace, as the access policy decided.</param>
    public WorkspaceMapSelection(IWorkspaceImplementationConfiguration workspace, ILogger logger, bool mayEdit)
    {
        _workspace = workspace;
        _logger = logger;
        _mayEdit = mayEdit;

        // The same object can be attached twice under different relationships; it is still one node, placed by the first.
        foreach (var resource in workspace.Resources)
            _attachments.TryAdd(resource.ResourceId, resource);
    }

    /// <inheritdoc />
    public override MapScopeDto Describe() => new() { Kind = MapScopeKinds.Workspace, Workspace = _workspace.Name, CanEdit = _mayEdit };

    /// <inheritdoc />
    public override bool CanEditScope => _mayEdit;

    /// <inheritdoc />
    public override Guid? ScopeId => _workspace.Id;

    /// <inheritdoc />
    public override IReadOnlyCollection<Guid>? Held(string kind) => Attached(kind).ToList();

    /// <inheritdoc />
    public override async IAsyncEnumerable<IPipelineImplementationConfiguration> Pipelines(
        MapSources sources, MapState state, [EnumeratorCancellation] CancellationToken ct)
    {
        foreach (var id in Attached(MapKinds.Pipeline))
        {
            var read = await sources.Pipelines.Get(id, ConfigurationLevels.RecordOnly, ct).ConfigureAwait(false);
            var pipeline = MapSources.Resolve(read, MapKinds.Pipeline, Reference(id), NamedBy(), state, _logger);
            if (pipeline is null)
            {
                if (state.Outcome.IsFailure) yield break;
                continue;
            }

            yield return pipeline;
        }
    }

    /// <inheritdoc />
    public override async IAsyncEnumerable<IDataSetImplementationConfiguration> DataSets(
        MapSources sources, MapState state, [EnumeratorCancellation] CancellationToken ct)
    {
        foreach (var id in Attached(MapKinds.DataSet))
        {
            var read = await sources.DataSets.Get(id, ConfigurationLevels.Children, ct).ConfigureAwait(false);
            var dataSet = MapSources.Resolve(read, MapKinds.DataSet, Reference(id), NamedBy(), state, _logger);
            if (dataSet is null)
            {
                if (state.Outcome.IsFailure) yield break;
                continue;
            }

            yield return dataSet;
        }
    }

    /// <inheritdoc />
    public override async IAsyncEnumerable<IDataStoreImplementationConfiguration> DataStores(
        MapSources sources, MapState state, [EnumeratorCancellation] CancellationToken ct)
    {
        foreach (var id in Attached(MapKinds.DataStore))
        {
            var read = await sources.DataStores.Get(id, ConfigurationLevels.RecordOnly, ct).ConfigureAwait(false);
            var dataStore = MapSources.Resolve(read, MapKinds.DataStore, Reference(id), NamedBy(), state, _logger);
            if (dataStore is null)
            {
                if (state.Outcome.IsFailure) yield break;
                continue;
            }

            yield return dataStore;
        }
    }

    /// <inheritdoc />
    public override async Task<IGenericResult<IReadOnlyDictionary<Guid, IReadOnlyList<WorkspaceMapVariationCountDto>>>> VariationCounts(
        MapSources sources, CancellationToken ct)
    {
        var owned = await DataSetVariationQueries.OwnedBy(sources.Variations, _workspace.Id, ct).ConfigureAwait(false);
        return owned.IsFailure
            ? owned.ToNewResult<IReadOnlyDictionary<Guid, IReadOnlyList<WorkspaceMapVariationCountDto>>>()
            : GenericResult<IReadOnlyDictionary<Guid, IReadOnlyList<WorkspaceMapVariationCountDto>>>.Success(MapVariationCounts.Of(owned.Value!));
    }

    /// <inheritdoc />
    public override MapNodeDto Place(MapNodeDto node, Guid resourceId)
    {
        if (!_attachments.TryGetValue(resourceId, out var attachment)) return node;

        node.X = attachment.X;
        node.Y = attachment.Y;
        if (!string.IsNullOrEmpty(attachment.Relationship)) node.Metadata["relationship"] = attachment.Relationship;
        return node;
    }

    /// <inheritdoc />
    public override IEnumerable<MapNodeDto> UnreadNodes()
        => _attachments.Values
            .Where(resource => !IsRead(resource.ResourceType))
            .Select(MapNodes.Attached);

    /// <inheritdoc />
    public override IEnumerable<MapEdgeDto> Declared() => _workspace.Relationships.Select(MapEdges.Declared);

    private IEnumerable<Guid> Attached(string kind)
        => _workspace.Resources
            .Where(resource => string.Equals(resource.ResourceType, kind, StringComparison.Ordinal))
            .Select(resource => resource.ResourceId)
            .Distinct();

    private static bool IsRead(string resourceType)
        => string.Equals(resourceType, MapKinds.DataSet, StringComparison.Ordinal)
           || string.Equals(resourceType, MapKinds.DataStore, StringComparison.Ordinal)
           || string.Equals(resourceType, MapKinds.Pipeline, StringComparison.Ordinal);

    private static string Reference(Guid id) => id.ToString("D", CultureInfo.InvariantCulture);

    private string NamedBy() => string.Create(CultureInfo.InvariantCulture, $"workspace '{_workspace.Name}'");
}
