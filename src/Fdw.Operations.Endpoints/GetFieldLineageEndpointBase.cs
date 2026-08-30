using Fdw.Data.DataSets;
using Fdw.Data.DataSets.Abstractions;
using Fdw.Services.Data.Clients.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FastEndpoints;
using Fdw.Commands.Data;
using Fdw.Data;
using Fdw.Data.Abstractions;
using Fdw.Data.Lineage;
using Fdw.Services.Data;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Pipelines;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Fdw.Web.RestEndpoints.Logging;

namespace Fdw.Operations.Endpoints;

/// <summary>
/// Abstract endpoint that returns field-level lineage for a specific field within an entity.
/// Builds a full graph to resolve the entry node, then identifies connections and calculations
/// related to the requested field.
/// </summary>
public abstract class GetFieldLineageEndpointBase : Endpoint<LineageFieldRequest, LineageGraphResponse>
{
    private readonly DataSetConfigurationProvider _dataSetProvider;
    private readonly LineageConfigurationProvider _lineageProvider;
    private readonly ILogger<GetFieldLineageEndpointBase> _logger;
    private readonly PipelineServiceConfigurationProvider _pipelineProvider;

    /// <inheritdoc />
    protected GetFieldLineageEndpointBase(
        DataSetConfigurationProvider dataSetProvider,
        LineageConfigurationProvider lineageProvider,
        PipelineServiceConfigurationProvider pipelineProvider,
        ILogger<GetFieldLineageEndpointBase> logger)
    {
        _dataSetProvider = dataSetProvider;
        _lineageProvider = lineageProvider;
        _pipelineProvider = pipelineProvider;
        _logger = logger ?? NullLogger<GetFieldLineageEndpointBase>.Instance;
    }

    /// <summary>Configures the endpoint route, policies, and OpenAPI metadata.</summary>
    public override void Configure()
    {
        Get("/lineage/{EntityType}/{EntityName}/fields/{FieldName}");
#if DEVELOP
        AllowAnonymous();
#else
        Policies("pipelines:read");
#endif
        Summary(s =>
        {
            s.Summary = "Get field-level lineage";
            s.Description = "Returns field-level lineage for a specific field within an entity.";
        });
    }

    /// <summary>Handles the request by building a field-focused subgraph.</summary>
    public override async Task HandleAsync(LineageFieldRequest req, CancellationToken ct)
    {
        ApiEndpointLog.BuildingFieldLineage(_logger, req.EntityType, req.EntityName, req.FieldName);

        var fullGraph = await BuildFullGraph(ct).ConfigureAwait(false);

        var entryNodeId = $"{req.EntityType}_{req.EntityName}";
        var entryNode = fullGraph.FindNode(entryNodeId);
        if (entryNode == null)
        {
            ApiEndpointLog.EntityNotFoundInLineageGraph(_logger, req.EntityType, req.EntityName);
            await Send.NotFoundAsync(ct).ConfigureAwait(false);
            return;
        }

        var fieldGraph = await BuildFieldSubgraph(fullGraph, entryNode, entryNodeId, req, ct).ConfigureAwait(false);

        var response = GetLineageGraphEndpointBase.MapToResponse(
            fieldGraph.Nodes.ToList(),
            fieldGraph.Edges.ToList());

        ApiEndpointLog.LineageGraphBuilt(_logger, response.Nodes.Count, response.Edges.Count);
        await Send.OkAsync(response, ct).ConfigureAwait(false);
    }

    /// <summary>Builds a field-focused subgraph containing connections and calculations related to the requested field.</summary>
    private async Task<LineageGraph> BuildFieldSubgraph(
        LineageGraph fullGraph,
        LineageNode entryNode,
        string entryNodeId,
        LineageFieldRequest req,
        CancellationToken ct)
    {
        var fieldGraph = new LineageGraph();
        fieldGraph.Nodes.Add(entryNode);

        // One read for the whole thing. The provider composes the DataSet with its sources and each
        // source with its mappings, so the sources carrying a matching field are found by walking
        // what came back - where this used to query the DataSet, then its sources, then issue one
        // more query per source for mappings, then one more per source id to read those sources
        // back again.
        var dataSet = await _dataSetProvider.Get(req.EntityName, ct).ConfigureAwait(false);
        var sources = dataSet.IsSuccess && dataSet.Value is { } composed
            ? (composed.Sources ?? []).Where(source =>
                (source.Mappings ?? []).Exists(m =>
                    string.Equals(m.LogicalFieldName, req.FieldName, StringComparison.OrdinalIgnoreCase)))
                .ToList()
            : [];

        var addedNodeIds = new HashSet<string>(StringComparer.Ordinal) { entryNodeId };

        var edgeId = AddConnectionNodes(fieldGraph, fullGraph, sources, entryNodeId, addedNodeIds, 0);
        await AddCalculationNodes(fieldGraph, fullGraph, entryNodeId, req.FieldName, addedNodeIds, edgeId, ct).ConfigureAwait(false);

        return fieldGraph;
    }

