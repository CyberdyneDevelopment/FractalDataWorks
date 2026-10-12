using System.Collections.Generic;
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

/// <summary>Answers how one data set or data store reaches another, following the way data flows.</summary>
public abstract class GetLineagePathEndpointBase : LineageEndpointBase<LineagePathRequest, LineagePathResponse>
{
    /// <summary>Initializes a new instance of the <see cref="GetLineagePathEndpointBase"/> class.</summary>
    protected GetLineagePathEndpointBase(
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
        "/lineage/path",
        "Get how one node reaches another",
        "Finds the fewest steps data takes from one data set or data store to another, within maxDepth steps (1 to 8, 8 when omitted). " +
        "found is false when there is none. Each node's depth is its step count from the start; a step through a pipeline names it in its via. " +
        "Hidden objects are absent, so a path through one is not found.");

    public override async Task HandleAsync(LineagePathRequest req, CancellationToken ct)
        => await Reply(await Trace(req, ct).ConfigureAwait(false), ct).ConfigureAwait(false);

    private async Task<IGenericResult<LineagePathResponse>> Trace(LineagePathRequest request, CancellationToken ct)
    {
        var query = LineageRequests.Path(request, LineageLogger);
        if (query.IsFailure) return query.ToNewResult<LineagePathResponse>();

        var graph = await Reader.Read(request.Workspace, User, ct).ConfigureAwait(false);
        if (graph.IsFailure) return graph.ToNewResult<LineagePathResponse>();

        var missing = Missing(graph.Value!, query.Value!);
        return missing is not null
            ? MapRequestRefusals.NodeNotFound<LineagePathResponse>(LineageLogger, missing)
            : await Answer(graph.Value!, query.Value!, ct).ConfigureAwait(false);
    }

    private static string? Missing(LineageGraph graph, LineagePathQuery query)
    {
        if (graph.Node(query.From) is null) return query.From;
        return graph.Node(query.To) is null ? query.To : null;
    }

    private async Task<IGenericResult<LineagePathResponse>> Answer(LineageGraph graph, LineagePathQuery query, CancellationToken ct)
    {
        var walk = graph.Path(query.From, query.To, query.MaxDepth);
        var flows = new List<LineageFlow>();
        if (walk is not null) flows = walk.Flows;

        var vias = await LineageResponses.Vias(Reader, graph, flows, true, ct).ConfigureAwait(false);
        if (vias.IsFailure) return vias.ToNewResult<LineagePathResponse>();

        WorkspaceLineageLog.PathSearched(LineageLogger, query.From, query.To, query.MaxDepth, walk is null ? "not found" : "found");
        return GenericResult<LineagePathResponse>.Success(LineageResponses.Path(walk, query.MaxDepth, vias.Value!));
    }
}
