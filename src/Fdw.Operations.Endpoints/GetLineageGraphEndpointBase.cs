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
/// Abstract endpoint that returns the transitive lineage graph for a specific entity.
/// Builds a full graph from configuration data, then extracts the upstream and downstream
/// subgraph reachable from the requested entity.
/// </summary>
public abstract class GetLineageGraphEndpointBase : Endpoint<LineageGraphRequest, LineageGraphResponse>
{
    // Why: IConfigurationGateway routes directly to ConfigurationDb via configurationSchema.json.
    // Using plain IDataGateway would look for "ConfigurationDb" in the runtime DataStore table
    // (data.DataStore), where it does not exist — it is only a bootstrap connection in the JSON.
    private readonly IConfigurationGateway _configurationGateway;
    private readonly ILogger<GetLineageGraphEndpointBase> _logger;
    // Why: pipelines are a 3-level polymorphic typed-body aggregate (Pipeline -> EtlPipeline -> engine);
    // a flat QueryAll<PipelineLineageRecord> against pipe.Pipeline cannot see the engine-body linkage
    // columns (SourceDataSet/DestinationDataSet/SourceConnectionName/DestinationConnectionName/
    // IsEnabled). Resolving through the composing provider reuses the ONE-provider mechanism instead
    // of re-implementing the join here. See PipelineLineageLoader.
    private readonly PipelineServiceConfigurationProvider _pipelineProvider;

    /// <inheritdoc />
    protected GetLineageGraphEndpointBase(
        IConfigurationGateway configurationGateway,
        PipelineServiceConfigurationProvider pipelineProvider,
        ILogger<GetLineageGraphEndpointBase> logger)
    {
        _configurationGateway = configurationGateway;
        _pipelineProvider = pipelineProvider;
        _logger = logger ?? NullLogger<GetLineageGraphEndpointBase>.Instance;
    }

    /// <summary>Configures the endpoint route, policies, and OpenAPI metadata.</summary>
    public override void Configure()
    {
        Get("/lineage/{EntityType}/{EntityName}");
#if DEVELOP
        AllowAnonymous();
#else
        Policies("pipelines:read");
#endif
        Summary(s =>
        {
            s.Summary = "Get lineage graph";
            s.Description = "Returns the transitive lineage graph for a specific entity.";
        });
    }

    /// <summary>Handles the request by building the full graph and extracting the subgraph for the requested entity.</summary>
    public override async Task HandleAsync(LineageGraphRequest req, CancellationToken ct)
    {
        ApiEndpointLog.BuildingLineageGraph(_logger, req.EntityType, req.EntityName);

        var graph = await BuildFullGraph(ct).ConfigureAwait(false);

        var entryNodeId = $"{req.EntityType}_{req.EntityName}";
        var entryNode = graph.FindNode(entryNodeId);
        if (entryNode == null)
        {
            ApiEndpointLog.EntityNotFoundInLineageGraph(_logger, req.EntityType, req.EntityName);
            await Send.NotFoundAsync(ct).ConfigureAwait(false);
            return;
        }

        var response = BuildSubgraphResponse(graph, entryNodeId);

        ApiEndpointLog.LineageGraphBuilt(_logger, response.Nodes.Count, response.Edges.Count);
        await Send.OkAsync(response, ct).ConfigureAwait(false);
    }

