using System.Threading;
using System.Threading.Tasks;
using FastEndpoints;
using Fdw.Data.DataSets.Abstractions;
using Fdw.Results;
using Fdw.Services.Connections.Abstractions;
using Fdw.Services.Data;
using Fdw.Services.Pipelines.Abstractions;
using Fdw.Services.Quality;
using Fdw.Services.Scheduling.Abstractions.Configuration;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Services.Workspaces.Endpoints;

/// <summary>
/// Counts what the map holds, for everything the caller can see or one workspace, without reading the map: the first thing a
/// client with a large estate asks, so it knows how much it is about to draw before it draws any of it.
/// </summary>
/// <remarks>
/// A count that cannot be had cheaply is null and named in <c>notCounted</c>, never 0. Gated like the map: <c>pipelines:read</c>,
/// and a workspace scope is gated again by whether the caller may view that workspace.
/// </remarks>
public abstract class GetMapSummaryEndpointBase : Endpoint<MapRequest, MapSummaryResponse>
{
    private readonly MapSources _sources;
    private readonly MapKindCounts _counts;
    private readonly MapScopeSelector _scopes;
    private readonly ILogger _logger;

    /// <summary>Initializes a new instance of the <see cref="GetMapSummaryEndpointBase"/> class.</summary>
    protected GetMapSummaryEndpointBase(
        ILogger<GetMapSummaryEndpointBase> logger,
        IDataSetConfigurationProvider dataSets,
        DataStoreConfigurationProvider dataStores,
        IPipelineConfigurationProvider pipelines,
        IDataSetVariationConfigurationProvider variations,
        ICategoryConfigurationProvider categories,
        IWorkspaceConfigurationProvider workspaces,
        IWorkspaceAccessPolicy access,
        IConnectionConfigurationProvider connections,
        IQualityRuleConfigurationProvider qualityRules,
        IScheduleConfigurationProvider schedules)
    {
        _logger = logger ?? NullLogger<GetMapSummaryEndpointBase>.Instance;
        _sources = new MapSources(dataSets, dataStores, pipelines, variations, categories);
        _counts = new MapKindCounts(connections, qualityRules, schedules);
        _scopes = new MapScopeSelector(workspaces, access, _logger);
    }

    /// <inheritdoc />
    public override void Configure()
    {
        Get("/map/summary");
#if DEVELOP
        AllowAnonymous();
#else
        Policies("pipelines:read");
#endif
        Summary(s =>
        {
            s.Summary = "Count what the map holds";
            s.Description = "Counts the data sets, data stores and pipelines the map would hold for everything the caller can see or one " +
                "workspace (organization is answered with every count not given), from the domain tables, without reading the map; in the " +
                "everything scope it also counts connections, quality rules and schedules. A count that cannot be had cheaply is null and " +
                "named in notCounted, never 0.";
        });
        Description(x => x.WithTags("map"));
    }

    /// <inheritdoc />
    public override async Task HandleAsync(MapRequest req, CancellationToken ct)
    {
        var selection = await _scopes.Select(req.Workspace, req.Organization, ct).ConfigureAwait(false);
        var counted = selection.IsFailure
            ? selection.ToNewResult<MapSummaryResponse>()
            : await MapSummaryReader.Count(selection.Value!, _sources, _counts, _logger, ct).ConfigureAwait(false);
        if (counted.IsFailure)
        {
            await Send.ResultAsync(MapProblemDetails.From(counted, HttpContext)).ConfigureAwait(false);
            return;
        }

        await Send.OkAsync(counted.Value!, ct).ConfigureAwait(false);
    }
}
