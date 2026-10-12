using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Data.DataSets.Abstractions;
using Fdw.Results;
using Fdw.Services.Authentication.Abstractions.Security;
using Fdw.Services.Data;
using Fdw.Services.Workspaces.Logging;
using Fdw.Services.Pipelines.Abstractions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Workspaces.Endpoints;

/// <summary>Finds data sets, data stores and pipelines by name, for picking the node a lineage view is centred on.</summary>
public abstract class SearchLineageEndpointBase : LineageEndpointBase<LineageSearchRequest, List<LineageSearchHitDto>>
{
    /// <summary>Initializes a new instance of the <see cref="SearchLineageEndpointBase"/> class.</summary>
    protected SearchLineageEndpointBase(
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
        "/lineage/search",
        "Search lineage nodes by name",
        "Returns the data sets, data stores and pipelines whose names contain the text (or start with it, with match=startsWith), " +
        "ignoring case, the closest names first, at most 100 (20 when take is omitted). kinds names the kinds to search and defaults " +
        "to DataSet, DataStore and Pipeline. skip pages through the matches. Names are found in the domain tables, so a search never " +
        "reads the whole map. With paged=true the answer is the paged envelope and every hit carries the path that leads to it: the " +
        "workspace the search was scoped to, when it was scoped, then the hit. Hidden objects are absent.");

    public override async Task HandleAsync(LineageSearchRequest req, CancellationToken ct)
    {
        var found = await Find(req, ct).ConfigureAwait(false);
        if (found.IsFailure || req.Paged != true)
        {
            await Reply(found.IsFailure ? found.ToNewResult<List<LineageSearchHitDto>>() : Hits(found.Value!), ct).ConfigureAwait(false);
            return;
        }

        await Send.ResultAsync(TypedResults.Json(LineageResponses.PagedHits(found.Value!))).ConfigureAwait(false);
    }

    private static IGenericResult<List<LineageSearchHitDto>> Hits(LineageSearchPage page)
        => GenericResult<List<LineageSearchHitDto>>.Success(LineageResponses.Hits(page.Items));

    private async Task<IGenericResult<LineageSearchPage>> Find(LineageSearchRequest request, CancellationToken ct)
    {
        var query = LineageRequests.Search(request, LineageLogger);
        if (query.IsFailure) return query.ToNewResult<LineageSearchPage>();

        var page = await Reader.Search(request.Workspace, query.Value!, ct).ConfigureAwait(false);
        if (page.IsFailure) return page;

        WorkspaceLineageLog.SearchAnswered(LineageLogger, query.Value!.Text, page.Value!.Items.Count);
        return page;
    }
}