    /// <summary>
    /// Builds the complete lineage graph from all configuration data.
    /// Loads all entities in parallel, then creates nodes and edges.
    /// </summary>
    protected virtual async Task<LineageGraph> BuildFullGraph(CancellationToken ct)
    {
        var dataSetsTask = QueryAll<DataSetRecord>("DataSet", "data", DataSetTags, ct);
        var pipelinesTask = PipelineLineageLoader.Load(_pipelineProvider, _logger, ct);
        var sourcesTask = QueryAll<DataSetSourceRecord>("DataSetSource", "data", DataSetSourceTags, ct);
        var chainsTask = QueryAll<ChainDefinitionLineageRecord>("ChainDefinition", "transform", ChainDefinitionTags, ct);
        var stepsTask = QueryAll<ChainStepLineageRecord>("ChainStep", "transform", ChainStepTags, ct);
        var stepFieldsTask = QueryAll<ChainStepSourceFieldRecord>("ChainStepSourceField", "transform", ChainStepSourceFieldTags, ct);
        var fieldMappingsTask = QueryAll<DataSetFieldMappingRecord>("DataSetFieldMapping", "data", DataSetFieldMappingTags, ct);

        await Task.WhenAll(dataSetsTask, pipelinesTask, sourcesTask, chainsTask, stepsTask, stepFieldsTask, fieldMappingsTask).ConfigureAwait(false);

        var dataSets = await dataSetsTask.ConfigureAwait(false);
        var pipelines = await pipelinesTask.ConfigureAwait(false);
        var sources = await sourcesTask.ConfigureAwait(false);
        var chains = await chainsTask.ConfigureAwait(false);
        var steps = await stepsTask.ConfigureAwait(false);
        var stepFields = await stepFieldsTask.ConfigureAwait(false);
        var fieldMappings = await fieldMappingsTask.ConfigureAwait(false);

        return BuildGraphFromRecords(dataSets, pipelines, sources, chains, steps, stepFields, fieldMappings, _logger);
    }

    /// <summary>
    /// Builds a <see cref="LineageGraph"/> from pre-queried record sets.
    /// Exposed as <c>internal static</c> so <see cref="ExpandLineageNodeEndpointBase"/> can
    /// share the assembly-private node and edge builders without duplicating them.
    /// </summary>
    internal static LineageGraph BuildGraphFromRecords(
        IReadOnlyList<DataSetRecord> dataSets,
        IReadOnlyList<PipelineLineageRecord> pipelines,
        IReadOnlyList<DataSetSourceRecord> sources,
        IReadOnlyList<ChainDefinitionLineageRecord> chains,
        IReadOnlyList<ChainStepLineageRecord> steps,
        IReadOnlyList<ChainStepSourceFieldRecord> stepFields,
        IReadOnlyList<DataSetFieldMappingRecord> fieldMappings,
        ILogger logger)
    {
        // Why: QueryAll returns every version-on-write row of data.DataSet (one per saved version, all
        // sharing one logical Id). The lineage graph is a current-state view keyed by logical Id, so
        // collapse to one record per Id first — otherwise the Id→Name ToDictionary in AddEdges throws
        // "An item with the same key has already been added" on the first DataSet that has ever been
        // updated (e.g. a compound DataSet whose sources were bound via /map), and AddNodes would emit
        // a duplicate node per historical version. All versions share the same Name, so First() is safe.
        dataSets = dataSets.GroupBy(ds => ds.Id).Select(g => g.First()).ToList();

        var graph = new LineageGraph();
        AddNodes(graph, dataSets, pipelines, sources, chains);
        AddEdges(graph, dataSets, pipelines, sources, chains, steps, stepFields, fieldMappings, logger);
        return graph;
    }

    /// <summary>Creates all nodes: DataSets, Pipelines, Connections, and Calculations.</summary>
    private static void AddNodes(
        LineageGraph graph,
        IReadOnlyList<DataSetRecord> dataSets,
        IReadOnlyList<PipelineLineageRecord> pipelines,
        IReadOnlyList<DataSetSourceRecord> sources,
        IReadOnlyList<ChainDefinitionLineageRecord> chains)
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

