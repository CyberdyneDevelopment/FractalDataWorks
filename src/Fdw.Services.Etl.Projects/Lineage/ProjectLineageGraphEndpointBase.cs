using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Commands.Data;
using Fdw.Data.Lineage;
using Fdw.Operations.Endpoints;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Etl.Projects.Abstractions.TypeCollections;
using Fdw.Services.Pipelines;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Etl.Projects.Lineage;

/// <summary>
/// Extends <see cref="GetLineageGraphEndpointBase"/> by adding OrchestrationNode and Pipeline
/// nodes and edges from the <c>pipe.*</c> tables to the full lineage graph.
/// </summary>
/// <remarks>
/// Why a derived class instead of patching the base: the base lives in
/// <c>Fdw.Operations.Endpoints</c> which must not depend on
/// <c>Fdw.Services.Etl.Projects</c> (circular reference).
/// Reference apps that want project-aware lineage inherit from this class instead of the base.
///
/// FDW-388: Queries the single recursive <c>pipe.OrchestrationNode</c> table instead of the
/// v1 3-table hierarchy. NodeTypeId discriminates between Project (1), Stage (2), and Step (3)
/// nodes; all levels are now fetched in a single query.
/// </remarks>
public abstract class ProjectLineageGraphEndpointBase : GetLineageGraphEndpointBase
{
    private readonly IConfigurationGateway _configurationGateway;

    private const string OrchestrationNodeContainer = "OrchestrationNode";
    private const string NodePipelineContainer = "OrchestrationNodePipeline";
    private const string NodePipelinePrerequisiteContainer = "OrchestrationNodePipelinePrerequisite";
    private const string PipeSchemaPath = "pipe";
    private const string ConfigurationDbConnection = "PlatformConfiguration";

    private static readonly TimeSpan LineageCacheDuration = TimeSpan.FromMinutes(5);

    private static readonly string[] NodeInvalidationTags = ["pipe.OrchestrationNode"];
    private static readonly string[] NodePipelineInvalidationTags = ["pipe.OrchestrationNodePipeline"];
    private static readonly string[] PrerequisiteInvalidationTags = ["pipe.OrchestrationNodePipelinePrerequisite"];

    private static readonly int ProjectNodeTypeId = OrchestrationNodeTypes.ByName("Project").Id;
    private static readonly int StageNodeTypeId = OrchestrationNodeTypes.ByName("Stage").Id;
    private static readonly int StepNodeTypeId = OrchestrationNodeTypes.ByName("Step").Id;

    /// <inheritdoc/>
    protected ProjectLineageGraphEndpointBase(
        IConfigurationGateway configurationGateway,
        PipelineServiceConfigurationProvider pipelineProvider,
        ILogger<GetLineageGraphEndpointBase> logger)
        : base(configurationGateway, pipelineProvider, logger)
    {
        _configurationGateway = configurationGateway;
    }

    /// <inheritdoc/>
    protected override async Task<LineageGraph> BuildFullGraph(CancellationToken ct)
    {
        // Step 1: build the base graph (DataSets, Pipelines, Connections, Calculations).
        var graph = await base.BuildFullGraph(ct).ConfigureAwait(false);

        // Step 2: query the single recursive node table + 2 membership tables in parallel.
        var nodesTask = QueryWithCaching<OrchestrationNodeLineageRecord>(
            OrchestrationNodeContainer, NodeInvalidationTags, ct);
        var nodePipelinesTask = QueryWithCaching<OrchestrationNodePipelineLineageRecord>(
            NodePipelineContainer, NodePipelineInvalidationTags, ct);
        var prerequisitesTask = QueryWithCaching<OrchestrationNodePipelinePrerequisiteLineageRecord>(
            NodePipelinePrerequisiteContainer, PrerequisiteInvalidationTags, ct);

        await Task.WhenAll(nodesTask, nodePipelinesTask, prerequisitesTask).ConfigureAwait(false);

        var nodes = await nodesTask.ConfigureAwait(false);
        var nodePipelines = await nodePipelinesTask.ConfigureAwait(false);
        var prerequisites = await prerequisitesTask.ConfigureAwait(false);

        // Step 3: partition nodes by type and add lineage nodes.
        var projectNodes = nodes.Where(n => n.NodeTypeId == ProjectNodeTypeId).ToList();
        var stageNodes = nodes.Where(n => n.NodeTypeId == StageNodeTypeId).ToList();
        var stepNodes = nodes.Where(n => n.NodeTypeId == StepNodeTypeId).ToList();

        AddProjectNodes(graph, projectNodes);
        AddStageNodes(graph, stageNodes);
        AddStepNodes(graph, stepNodes);

        // Step 4: add containment and dependency edges.
        var edgeId = graph.Edges.Count;
        var nodeById = nodes.ToDictionary(n => n.Id, n => n);
        AddContainmentEdges(graph, nodeById, stageNodes, stepNodes, nodePipelines, prerequisites, ref edgeId);

        return graph;
    }

