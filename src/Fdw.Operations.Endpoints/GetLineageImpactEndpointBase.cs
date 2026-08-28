using Fdw.Services.Data.Clients.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FastEndpoints;
using Fdw.Commands.Data;
using Fdw.Data.Lineage;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Pipelines;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Fdw.Web.RestEndpoints.Logging;

namespace Fdw.Operations.Endpoints;

/// <summary>
/// Abstract endpoint that returns the downstream impact analysis for a named entity.
/// Builds a full lineage graph, then extracts all downstream DataSet nodes to compute impact.
/// </summary>
public abstract class GetLineageImpactEndpointBase : Endpoint<LineageImpactRequest, ImpactAnalysisResponse>
{
    private readonly IConfigurationGateway _configurationGateway;
    private readonly ILogger<GetLineageImpactEndpointBase> _logger;
    private readonly PipelineServiceConfigurationProvider _pipelineProvider;

    /// <inheritdoc />
    protected GetLineageImpactEndpointBase(
        IConfigurationGateway configurationGateway,
        PipelineServiceConfigurationProvider pipelineProvider,
        ILogger<GetLineageImpactEndpointBase> logger)
    {
        _configurationGateway = configurationGateway;
        _pipelineProvider = pipelineProvider;
        _logger = logger ?? NullLogger<GetLineageImpactEndpointBase>.Instance;
    }

    /// <summary>Configures the endpoint route, policies, and OpenAPI metadata.</summary>
    public override void Configure()
    {
        Get("/lineage/{EntityName}/impact");
#if DEVELOP
        AllowAnonymous();
#else
        Policies("pipelines:read");
#endif
        Summary(s =>
        {
            s.Summary = "Get lineage impact analysis";
            s.Description = "Returns downstream impact analysis for a named entity across all entity types.";
        });
    }

    /// <summary>Handles the request by building the full graph and extracting downstream impact.</summary>
    public override async Task HandleAsync(LineageImpactRequest req, CancellationToken ct)
    {
        ApiEndpointLog.BuildingLineageGraph(_logger, "Any", req.EntityName);

        var graph = await BuildFullGraph(ct).ConfigureAwait(false);
        var (entryNode, entryNodeId) = FindEntryNode(graph, req.EntityName);

        if (entryNode == null || entryNodeId == null)
        {
            ApiEndpointLog.EntityNotFoundInLineageGraph(_logger, "Any", req.EntityName);
            await Send.NotFoundAsync(ct).ConfigureAwait(false);
            return;
        }

        var response = BuildImpactResponse(graph, entryNode, entryNodeId, req.EntityName);
        await Send.OkAsync(response, ct).ConfigureAwait(false);
    }

    /// <summary>Finds the entry node by trying common entity type prefixes.</summary>
    private static (LineageNode? Node, string? NodeId) FindEntryNode(LineageGraph graph, string entityName)
    {
        var entityTypes = new[] { "DataSet", "Pipeline", "Connection", "Calculation" };
        foreach (var entityType in entityTypes)
        {
            var nodeId = $"{entityType}_{entityName}";
            var node = graph.FindNode(nodeId);
            if (node != null)
                return (node, nodeId);
        }
        return (null, null);
    }

    /// <summary>Builds the impact analysis response from downstream traversal.</summary>
    private static ImpactAnalysisResponse BuildImpactResponse(
        LineageGraph graph, LineageNode entryNode, string entryNodeId, string entityName)
    {
        var downstream = graph.GetDownstreamAll(entryNodeId);
        var directDownstream = graph.Edges
            .Where(e => string.Equals(e.SourceId, entryNodeId, StringComparison.Ordinal))
            .Select(e => e.TargetId)
            .ToHashSet(StringComparer.Ordinal);

        var impactedDataSets = downstream
            .Where(n => string.Equals(n.Type.Name, "DataSet", StringComparison.OrdinalIgnoreCase))
            .Select(n => new ImpactedDataSetResponse
            {
                DataSetName = n.Name,
                Category = n.Description,
                ImpactLevel = directDownstream.Contains($"DataSet_{n.Name}") ? "High" : "Medium",
                AffectedSourceCount = 1,
                AffectedSources = new List<string> { entryNode.Name }
            })
            .ToList();

        return new ImpactAnalysisResponse
        {
            TargetType = entryNode.Type.Name,
            TargetName = entityName,
            ImpactedDataSets = impactedDataSets,
            TotalImpactedCount = impactedDataSets.Count,
            HighImpactCount = impactedDataSets.Count(d => string.Equals(d.ImpactLevel, "High", StringComparison.Ordinal)),
            AnalyzedAt = DateTime.UtcNow
        };
    }

    /// <summary>Builds the complete lineage graph from configuration data.</summary>
    private async Task<LineageGraph> BuildFullGraph(CancellationToken ct)
    {
        var dataSetsTask = QueryAll<DataSetRecord>("DataSet", ct);
        var pipelinesTask = PipelineLineageLoader.Load(_pipelineProvider, _logger, ct);
        var sourcesTask = QueryAll<DataSetSourcePayload>("DataSetSource", ct);

        await Task.WhenAll(dataSetsTask, pipelinesTask, sourcesTask).ConfigureAwait(false);

        var dataSets = await dataSetsTask.ConfigureAwait(false);
        var pipelines = await pipelinesTask.ConfigureAwait(false);
        var sources = await sourcesTask.ConfigureAwait(false);

        var graph = new LineageGraph();
        AddNodes(graph, dataSets, pipelines, sources);
        AddEdges(graph, dataSets, pipelines, sources);
        return graph;
    }