        var connectionNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var src in sources)
        {
            if (!string.IsNullOrEmpty(src.ConnectionName))
                connectionNames.Add(src.ConnectionName);
        }
        foreach (var p in pipelines)
        {
            if (!string.IsNullOrEmpty(p.SourceConnectionName))
                connectionNames.Add(p.SourceConnectionName);
            if (!string.IsNullOrEmpty(p.DestinationConnectionName))
                connectionNames.Add(p.DestinationConnectionName);
        }
        foreach (var connName in connectionNames)
        {
            graph.Nodes.Add(new LineageNode
            {
                Id = $"Connection_{connName}",
                Type = LineageNodeTypes.ByName("Connection"),
                Name = connName
            });
        }

        foreach (var chain in chains)
        {
            graph.Nodes.Add(new LineageNode
            {
                Id = $"Calculation_{chain.Name}",
                Type = LineageNodeTypes.ByName("Calculation"),
                Name = chain.Name,
                Description = chain.Category
            });
        }
    }

    /// <summary>Creates all edges representing data flow relationships.</summary>
    private static void AddEdges(
        LineageGraph graph,
        IReadOnlyList<DataSetRecord> dataSets,
        IReadOnlyList<PipelineLineageRecord> pipelines,
        IReadOnlyList<DataSetSourceRecord> sources,
        IReadOnlyList<ChainDefinitionLineageRecord> chains,
        IReadOnlyList<ChainStepLineageRecord> steps,
        IReadOnlyList<ChainStepSourceFieldRecord> stepFields,
        IReadOnlyList<DataSetFieldMappingRecord> fieldMappings,
        ILogger logger)
    {
        var dataSetNameById = dataSets.ToDictionary(ds => ds.Id, ds => ds.Name);
        var edgeId = 0;

        AddSourceAndPipelineEdges(graph, pipelines, sources, dataSetNameById, logger, ref edgeId);
        AddCalculationEdges(graph, sources, chains, steps, stepFields, fieldMappings, dataSetNameById, ref edgeId);
    }

    /// <summary>
    /// Creates edges for Connection→DataSet (ReadsFrom), DataSet→DataSet (DerivesFrom), DataSet→Pipeline
    /// (Consumes), Pipeline→DataSet (ProducesDataSet), Pipeline→Connection (WritesTo), and Connection→
    /// Pipeline (ReadsFrom) relationships.
    /// </summary>
    private static void AddSourceAndPipelineEdges(
        LineageGraph graph,
        IReadOnlyList<PipelineLineageRecord> pipelines,
        IReadOnlyList<DataSetSourceRecord> sources,
        Dictionary<Guid, string> dataSetNameById,
        ILogger logger,
        ref int edgeId)
    {
        var consumesCount = 0;
        var producesDataSetCount = 0;
        var writesToCount = 0;
        var readsFromCount = 0;
        var derivesFromCount = 0;
        // Why: a source DataSet can be reused by multiple sibling sources of the same owner DataSet
        // (e.g. re-mapped per field-group); dedup so DerivesFrom is emitted once per distinct pair.
        var derivesFromEdgeKeys = new HashSet<string>(StringComparer.Ordinal);

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
                readsFromCount++;
            }

            // Why: data.DataSetSource.SourceDataSetName expresses derived-DataSet lineage
            // (POST /datasets/{p}/sources/{s}/map) — never read by any graph before this fix.
            if (!string.IsNullOrEmpty(src.SourceDataSetName) &&
                dataSetNameById.TryGetValue(src.DataSetId, out var ownerName) &&
                derivesFromEdgeKeys.Add($"{src.SourceDataSetName}→{ownerName}"))
            {
                graph.Edges.Add(new LineageEdge
                {
                    Id = $"e{edgeId++}",
                    SourceId = $"DataSet_{src.SourceDataSetName}",
                    TargetId = $"DataSet_{ownerName}",
                    Type = LineageEdgeTypes.ByName("DerivesFrom")
                });
                derivesFromCount++;
            }
        }

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
                consumesCount++;
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
                producesDataSetCount++;
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
                writesToCount++;
            }

            // Why: the source connection was collected as a NODE but no edge was ever drawn to it —
            // a genuine missing edge. Populated when the pipeline's source is a Connection (ETL, not ELT).
            if (!string.IsNullOrEmpty(p.SourceConnectionName))
            {
                graph.Edges.Add(new LineageEdge
                {
                    Id = $"e{edgeId++}",
                    SourceId = $"Connection_{p.SourceConnectionName}",
                    TargetId = $"Pipeline_{p.Name}",
                    Type = LineageEdgeTypes.ByName("ReadsFrom")
                });
                readsFromCount++;
            }
        }

        ApiEndpointLog.LineageEdgesCreated(logger, consumesCount, producesDataSetCount, writesToCount, readsFromCount, derivesFromCount);
    }

    /// <summary>Creates edges linking Calculations to DataSets via ChainStep field cross-references.</summary>
    private static void AddCalculationEdges(
        LineageGraph graph,
        IReadOnlyList<DataSetSourceRecord> sources,
        IReadOnlyList<ChainDefinitionLineageRecord> chains,
        IReadOnlyList<ChainStepLineageRecord> steps,
        IReadOnlyList<ChainStepSourceFieldRecord> stepFields,
        IReadOnlyList<DataSetFieldMappingRecord> fieldMappings,
        Dictionary<Guid, string> dataSetNameById,
        ref int edgeId)
    {
        var stepsByChainId = steps.GroupBy(s => s.ChainDefinitionId).ToDictionary(g => g.Key, g => g.ToList());
        var stepFieldsByStepId = stepFields.GroupBy(sf => sf.ChainStepId).ToDictionary(g => g.Key, g => g.ToList());
        var fieldMappingsBySourceId = fieldMappings.GroupBy(fm => fm.DataSetSourceId).ToDictionary(g => g.Key, g => g.ToList());

        var fieldToDataSets = BuildFieldToDataSetLookup(sources, fieldMappingsBySourceId, dataSetNameById);
        var calcEdges = new HashSet<string>(StringComparer.Ordinal);

        foreach (var chain in chains)
        {
            if (!stepsByChainId.TryGetValue(chain.Id, out var chainSteps)) continue;

            foreach (var step in chainSteps)
            {
                if (stepFieldsByStepId.TryGetValue(step.Id, out var srcFields))
                {
                    foreach (var sf in srcFields)
                    {
                        if (!fieldToDataSets.TryGetValue(sf.FieldName, out var dsNames)) continue;
                        foreach (var dsName in dsNames)
                        {
                            var edgeKey = $"InputsFrom_DataSet_{dsName}_Calculation_{chain.Name}";
                            if (calcEdges.Add(edgeKey))
                            {
                                graph.Edges.Add(new LineageEdge
                                {
                                    Id = $"e{edgeId++}",
                                    SourceId = $"DataSet_{dsName}",
                                    TargetId = $"Calculation_{chain.Name}",
                                    Type = LineageEdgeTypes.ByName("InputsFrom")
                                });
                            }
                        }
                    }
                }

                if (!string.IsNullOrEmpty(step.TargetField) && fieldToDataSets.TryGetValue(step.TargetField, out var targetDsNames))
                {
                    foreach (var dsName in targetDsNames)
                    {
                        var edgeKey = $"Produces_Calculation_{chain.Name}_DataSet_{dsName}";
                        if (calcEdges.Add(edgeKey))
                        {
                            graph.Edges.Add(new LineageEdge
                            {
                                Id = $"e{edgeId++}",
                                SourceId = $"Calculation_{chain.Name}",
                                TargetId = $"DataSet_{dsName}",
                                Type = LineageEdgeTypes.ByName("Produces")
                            });
                        }
                    }
                }
            }
        }
    }

    /// <summary>Builds a reverse lookup from logical field names to the DataSet names that contain them.</summary>
    private static Dictionary<string, HashSet<string>> BuildFieldToDataSetLookup(
        IReadOnlyList<DataSetSourceRecord> sources,
        Dictionary<Guid, List<DataSetFieldMappingRecord>> fieldMappingsBySourceId,
        Dictionary<Guid, string> dataSetNameById)
    {
        var fieldToDataSets = new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase);
        foreach (var src in sources)
        {
            if (!dataSetNameById.TryGetValue(src.DataSetId, out var dsName)) continue;
            if (!fieldMappingsBySourceId.TryGetValue(src.Id, out var mappings)) continue;
            foreach (var m in mappings)
            {
                if (!fieldToDataSets.TryGetValue(m.LogicalFieldName, out var dsSet))
                {
                    dsSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                    fieldToDataSets[m.LogicalFieldName] = dsSet;
                }
                dsSet.Add(dsName);
            }
        }
        return fieldToDataSets;
    }

    /// <summary>Builds a response DTO from the subgraph reachable from the entry node.</summary>
    private static LineageGraphResponse BuildSubgraphResponse(LineageGraph graph, string entryNodeId)
    {
        var upstream = graph.GetUpstreamAll(entryNodeId);
        var downstream = graph.GetDownstreamAll(entryNodeId);

        var subgraphNodeIds = new HashSet<string>(StringComparer.Ordinal) { entryNodeId };
        foreach (var n in upstream) subgraphNodeIds.Add(n.Id);
        foreach (var n in downstream) subgraphNodeIds.Add(n.Id);

        var subgraphNodes = graph.Nodes.Where(n => subgraphNodeIds.Contains(n.Id)).ToList();
        var subgraphEdges = graph.Edges.Where(e =>
            subgraphNodeIds.Contains(e.SourceId) && subgraphNodeIds.Contains(e.TargetId)).ToList();

        return MapToResponse(subgraphNodes, subgraphEdges);
    }

    /// <summary>Maps LineageNode and LineageEdge collections to the response DTO.</summary>
    internal static LineageGraphResponse MapToResponse(
        IReadOnlyList<LineageNode> nodes,
        IReadOnlyList<LineageEdge> edges)
    {
        return new LineageGraphResponse
        {
            Nodes = nodes.Select(n => new LineageGraphNodeResponse
            {
                Id = n.Id,
                Label = n.Name,
                Type = n.Type.Name,
                Category = n.Description,
                Properties = n.Metadata != null
                    ? n.Metadata.Where(kv => kv.Value != null).ToDictionary(kv => kv.Key, kv => kv.Value!, StringComparer.Ordinal)
                    : new Dictionary<string, object>(StringComparer.Ordinal)
            }).ToList(),
            Edges = edges.Select(e => new LineageGraphEdgeResponse
            {
                SourceId = e.SourceId,
                TargetId = e.TargetId,
                Relation = e.Type.Name,
                Properties = new Dictionary<string, object>(StringComparer.Ordinal)
            }).ToList()
        };
    }

    private static readonly TimeSpan LineageCacheDuration = TimeSpan.FromMinutes(5);

    // Why static readonly: CA1861 — avoids allocating new string[] arrays on every call.
    private static readonly string[] DataSetTags = ["data.DataSet"];
    private static readonly string[] DataSetSourceTags = ["data.DataSetSource"];
    private static readonly string[] ChainDefinitionTags = ["transform.ChainDefinition"];
    private static readonly string[] ChainStepTags = ["transform.ChainStep"];
    private static readonly string[] ChainStepSourceFieldTags = ["transform.ChainStepSourceField"];
    private static readonly string[] DataSetFieldMappingTags = ["data.DataSetFieldMapping"];

    /// <summary>Queries all records from a configuration table, with CachePolicy metadata applied.</summary>
    /// <param name="containerName">The container (table) name.</param>
    /// <param name="pathName">The path (schema) name within the DataStore.</param>
    /// <param name="invalidationTags">Cache invalidation tags in {schema}.{table} format.</param>
    /// <param name="ct">Cancellation token.</param>
    private async Task<IReadOnlyList<T>> QueryAll<T>(
        string containerName,
        string pathName,
        string[] invalidationTags,
        CancellationToken ct) where T : class
    {
        // Why: Addressing moved off IDataCommand onto DataStoreTarget; path is passed explicitly
        // so the correct schema segment (data/pipe/transform) is preserved per-container.
        var command = new QueryCommand<T>
        {
            // Why: add CachePolicy metadata so DataGatewayService caches this result for 5 minutes.
            // Lineage graph data changes infrequently; caching removes repeated full-table scans
            // on every graph request.
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
