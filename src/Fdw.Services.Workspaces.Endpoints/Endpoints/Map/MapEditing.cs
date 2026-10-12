using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.Services.Authentication.Abstractions.Security;

namespace Fdw.Services.Workspaces.Endpoints;

/// <summary>What the caller may edit on the map, decided by the permission the edit endpoints enforce.</summary>
/// <remarks>
/// <para>
/// The map writes nothing. Editing an element means going to the workbench, or opening a panel beside the map, and
/// saving there through the element's own endpoint. This says whether that is worth offering: a node's
/// <see cref="MapNodeDto.CanEdit"/> is true when the caller holds the write permission of the endpoint that edits it.
/// </para>
/// <para>
/// The question goes to the framework's own authorization (<see cref="IFrameworkAuthorizationService.Authorize"/>), the one
/// the policy handler behind every edit endpoint calls, on the permissions carried in the caller's token. The same
/// resource-and-action check means the same wildcard grants count, and nothing is looked up in the role store.
/// </para>
/// <para>
/// A kind with no entry here is not editable from the map, and says so (<see langword="false"/>): a resource of a kind
/// the map does not know has no edit endpoint to hand off to.
/// </para>
/// </remarks>
internal sealed class MapEditing
{
    private const string Write = "write";

    // The resource each kind's edit endpoint names (its ResourceName), so the permission is "{resource}:write".
    private static readonly IReadOnlyDictionary<string, string> ResourceByKind = new Dictionary<string, string>(StringComparer.Ordinal)
    {
        [MapKinds.DataSet] = "datasets",
        [MapKinds.DataStore] = "datastores",
        [MapKinds.Pipeline] = "pipelines",

        // A container is edited inside its data store.
        [MapKinds.Container] = "datastores",

        // A field is edited inside its data set.
        [MapKinds.DataSetField] = "datasets",
    };

    private readonly IReadOnlyDictionary<string, bool> _byKind;
    private readonly bool _workspace;

    private MapEditing(IReadOnlyDictionary<string, bool> byKind, bool workspace)
    {
        _byKind = byKind;
        _workspace = workspace;
    }

    /// <summary>Asks the framework's authorization once for each resource an edit endpoint names.</summary>
    /// <param name="authorization">The framework's authorization.</param>
    /// <param name="caller">The caller, with the permissions their token carries.</param>
    /// <param name="mayEditWorkspace">Whether the caller may change the workspace the map shows: its attachments and its declared joins. False outside a workspace scope.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>What the caller may edit, or the failure when the authorization could not answer.</returns>
    public static async Task<IGenericResult<MapEditing>> Resolve(
        IFrameworkAuthorizationService authorization, IAuthenticationContext caller, bool mayEditWorkspace, CancellationToken ct)
    {
        var byResource = new Dictionary<string, bool>(StringComparer.Ordinal);
        var byKind = new Dictionary<string, bool>(StringComparer.Ordinal);
        foreach (var (kind, resource) in ResourceByKind)
        {
            if (!byResource.TryGetValue(resource, out var allowed))
            {
                var answer = await authorization.Authorize(caller, resource, Write, ct).ConfigureAwait(false);
                if (answer.IsFailure) return answer.ToNewResult<MapEditing>();

                allowed = answer.Value;
                byResource[resource] = allowed;
            }

            byKind[kind] = allowed;
        }

        return GenericResult<MapEditing>.Success(new MapEditing(byKind, mayEditWorkspace));
    }

    /// <summary>Marks a node editable when the caller may edit its kind.</summary>
    /// <param name="node">The node.</param>
    public MapNodeDto Apply(MapNodeDto node)
    {
        node.CanEdit = _byKind.TryGetValue(node.Kind, out var allowed) && allowed;
        return node;
    }

    /// <summary>Marks an edge editable when it is a join the workspace declares and the caller may edit the workspace.</summary>
    /// <param name="edge">The edge.</param>
    /// <remarks>Every other edge is derived from configuration (storage, derivation, a pipeline's data sets) and is changed by editing what it joins, not by editing the edge.</remarks>
    public MapEdgeDto Apply(MapEdgeDto edge)
    {
        edge.CanEdit = _workspace && string.Equals(edge.RelationType, MapRelations.RelatesTo, StringComparison.Ordinal);
        return edge;
    }
}
