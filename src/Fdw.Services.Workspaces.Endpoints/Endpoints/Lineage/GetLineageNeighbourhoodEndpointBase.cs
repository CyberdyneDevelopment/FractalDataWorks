using System.Threading;
using System.Threading.Tasks;
using Fdw.Data.DataSets.Abstractions;
using Fdw.Results;
using Fdw.Services.Authentication.Abstractions.Security;
using Fdw.Services.Data;
using Fdw.Services.Workspaces.Logging;
using Fdw.Services.Pipelines.Abstractions;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Workspaces.Endpoints;

/// <summary>Answers what feeds a node and what it feeds, out to a depth, with the pipeline on every edge that passes through one.</summary>
public abstract class GetLineageNeighbourhoodEndpointBase : LineageEndpointBase<LineageNeighbourhoodRequest, LineageNeighbourhoodResponse>
{
    /// <summary>Initializes a new instance of the <see cref="GetLineageNeighbourhoodEndpointBase"/> class.</summary>
    protected GetLineageNeighbourhoodEndpointBase(
        ILogger logger,
        IDataSetConfigurationProvider dataSets,
        DataStoreConfigurationProvider dataStores,
        IPipelineConfigurationProvider pipelines,
        IDataSetVariationConfigurationProvider variations,
        IWorkspaceConfigurationProvider workspaces,
        IWorkspaceAccessPolicy access,
        IFrameworkAuthorizationService authorization)
        : base(logger, dataSets, dataStores, pipelines, variations, workspaces, access, authorization)
    {
    }

    public override void Configure() => ConfigureLineage(
        "/lineage/neighbourhood",
        "Get the lineage neighbourhood of a node",
        "Walks from one data set, data store or pipeline toward what feeds it (up), what it feeds (down) or both, to a depth of 1 to 5, " +
        "and returns only that neighbourhood, never the whole graph. A step through a pipeline joins the data set the pipeline reads to the " +
        "one it writes and names the pipeline in its via; a pipeline that reads several inputs and writes several outputs gives a step for " +
        "each pair. The answer holds at most 500 nodes and says when it was cut short. Data stores are left out unless kinds names DataStore. " +
        "Connection is named by the console but not offered yet, and is reported in unavailableKinds. Hidden objects are absent, as on the map.");

    public override async Task HandleAsync(LineageNeighbourhoodRequest req, CancellationToken ct)
        => await Reply(await Walk(req, ct).ConfigureAwait(false), ct).ConfigureAwait(false);

    private async Task<IGenericResult<LineageNeighbourhoodResponse>> Walk(LineageNeighbourhoodRequest request, CancellationToken ct)
    {
        var query = LineageRequests.Neighbourhood(request, LineageLogger);
        if (query.IsFailure) return query.ToNewResult<LineageNeighbourhoodResponse>();

        var graph = await Reader.Read(request.Workspace, User, ct).ConfigureAwait(false);
        if (graph.IsFailure) return graph.ToNewResult<LineageNeighbourhoodResponse>();

        var focus = graph.Value!.Node(query.Value!.Node);
        return focus is null
            ? MapRequestRefusals.NodeNotFound<LineageNeighbourhoodResponse>(LineageLogger, query.Value!.Node)
            : await Answer(graph.Value!, focus, query.Value!, ct).ConfigureAwait(false);
    }

    private async Task<IGenericResult<LineageNeighbourhoodResponse>> Answer(
        LineageGraph graph, LineageNode focus, LineageNeighbourhoodQuery query, CancellationToken ct)
    {
        var walk = graph.Neighbourhood(focus.Id, query.Direction, query.Depth, query.Kinds);
        var vias = await LineageResponses.Vias(Reader, graph, walk.Flows, query.Kinds.Pipelines, ct).ConfigureAwait(false);
        if (vias.IsFailure) return vias.ToNewResult<LineageNeighbourhoodResponse>();

        WorkspaceLineageLog.NeighbourhoodWalked(
            LineageLogger, focus.Id, query.Direction.Wire, query.Depth, walk.Nodes.Count, walk.Flows.Count, walk.Truncated);
        return GenericResult<LineageNeighbourhoodResponse>.Success(LineageResponses.Neighbourhood(focus, walk, query.Kinds, vias.Value!));
    }
}
