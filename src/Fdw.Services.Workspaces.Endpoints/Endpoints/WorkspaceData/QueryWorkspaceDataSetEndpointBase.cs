using System;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Data.DataSets.Abstractions;
using Fdw.Results;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Data.Endpoints;
using Fdw.Web.Http.Abstractions.EndPoints;
using Fdw.Web.RestEndpoints.Logging;
using Fdw.Web.RestEndpoints.Query;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Services.Workspaces.Endpoints;

/// <summary>
/// Queries one data set through a workspace: <c>POST /{WorkspaceName}/{DataSetName}</c>.
/// </summary>
/// <remarks>
/// <para>
/// The route a third party's API key is meant for (FDW-876). A workspace is a layer over the data
/// set: the query is the one <c>POST /data/{DataSetName}</c> runs
/// (<see cref="DataQueryEndpointBase{TRequest, TResponse}"/>), and this route decides only which data
/// set the name reaches (FDW-975). Any signed-in caller may ask; two gates decide, in order:
/// <see cref="IWorkspaceAccessPolicy.MayRead"/> (the owner, an in-effect membership whose role reads,
/// <c>workspaces:query</c>, or <c>workspaces/{id}:query</c>), then the data set must be attached to
/// this workspace as a <c>DataSet</c> resource. Membership in the workspace is the grant: it reaches
/// every data set attached to it and nothing else. There is no platform permission of its own
/// (FDW-937): the policy already answers for the platform permission, and gating on it here would shut
/// out a member who holds none -- which is exactly a vendor's key.
/// </para>
/// <para>
/// Nothing outside the grant is ever loaded or disclosed. A workspace that does not exist and one
/// the caller may not read both answer 404 <c>WorkspaceNotReadable</c>; the data set is found among
/// the variations the workspace owns and then its own attachments, so a data set outside it is never
/// read and answers 404 <c>WorkspaceDataSetNotAttached</c> whether or not it exists elsewhere.
/// </para>
/// <para>
/// The route sits at the root, not under <c>/data/</c> (FDW-969). A literal route segment beats a
/// parameter, so a workspace and data set whose names spell another two-segment POST route
/// (<c>auth/login</c>, or <c>data/{DataSetName}</c> for a workspace named <c>data</c>) are answered by
/// that route instead and cannot be queried here.
/// </para>
/// </remarks>
public abstract class QueryWorkspaceDataSetEndpointBase
    : DataQueryEndpointBase<QueryWorkspaceDataSetRequest, WorkspaceDataSetQueryResponse>
{
    private readonly IWorkspaceReadService _workspaces;

    /// <summary>Initializes a new instance of the <see cref="QueryWorkspaceDataSetEndpointBase"/> class.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="workspaces">Decides which data set a name reaches through a workspace the caller may read.</param>
    /// <param name="dataGateways">Supplies the gateway the query is dispatched through.</param>
    protected QueryWorkspaceDataSetEndpointBase(
        ILogger<QueryWorkspaceDataSetEndpointBase> logger,
        IWorkspaceReadService workspaces,
        IDataGatewayProvider dataGateways)
        : base(logger ?? NullLogger<QueryWorkspaceDataSetEndpointBase>.Instance, dataGateways)
    {
        _workspaces = workspaces ?? throw new ArgumentNullException(nameof(workspaces));
    }

    /// <inheritdoc />
    public override void Configure()
    {
        Post("/{WorkspaceName}/{DataSetName}");
        Tags("Workspaces");
        // Why a declared group: the route names no resource, only parameters, so the reference has nothing in the
        // path to list it under (FDW-1100).
        Description(x => x.WithTags("Workspaces"));
        // Why the metadata: a caller no page limit applies to is answered by streaming every row, and the buffering
        // middleware would otherwise hold that response to set a Content-Length (FDW-1209).
        Options(builder => builder.WithMetadata(StreamingResponseMetadata.Instance));
        Summary(s =>
        {
            s.Summary = "Query a data set through a workspace";
            s.Description =
                "Queries one data set attached to the workspace. The caller must own the workspace, hold an " +
                "active membership whose role reads it, or hold workspaces:query or workspaces/{id}:query. " +
                "Filter tree, sort and paging travel " +
                "in the body; take defaults to and is capped by the pagination settings. An unknown or unreadable workspace, and a data set " +
                "not attached to it, both answer 404.";
        });
    }

    /// <inheritdoc />
    /// <remarks>
    /// Which data set the name reaches is the workspace read service's decision, shared with every other
    /// way of reading through a workspace; the query that runs is the base's.
    /// </remarks>
    protected override async Task<IGenericResult<IDataSetImplementationConfiguration>> ResolveDataSet(
        QueryWorkspaceDataSetRequest req, CancellationToken ct)
    {
        var resolved = await _workspaces.DataSet(req.WorkspaceName, req.DataSetName, ct).ConfigureAwait(false);
        if (resolved.IsFailure)
        {
            EndpointLogger.AuthorizationFailed(Logger, GetType().Name, "WorkspaceDataSetNotReadable");
            return resolved.ToNewResult<IDataSetImplementationConfiguration>();
        }

        EndpointLogger.AuthorizationSucceeded(Logger, GetType().Name);
        return GenericResult<IDataSetImplementationConfiguration>.Success(resolved.Value!.ExecutionTarget);
    }

    /// <inheritdoc />
    /// <remarks>
    /// When the name is a view, the stored filter of every view in its chain, parent first (FDW-1032): the read service
    /// walks the chain and hands the filters back on the data set it resolves. A data set attached to the workspace has
    /// none, which is true of it and not a missing value. The name was resolved a moment ago for this same request, so
    /// this read repeats what the first found, from the provider's cache.
    /// </remarks>
    protected override async Task<IGenericResult<QueryFilterNode?>> ScopeFilter(QueryWorkspaceDataSetRequest req, CancellationToken ct)
    {
        var resolved = await _workspaces.DataSet(req.WorkspaceName, req.DataSetName, ct).ConfigureAwait(false);
        return resolved.IsFailure
            ? resolved.ToNewResult<QueryFilterNode?>()
            : DataSetVariationScope.Of(resolved.Value!.ViewFilters, resolved.Value.Name, Logger);
    }

    /// <inheritdoc />
    protected override WorkspaceDataSetQueryResponse NewResponse(QueryWorkspaceDataSetRequest req)
        => new() { WorkspaceName = req.WorkspaceName };

    /// <inheritdoc />
    protected override DataQueryStreamResponse NewStreamResponse(QueryWorkspaceDataSetRequest req, DataQueryStream stream)
        => new WorkspaceDataSetQueryStreamResponse(req.WorkspaceName, stream, req.Skip);
}