    /// <summary>Creates all nodes: DataSets, Pipelines, and Connections.</summary>
    private static void AddNodes(
        LineageGraph graph,
        IReadOnlyList<DataSetRecord> dataSets,
        IReadOnlyList<PipelineLineageRecord> pipelines,
        IReadOnlyList<DataSetSourcePayload> sources)
    {
        foreach (var ds in dataSets)
        {
            graph.Nodes.Add(new LineageNode
            {
                Id = $"DataSet_{ds.Name}",
                Type = LineageNodeTypes.ByName("DataSet"),
                Name = ds.Name,
                Description = ds.Category
            });
        }

        foreach (var p in pipelines)
        {
            graph.Nodes.Add(new LineageNode
            {
                Id = $"Pipeline_{p.Name}",
                Type = LineageNodeTypes.ByName("Pipeline"),
                Name = p.Name,
                Description = p.ServiceOptionType
            });
        }

        var connectionNames = CollectConnectionNames(pipelines, sources);
        foreach (var connName in connectionNames)
        {
            graph.Nodes.Add(new LineageNode
            {
                Id = $"Connection_{connName}",
                Type = LineageNodeTypes.ByName("Connection"),
                Name = connName
            });
        }
    }

    /// <summary>Collects unique connection names from pipelines and sources.</summary>
    private static HashSet<string> CollectConnectionNames(
        IReadOnlyList<PipelineLineageRecord> pipelines,
        IReadOnlyList<DataSetSourcePayload> sources)
    {
        var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var src in sources)
        {
            if (!string.IsNullOrEmpty(src.ConnectionName))
                names.Add(src.ConnectionName);
        }
        foreach (var p in pipelines)
        {
            if (!string.IsNullOrEmpty(p.SourceConnectionName))
                names.Add(p.SourceConnectionName);
            if (!string.IsNullOrEmpty(p.DestinationConnectionName))
                names.Add(p.DestinationConnectionName);
        }
        return names;
    }

    /// <summary>Creates edges representing data flow relationships.</summary>
    private static void AddEdges(
        LineageGraph graph,
        IReadOnlyList<DataSetRecord> dataSets,
        IReadOnlyList<PipelineLineageRecord> pipelines,
        IReadOnlyList<DataSetSourcePayload> sources)
    {
        var dataSetNameById = dataSets.ToDictionary(ds => ds.Id, ds => ds.Name);
        var edgeId = 0;

        foreach (var src in sources)
        {
            if (!string.IsNullOrEmpty(src.ConnectionName) && dataSetNameById.TryGetValue(src.DataSetId, out var dsName))
            {
                graph.Edges.Add(new LineageEdge
                {
                    Id = $"e{edgeId++}",
                    SourceId = $"Connection_{src.ConnectionName}",
                    TargetId = $"DataSet_{dsName}",
                    Type = LineageEdgeTypes.ByName("ReadsFrom")
                });
            }
        }

        AddPipelineEdges(graph, pipelines, ref edgeId);
    }

    /// <summary>Creates edges for pipeline data flow.</summary>
    private static void AddPipelineEdges(
        LineageGraph graph,
        IReadOnlyList<PipelineLineageRecord> pipelines,
        ref int edgeId)
    {
        foreach (var p in pipelines)
        {
            if (!string.IsNullOrEmpty(p.SourceDataSet))
            {
                graph.Edges.Add(new LineageEdge
                {
                    Id = $"e{edgeId++}",
                    SourceId = $"DataSet_{p.SourceDataSet}",
                    TargetId = $"Pipeline_{p.Name}",
                    Type = LineageEdgeTypes.ByName("Consumes")
                });
            }

            if (!string.IsNullOrEmpty(p.DestinationDataSet))
            {
                graph.Edges.Add(new LineageEdge
                {
                    Id = $"e{edgeId++}",
                    SourceId = $"Pipeline_{p.Name}",
                    TargetId = $"DataSet_{p.DestinationDataSet}",
                    Type = LineageEdgeTypes.ByName("ProducesDataSet")
                });
            }

            if (!string.IsNullOrEmpty(p.DestinationConnectionName))
            {
                graph.Edges.Add(new LineageEdge
                {
                    Id = $"e{edgeId++}",
                    SourceId = $"Pipeline_{p.Name}",
                    TargetId = $"Connection_{p.DestinationConnectionName}",
                    Type = LineageEdgeTypes.ByName("WritesTo")
                });
            }

            if (!string.IsNullOrEmpty(p.SourceConnectionName))
            {
                graph.Edges.Add(new LineageEdge
                {
                    Id = $"e{edgeId++}",
                    SourceId = $"Connection_{p.SourceConnectionName}",
                    TargetId = $"Pipeline_{p.Name}",
                    Type = LineageEdgeTypes.ByName("ReadsFrom")
                });
            }
        }
    }

    /// <summary>Queries all records from a configuration table in the ConfigurationDb data schema.</summary>
    private async Task<IReadOnlyList<T>> QueryAll<T>(string containerName, CancellationToken ct) where T : class
    {
        var command = new QueryCommand<T>();
        var result = await _configurationGateway.Execute<IEnumerable<T>>(
            command, new DataStoreTarget("PlatformConfiguration", "data", containerName), ct).ConfigureAwait(false);
        return result.IsSuccess ? result.Value?.ToList() ?? [] : [];
    }
}
