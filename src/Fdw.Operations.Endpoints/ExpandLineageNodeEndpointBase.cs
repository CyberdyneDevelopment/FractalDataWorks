using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FastEndpoints;
using Fdw.Commands.Data;
using Fdw.Commands.Data.Abstractions.Caching;
using Fdw.Data;
using Fdw.Data.Abstractions;
using Fdw.Data.Lineage;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Pipelines;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Fdw.Web.RestEndpoints.Logging;

namespace Fdw.Operations.Endpoints;

/// <summary>
/// Abstract endpoint that expands a single lineage node, returning its direct (one-hop)
/// upstream and downstream neighbors. Used for lazy tree expansion without loading
/// the full transitive graph.
/// </summary>
public abstract class ExpandLineageNodeEndpointBase : Endpoint<ExpandLineageNodeRequest, LineageGraphResponse>
{
    // Why: IConfigurationGateway routes directly to ConfigurationDb via configurationSchema.json.
    private readonly IConfigurationGateway _configurationGateway;
    private readonly ILogger<ExpandLineageNodeEndpointBase> _logger;
    // Why: same composing-provider mechanism as GetLineageGraphEndpointBase — see that class's
    // field comment. Pipeline linkage lives on the engine typed body, not the flat pipe.Pipeline row.
    private readonly PipelineServiceConfigurationProvider _pipelineProvider;

    /// <inheritdoc />
    protected ExpandLineageNodeEndpointBase(
        IConfigurationGateway configurationGateway,
        PipelineServiceConfigurationProvider pipelineProvider,
        ILogger<ExpandLineageNodeEndpointBase> logger)
    {
        _configurationGateway = configurationGateway;
        _pipelineProvider = pipelineProvider;
        _logger = logger ?? NullLogger<ExpandLineageNodeEndpointBase>.Instance;
    }

    /// <inheritdoc />
    public override void Configure()
    {
        Get("/etl/lineage/expand");
#if DEVELOP
        AllowAnonymous();
#else
        Policies("pipelines:read");
#endif
        Summary(s =>
        {
            s.Summary = "Expand lineage node";
            s.Description = "Returns the direct upstream and downstream neighbors of a single lineage node.";
        });
    }

    /// <inheritdoc />
    public override async Task HandleAsync(ExpandLineageNodeRequest req, CancellationToken ct)
    {
        ApiEndpointLog.BuildingLineageGraph(_logger, req.NodeType, req.NodeId);

        var graph = await BuildFullGraph(ct).ConfigureAwait(false);

        // Why: node IDs are composite keys of {NodeType}_{NodeId} matching how AddNodes creates them.
        var entryNodeId = $"{req.NodeType}_{req.NodeId}";
        var entryNode = graph.FindNode(entryNodeId);

        if (entryNode == null)
        {
            ApiEndpointLog.EntityNotFoundInLineageGraph(_logger, req.NodeType, req.NodeId);
            await Send.NotFoundAsync(ct).ConfigureAwait(false);
            return;
        }

        var directUpstream = graph.GetUpstream(entryNodeId).ToList();
        var directDownstream = graph.GetDownstream(entryNodeId).ToList();

        var neighborNodeIds = new HashSet<string>(StringComparer.Ordinal) { entryNodeId };
        foreach (var n in directUpstream) neighborNodeIds.Add(n.Id);
        foreach (var n in directDownstream) neighborNodeIds.Add(n.Id);

        var neighborNodes = graph.Nodes.Where(n => neighborNodeIds.Contains(n.Id)).ToList();
        // Why: include only edges where BOTH endpoints are in the neighbor set — avoids emitting
        // dangling edges that would confuse the UI tree renderer.
        var neighborEdges = graph.Edges
            .Where(e => neighborNodeIds.Contains(e.SourceId) && neighborNodeIds.Contains(e.TargetId))
            .ToList();

        var response = GetLineageGraphEndpointBase.MapToResponse(neighborNodes, neighborEdges);

        ApiEndpointLog.LineageGraphBuilt(_logger, response.Nodes.Count, response.Edges.Count);
        await Send.OkAsync(response, ct).ConfigureAwait(false);
    }

    // Why: QueryAll<T> is duplicated from GetLineageGraphEndpointBase because that class's
    // BuildFullGraph is `protected virtual` — not callable across class boundaries.
    // The shared node/edge building uses BuildGraphFromRecords (internal static on base class).
    private async Task<LineageGraph> BuildFullGraph(CancellationToken ct)
    {
        var dataSetsTask = QueryAll<DataSetRecord>("DataSet", "data", DataSetTags, ct);
        var pipelinesTask = PipelineLineageLoader.Load(_pipelineProvider, _logger, ct);
        var sourcesTask = QueryAll<DataSetSourceRecord>("DataSetSource", "data", DataSetSourceTags, ct);
        var chainsTask = QueryAll<ChainDefinitionLineageRecord>("ChainDefinition", "transform", ChainDefinitionTags, ct);
        var stepsTask = QueryAll<ChainStepLineageRecord>("ChainStep", "transform", ChainStepTags, ct);
        var stepFieldsTask = QueryAll<ChainStepSourceFieldRecord>("ChainStepSourceField", "transform", ChainStepSourceFieldTags, ct);
        var fieldMappingsTask = QueryAll<DataSetFieldMappingRecord>("DataSetFieldMapping", "data", DataSetFieldMappingTags, ct);

        await Task.WhenAll(dataSetsTask, pipelinesTask, sourcesTask, chainsTask, stepsTask, stepFieldsTask, fieldMappingsTask).ConfigureAwait(false);

        return GetLineageGraphEndpointBase.BuildGraphFromRecords(
            await dataSetsTask.ConfigureAwait(false),
            await pipelinesTask.ConfigureAwait(false),
            await sourcesTask.ConfigureAwait(false),
            await chainsTask.ConfigureAwait(false),
            await stepsTask.ConfigureAwait(false),
            await stepFieldsTask.ConfigureAwait(false),
            await fieldMappingsTask.ConfigureAwait(false),
            _logger);
    }

    private static readonly TimeSpan LineageCacheDuration = TimeSpan.FromMinutes(5);

    private static readonly string[] DataSetTags = ["data.DataSet"];
    private static readonly string[] DataSetSourceTags = ["data.DataSetSource"];
    private static readonly string[] ChainDefinitionTags = ["transform.ChainDefinition"];
    private static readonly string[] ChainStepTags = ["transform.ChainStep"];
    private static readonly string[] ChainStepSourceFieldTags = ["transform.ChainStepSourceField"];
    private static readonly string[] DataSetFieldMappingTags = ["data.DataSetFieldMapping"];

    private async Task<IReadOnlyList<T>> QueryAll<T>(string containerName, string pathName, string[] invalidationTags, CancellationToken ct) where T : class
    {
        // Why: Addressing moved off IDataCommand onto DataStoreTarget; path is passed explicitly
        // so the correct schema segment (data/pipe/transform) is preserved per-container.
        var command = new QueryCommand<T>
        {
            Metadata = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase)
            {
                [CachePolicy.CacheEnabledKey] = true,
                [CachePolicy.CacheDurationKey] = LineageCacheDuration,
                [CachePolicy.CacheInvalidationTagsKey] = invalidationTags
            }
        };
        var result = await _configurationGateway
            .Execute<IEnumerable<T>>(command, new DataStoreTarget("ConfigurationDb", pathName, containerName), ct)
            .ConfigureAwait(false);
        return result.IsSuccess ? result.Value?.ToList() ?? [] : [];
    }
}