    /// <summary>Adds connection nodes to the field subgraph for sources that feed the requested field.</summary>
    /// <returns>The next available edge ID.</returns>
    private static int AddConnectionNodes(
        LineageGraph fieldGraph,
        LineageGraph fullGraph,
        IReadOnlyList<DataSetSourceConfiguration> sources,
        string entryNodeId,
        HashSet<string> addedNodeIds,
        int edgeId)
    {
        foreach (var source in sources)
        {
            if (string.IsNullOrEmpty(source.ConnectionName)) continue;

            var connNodeId = $"Connection_{source.ConnectionName}";
            if (!addedNodeIds.Add(connNodeId)) continue;

            var connNode = fullGraph.FindNode(connNodeId);
            fieldGraph.Nodes.Add(connNode ?? new LineageNode
            {
                Id = connNodeId,
                Type = LineageNodeTypes.ByName("Connection"),
                Name = source.ConnectionName
            });
            fieldGraph.Edges.Add(new LineageEdge
            {
                Id = $"e{edgeId++}",
                SourceId = connNodeId,
                TargetId = entryNodeId,
                Type = LineageEdgeTypes.ByName("ReadsFrom")
            });
        }
        return edgeId;
    }

    /// <summary>Adds calculation nodes to the field subgraph for chains that consume or produce the requested field.</summary>
    private async Task AddCalculationNodes(
        LineageGraph fieldGraph,
        LineageGraph fullGraph,
        string entryNodeId,
        string fieldName,
        HashSet<string> addedNodeIds,
        int edgeId,
        CancellationToken ct)
    {
        var chains = await _lineageProvider.ReadTransform<ChainDefinitionLineageRecord>("ChainDefinition", ct).ConfigureAwait(false);
        var steps = await _lineageProvider.ReadTransform<ChainStepLineageRecord>("ChainStep", ct).ConfigureAwait(false);
        var stepFields = await _lineageProvider.ReadTransform<ChainStepSourceFieldRecord>("ChainStepSourceField", ct).ConfigureAwait(false);

        var stepsByChainId = steps.GroupBy(s => s.ChainDefinitionId).ToDictionary(g => g.Key, g => g.ToList());
        var stepFieldsByStepId = stepFields.GroupBy(sf => sf.ChainStepId).ToDictionary(g => g.Key, g => g.ToList());

        foreach (var chain in chains)
        {
            if (!stepsByChainId.TryGetValue(chain.Id, out var chainSteps)) continue;

            var consumesField = ChainConsumesField(chainSteps, stepFieldsByStepId, fieldName);
            var producesField = chainSteps.Any(step =>
                string.Equals(step.TargetField, fieldName, StringComparison.OrdinalIgnoreCase));

            if (!consumesField && !producesField) continue;

            var calcNodeId = $"Calculation_{chain.Name}";
            if (!addedNodeIds.Add(calcNodeId)) continue;

            var calcNode = fullGraph.FindNode(calcNodeId);
            fieldGraph.Nodes.Add(calcNode ?? new LineageNode
            {
                Id = calcNodeId,
                Type = LineageNodeTypes.ByName("Calculation"),
                Name = chain.Name,
                Description = chain.Category
            });

            if (consumesField)
            {
                fieldGraph.Edges.Add(new LineageEdge
                {
                    Id = $"e{edgeId++}",
                    SourceId = entryNodeId,
                    TargetId = calcNodeId,
                    Type = LineageEdgeTypes.ByName("InputsFrom")
                });
            }
            if (producesField)
            {
                fieldGraph.Edges.Add(new LineageEdge
                {
                    Id = $"e{edgeId++}",
                    SourceId = calcNodeId,
                    TargetId = entryNodeId,
                    Type = LineageEdgeTypes.ByName("Produces")
                });
            }
        }
    }

    /// <summary>Checks whether any step in a chain consumes the specified field.</summary>
    private static bool ChainConsumesField(
        List<ChainStepLineageRecord> chainSteps,
        Dictionary<Guid, List<ChainStepSourceFieldRecord>> stepFieldsByStepId,
        string fieldName)
    {
        return chainSteps.Any(step =>
            stepFieldsByStepId.TryGetValue(step.Id, out var sf) &&
            sf.Any(f => string.Equals(f.FieldName, fieldName, StringComparison.OrdinalIgnoreCase)));
    }

    /// <summary>Builds a node-only graph for resolving entry nodes.</summary>
    protected virtual async Task<LineageGraph> BuildFullGraph(CancellationToken ct)
    {
        var dataSets = await _lineageProvider.ReadData<DataSetRecord>("DataSet", ct).ConfigureAwait(false);
        var pipelines = await PipelineLineageLoader.Load(_pipelineProvider, _logger, ct).ConfigureAwait(false);
        var sources = await _lineageProvider.ReadData<DataSetSourceConfiguration>("DataSetSource", ct).ConfigureAwait(false);
        var chains = await _lineageProvider.ReadTransform<ChainDefinitionLineageRecord>("ChainDefinition", ct).ConfigureAwait(false);

        var graph = new LineageGraph();

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

        return graph;
    }


}
