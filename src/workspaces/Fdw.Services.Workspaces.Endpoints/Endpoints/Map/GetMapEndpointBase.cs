using System.Threading;
using System.Threading.Tasks;
using Fdw.Data.DataSets.Abstractions;
using Fdw.Results;
using Fdw.Services.Data;
using Fdw.Services.Pipelines.Abstractions;
using Fdw.Services.Authentication.Abstractions.Security;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Workspaces.Endpoints;

/// <summary>
/// Streams the map: data stores, pipelines and data sets, how they relate, and the variations of each data set as tokens
/// on it, for everything the caller can see, one organization, or one workspace.
/// </summary>
/// <remarks>
/// <para>
/// This is the lineage graph and the workspace map as one map (FDW-1029). Nothing is composed whole: every member is read
/// through its domain as the caller, a page at a time, and sent before the next is composed. There is no layer of
/// connections or sources; a data store stands for them.
/// </para>
/// <para>
/// A node is drawn once, under an id built from its kind and logical id. Exploding a node (<see cref="GetMapChildrenEndpointBase"/>)
/// adds only that node's children, and the client merges by id.
/// </para>
/// </remarks>
public abstract class GetMapEndpointBase : MapEndpointBase<MapRequest>
{
    private readonly MapSources _sources;
    private readonly MapScopeSelector _scopes;
    private readonly IFrameworkAuthorizationService _authorization;

    /// <inheritdoc />
    protected GetMapEndpointBase(
        ILogger<GetMapEndpointBase> logger,
        IDataSetConfigurationProvider dataSets,
        DataStoreConfigurationProvider dataStores,
        IPipelineConfigurationProvider pipelines,
        IDataSetVariationConfigurationProvider variations,
        ICategoryConfigurationProvider categories,
        IWorkspaceConfigurationProvider workspaces,
        IWorkspaceAccessPolicy access,
        IFrameworkAuthorizationService authorization)
        : base(logger)
    {
        _sources = new MapSources(dataSets, dataStores, pipelines, variations, categories);
        _scopes = new MapScopeSelector(workspaces, access, MapLogger);
        _authorization = authorization;
    }

    /// <inheritdoc />
    public override void Configure() => ConfigureMap(
        "/map",
        "Get the map",
        "Streams data stores, pipelines and data sets as nodes, and how they relate as edges, for everything the caller can see, " +
        "one organization (workspace and organization are exclusive query parameters) or one workspace. Each object appears once; " +
        "a data set carries its variations as per-kind counts. Nodes are sent as they are read; the counts and any error come last. " +
        "With Accept: application/x-ndjson the answer is one JSON object per line instead: scope first, then node lines (with progress lines), " +
        "then edge lines, then end last, which carries the counts, any error and, when maxNodes was reached, truncated with the cap. A " +
        "stream with no end line is incomplete.");

    /// <inheritdoc />
    public override Task HandleAsync(MapRequest req, CancellationToken ct) => SendStream(token => Open(req, token), ct, req.MaxNodes);

    private async Task<IGenericResult<MapStream>> Open(MapRequest request, CancellationToken ct)
    {
        if (request.MaxNodes is < 1)
            return Invalid<MapStream>($"maxNodes {request.MaxNodes} is below 1");

        var selection = await _scopes.Select(request.Workspace, request.Organization, ct).ConfigureAwait(false);
        if (selection.IsFailure) return selection.ToNewResult<MapStream>();

        var editing = await MapEditing.Resolve(
            _authorization, new ClaimsPrincipalAuthenticationContext(User), selection.Value!.CanEditScope, ct).ConfigureAwait(false);
        return editing.IsFailure
            ? editing.ToNewResult<MapStream>()
            : GenericResult<MapStream>.Success(new OverviewMapStream(selection.Value!, _sources, editing.Value!, MapLogger));
    }
}