    private static void AddProjectNodes(LineageGraph graph, IReadOnlyList<OrchestrationNodeLineageRecord> projects)
    {
        foreach (var project in projects)
        {
            graph.Nodes.Add(new LineageNode
            {
                Id = $"Project_{project.Name}",
                Type = LineageNodeTypes.ByName("Project"),
                Name = project.Name,
                Description = project.Description
            });
        }
    }

    private static void AddStageNodes(LineageGraph graph, IReadOnlyList<OrchestrationNodeLineageRecord> stages)
    {
        foreach (var stage in stages)
        {
            graph.Nodes.Add(new LineageNode
            {
                Id = $"Stage_{stage.Id:N}",
                Type = LineageNodeTypes.ByName("Stage"),
                Name = stage.Name,
                Metadata = new Dictionary<string, object?>(StringComparer.Ordinal)
                {
                    ["Ordinal"] = stage.Ordinal
                }
            });
        }
    }

    private static void AddStepNodes(LineageGraph graph, IReadOnlyList<OrchestrationNodeLineageRecord> steps)
    {
        foreach (var step in steps)
        {
            graph.Nodes.Add(new LineageNode
            {
                Id = $"Step_{step.Id:N}",
                Type = LineageNodeTypes.ByName("Step"),
                Name = step.Name,
                Metadata = new Dictionary<string, object?>(StringComparer.Ordinal)
                {
                    ["Ordinal"] = step.Ordinal
                }
            });
        }
    }

    private static void AddContainmentEdges(
        LineageGraph graph,
        Dictionary<Guid, OrchestrationNodeLineageRecord> nodeById,
        IReadOnlyList<OrchestrationNodeLineageRecord> stageNodes,
        IReadOnlyList<OrchestrationNodeLineageRecord> stepNodes,
        IReadOnlyList<OrchestrationNodePipelineLineageRecord> nodePipelines,
        IReadOnlyList<OrchestrationNodePipelinePrerequisiteLineageRecord> prerequisites,
        ref int edgeId)
    {
        // Project → Contains → Stage edges.
        foreach (var stage in stageNodes)
        {
            if (stage.ParentId is null) continue;
            if (!nodeById.TryGetValue(stage.ParentId.Value, out var parentProject)) continue;
            graph.Edges.Add(new LineageEdge
            {
                Id = $"e{edgeId++}",
                SourceId = $"Project_{parentProject.Name}",
                TargetId = $"Stage_{stage.Id:N}",
                Type = LineageEdgeTypes.ByName("Contains")
            });
        }

        // Stage → Sequences → Step edges.
        foreach (var step in stepNodes)
        {
            if (step.ParentId is null) continue;
            graph.Edges.Add(new LineageEdge
            {
                Id = $"e{edgeId++}",
                SourceId = $"Stage_{step.ParentId.Value:N}",
                TargetId = $"Step_{step.Id:N}",
                Type = LineageEdgeTypes.ByName("Sequences")
            });
        }

        // Step → Contains → Pipeline edges.
        var stepIds = new HashSet<Guid>(stepNodes.Select(s => s.Id));
        foreach (var membership in nodePipelines)
        {
            if (!stepIds.Contains(membership.NodeId)) continue;
            graph.Edges.Add(new LineageEdge
            {
                Id = $"e{edgeId++}",
                SourceId = $"Step_{membership.NodeId:N}",
                TargetId = $"Pipeline_{membership.Name}",
                Type = LineageEdgeTypes.ByName("Contains")
            });
        }

        // Pipeline → DependsOn → Pipeline edges.
        var pipelineNameById = nodePipelines
            .GroupBy(p => p.PipelineId)
            .ToDictionary(g => g.Key, g => g.First().Name);

        foreach (var prereq in prerequisites)
        {
            if (!pipelineNameById.TryGetValue(prereq.PipelineId, out var dependentName)) continue;
            if (!pipelineNameById.TryGetValue(prereq.PrerequisitePipelineId, out var prerequisiteName)) continue;

            graph.Edges.Add(new LineageEdge
            {
                Id = $"e{edgeId++}",
                SourceId = $"Pipeline_{dependentName}",
                TargetId = $"Pipeline_{prerequisiteName}",
                Type = LineageEdgeTypes.ByName("DependsOn")
            });
        }
    }

    /// <summary>
    /// Queries all records from the specified container with CachePolicy metadata applied.
    /// Returns an empty list on failure — consistent with the base class's best-effort
    /// lineage graph construction approach.
    /// </summary>
    private async Task<IReadOnlyList<T>> QueryWithCaching<T>(
        string containerName,
        string[] invalidationTags,
        CancellationToken ct) where T : class
    {
        var command = DataQuery.From<T>(ConfigurationDbConnection, PipeSchemaPath, containerName)
            .WithCaching(LineageCacheDuration, invalidationTags)
            .Build();

        var result = await _configurationGateway.Execute<IEnumerable<T>>(command, ct).ConfigureAwait(false);
        return result.IsSuccess ? result.Value?.ToList() ?? [] : [];
    }
}
