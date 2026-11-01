using System.Threading;
using System.Threading.Tasks;
using Fdw.Data.DataSets.Abstractions;
using Fdw.Results;
using Fdw.Services.Data;
using Fdw.Services.Workspaces.Results;
using Fdw.Services.Pipelines.Abstractions;
using Fdw.Services.Authentication.Abstractions.Security;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Workspaces.Endpoints;

/// <summary>
/// Explodes one node: streams only that node's children and the edges that join them to it, so the client adds them to the
/// map it holds and merges by id.
/// </summary>
/// <remarks>
/// A data store's children are its containers, and a data set's are the containers it is stored in. A pipeline and a
/// container have none, and exploding one answers 200 with nothing to add. A node whose object no longer exists answers
/// 404, never an empty set, so a client can tell a vanished node from one with nothing under it. A refused explode into
/// fields answers a coded 403, never an empty set either.
/// </remarks>
public abstract class GetMapChildrenEndpointBase : MapEndpointBase<MapChildrenRequest>
{
    private const string FieldsResource = "datasets";
    private const string FieldsAction = "read";

    private readonly MapSources _sources;
    private readonly IFrameworkAuthorizationService _authorization;

    /// <inheritdoc />
    protected GetMapChildrenEndpointBase(
        ILogger<GetMapChildrenEndpointBase> logger,
        IDataSetConfigurationProvider dataSets,
        DataStoreConfigurationProvider dataStores,
        IPipelineConfigurationProvider pipelines,
        IDataSetVariationConfigurationProvider variations,
        ICategoryConfigurationProvider categories,
        IFrameworkAuthorizationService authorization)
        : base(logger)
    {
        _sources = new MapSources(dataSets, dataStores, pipelines, variations, categories);
        _authorization = authorization;
    }

    /// <inheritdoc />
    public override void Configure() => ConfigureMap(
        "/map/nodes/{NodeId}/children",
        "Explode a map node",
        "Streams only the children of one node (a data store's containers, the containers a data set is stored in) and the edges " +
        "that join them to it. The client merges by node id. skip, take and q page and search the children; kinds=Container,DataSetField " +
        "adds a data set's fields, which needs the permission to read data sets (403 MapExplodeNotPermitted otherwise). The counts and any error come last.");

    /// <inheritdoc />
    public override Task HandleAsync(MapChildrenRequest req, CancellationToken ct) => SendStream(token => Open(req, token), ct);

    private async Task<IGenericResult<MapStream>> Open(MapChildrenRequest request, CancellationToken ct)
    {
        if (!MapNodeIds.TryParse(request.NodeId, out var kind, out var id))
            return Invalid<MapStream>($"'{request.NodeId}' is not an id of a node the map can explode");

        var query = MapChildrenQuery.Parse(request, kind, MapLogger);
        if (query.IsFailure) return query.ToNewResult<MapStream>();

        if (query.Value!.DataSetFields)
        {
            var refusal = await RefuseFields(request.NodeId, ct).ConfigureAwait(false);
            if (refusal is not null) return refusal;
        }

        // An explode has no workspace scope: only the objects it adds can be edited, never a workspace.
        var editing = await MapEditing.Resolve(
            _authorization, new ClaimsPrincipalAuthenticationContext(User), mayEditWorkspace: false, ct).ConfigureAwait(false);
        if (editing.IsFailure) return editing.ToNewResult<MapStream>();

        var opened = await ChildrenMapStream.Open(request.NodeId, kind, id, _sources, editing.Value!, query.Value!, MapLogger, ct).ConfigureAwait(false);
        return opened.IsFailure
            ? opened.ToNewResult<MapStream>()
            : GenericResult<MapStream>.Success(opened.Value!);
    }

    // The map's gate lets a caller see a data set; its fields are read under the data sets permission, which that gate does not imply.
    private async Task<IGenericResult<MapStream>?> RefuseFields(string nodeId, CancellationToken ct)
    {
        var answer = await _authorization.Authorize(
            new ClaimsPrincipalAuthenticationContext(User), FieldsResource, FieldsAction, ct).ConfigureAwait(false);
        if (answer.IsFailure) return answer.ToNewResult<MapStream>();

        return answer.Value
            ? null
            : GenericResult<MapStream>.Failure(
                WorkspacesResultCodes.ByName("MapExplodeNotPermitted"),
                MapLogger,
                ResultDetails.Create(
                    "node", nodeId,
                    "children", MapKinds.DataSetField,
                    "reason", "reading a data set's fields needs the permission to read data sets"));
    }
}
