using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Data.DataSets.Abstractions;
using Fdw.Results;
using Fdw.Services.Authentication.Abstractions.Security;
using Fdw.Services.Multitenancy.Abstractions;
using Fdw.Services.Settings;
using Fdw.Services.Data;
using Fdw.Services.Tags;
using Fdw.Services.Workspaces.Logging;
using Fdw.Services.Workspaces.Results;
using Fdw.Services.Pipelines.Abstractions;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Workspaces.Endpoints;

/// <summary>Answers the groups of data sets and containers, and the lines pipelines draw between them, so a map can start above data-set level.</summary>
public abstract class GetLineageGroupsEndpointBase : LineageEndpointBase<LineageGroupsRequest, LineageGroupsResponse>
{
    private const string DataSetKind = "DataSet";
    private const string DataStoreKind = "DataStore";
    private static readonly Dictionary<Guid, IReadOnlyList<string>> NoTags = [];

    private readonly ITagService _tags;

    /// <summary>Initializes a new instance of the <see cref="GetLineageGroupsEndpointBase"/> class.</summary>
    protected GetLineageGroupsEndpointBase(
        ILogger logger,
        IDataSetConfigurationProvider dataSets,
        IDataStoreConfigurationProvider dataStores,
        IPipelineConfigurationProvider pipelines,
        IDataSetVariationConfigurationProvider variations,
        ICategoryConfigurationProvider categories,
        IWorkspaceConfigurationProvider workspaces,
        IWorkspaceAccessPolicy access,
        IFrameworkAuthorizationService authorization,
        ITagService tags)
        : base(logger, dataSets, dataStores, pipelines, variations, categories, workspaces, access, authorization)
    {
        _tags = tags;
    }

    public override void Configure() => ConfigureLineage(
        "/lineage/groups",
        "Get the groups of data sets and the lines between them",
        "groupBy is required: category groups the data sets by category (the ones under none are Uncategorised), tag puts each data set in the group " +
        "of every tag it carries (the ones with none are Untagged), so the groups of a data set with several tags overlap. The containers pipelines read or " +
        "write directly are grouped by data store and path. A line goes from one group to another wherever a pipeline reads from the first and writes to the " +
        "second; it says how many pipelines run along it and names up to the LineageLinePipelines setting of them. A pipeline whose ends sit in one group draws no line, and ends that " +
        "share one tag draw no line for that tag. category limits a grouping by category to that category and the categories under it. tag limits a grouping " +
        "by tag to the data sets that carry it, grouped by their other tags. With groupBy=tag, containerGroups is required too: storePath keeps the containers " +
        "in their data store path groups, storeTags puts a container in the group of each tag its data store carries. Only what the caller can see is counted, as on the lineage and the map.");

    public override async Task HandleAsync(LineageGroupsRequest req, CancellationToken ct)
        => await Reply(await Group(req, ct).ConfigureAwait(false), ct).ConfigureAwait(false);

    private async Task<IGenericResult<LineageGroupsResponse>> Group(LineageGroupsRequest request, CancellationToken ct)
    {
        var ask = LineageRequests.Groups(request, LineageLogger);
        if (ask.IsFailure) return ask.ToNewResult<LineageGroupsResponse>();

        var limit = LinePipelines();
        if (limit.IsFailure) return limit.ToNewResult<LineageGroupsResponse>();

        var query = await Query(ask.Value!, limit.Value, ct).ConfigureAwait(false);
        if (query.IsFailure) return query.ToNewResult<LineageGroupsResponse>();

        var graph = await Reader.Read(
            request.Workspace, User, readCategories: string.Equals(ask.Value!.GroupBy, LineageGroups.ByCategory, StringComparison.Ordinal), ct).ConfigureAwait(false);
        if (graph.IsFailure) return graph.ToNewResult<LineageGroupsResponse>();

        var answer = LineageGroups.Of(graph.Value!, query.Value!);
        WorkspaceLineageLog.GroupsBuilt(LineageLogger, ask.Value!.GroupBy, string.Concat(ask.Value!.Category, ask.Value!.Tag), answer.Groups.Count, answer.Lines.Count);
        return GenericResult<LineageGroupsResponse>.Success(answer);
    }

    // What the grouping is made from: the category's path for a category limit, or the tag of every data set (one read) for a tag grouping.
    // The number of pipelines a line names is the LineageLinePipelines setting for the caller's tenant; none is assumed, so an absent or non-positive one is refused.
    private IGenericResult<int> LinePipelines()
        => Resolve<IEffectiveSettingsProvider>().TryGetEffectiveValue<int>(SettingDefinitions.LineageLinePipelines, out var limit, Resolve<ITenantContext>().TenantId) && limit > 0
            ? GenericResult<int>.Success(limit)
            : GenericResult<int>.Failure(
                WorkspacesResultCodes.ByName("LineageSettingMissing"), LineageLogger, ResultDetails.Create("setting", SettingDefinitions.LineageLinePipelines));

    private async Task<IGenericResult<LineageGroupsQuery>> Query(LineageGroupsAsk ask, int maxPipelines, CancellationToken ct)
    {
        if (string.Equals(ask.GroupBy, LineageGroups.ByCategory, StringComparison.Ordinal))
        {
            var path = await Reader.CategoryPath(ask.Category, ct).ConfigureAwait(false);
            return path.IsFailure
                ? path.ToNewResult<LineageGroupsQuery>()
                : GenericResult<LineageGroupsQuery>.Success(new LineageGroupsQuery(ask.GroupBy, path.Value, null, NoTags, false, NoTags, maxPipelines));
        }

        if (ask.Tag is not null)
        {
            var tag = await _tags.Get(ask.Tag, ct).ConfigureAwait(false);
            if (tag.IsFailure) return tag.ToNewResult<LineageGroupsQuery>();
        }

        var tagged = await _tags.TagsByThing(DataSetKind, ct).ConfigureAwait(false);
        if (tagged.IsFailure) return tagged.ToNewResult<LineageGroupsQuery>();
        if (!ask.ContainersTakeStoreTags) return GenericResult<LineageGroupsQuery>.Success(new LineageGroupsQuery(ask.GroupBy, null, ask.Tag, tagged.Value!, false, NoTags, maxPipelines));

        var stores = await _tags.TagsByThing(DataStoreKind, ct).ConfigureAwait(false);
        return stores.IsFailure
            ? stores.ToNewResult<LineageGroupsQuery>()
            : GenericResult<LineageGroupsQuery>.Success(new LineageGroupsQuery(ask.GroupBy, null, ask.Tag, tagged.Value!, true, stores.Value!, maxPipelines));
    }
}
