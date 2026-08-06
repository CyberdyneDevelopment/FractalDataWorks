using System;
using System.Collections.Generic;
using Fdw.Services.Pipelines.Components.Canvas;
using Fdw.Services.Pipelines.Components.Canvas.Validation;
using Fdw.UI.Abstractions.Canvas;
using Fdw.UI.Abstractions.Canvas.EdgeTypes;
using Fdw.UI.Abstractions.Canvas.NodeTypes;
using Fdw.UI.Abstractions.RenderModeOptions;

namespace Fdw.Services.Pipelines.Components.Tests.Canvas;

/// <summary>
/// Shared helpers for building PipelineCanvasModel fixtures used across the canvas test suites.
/// </summary>
internal static class PipelineCanvasTestFixtures
{
    // ── Node type helpers ─────────────────────────────────────────────────────

    internal static ICanvasNodeType DataSetNodeType =>
        CanvasNodeTypes.ByName("DataSet")!;

    internal static ICanvasNodeType TransformNodeType =>
        CanvasNodeTypes.ByName("Transform")!;

    internal static ICanvasEdgeType FlowEdgeType =>
        CanvasEdgeTypes.ByName("Flow")!;

    internal static ICanvasEdgeType FieldMappingEdgeType =>
        CanvasEdgeTypes.ByName("FieldMapping")!;

    internal static IRenderMode EditMode =>
        RenderModes.ByName("Edit")!;

    internal static IRenderMode ViewMode =>
        RenderModes.ByName("View")!;

    // ── Port helpers ──────────────────────────────────────────────────────────

    private static IReadOnlyList<ICanvasPort> DataSetPorts()
    {
        return
        [
            new PipelineCanvasPort("in", "Input", PortDirections.ByName("In")!),
            new PipelineCanvasPort("out", "Output", PortDirections.ByName("Out")!),
        ];
    }

    private static IReadOnlyList<ICanvasPort> TransformPorts()
    {
        return
        [
            new PipelineCanvasPort("in", "Input", PortDirections.ByName("In")!),
            new PipelineCanvasPort("out", "Output", PortDirections.ByName("Out")!),
        ];
    }

    // ── Source node ───────────────────────────────────────────────────────────

    internal static PipelineCanvasNode BuildSourceNode(string id = "source", string dataSetName = "SourceData")
    {
        var meta = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            [PipelineCanvasMetadataKeys.DataSetRole] = PipelineCanvasMetadataKeys.RoleSource,
            [PipelineCanvasMetadataKeys.DataSetName] = dataSetName,
        };
        return new PipelineCanvasNode(id, DataSetNodeType, dataSetName, "Source", 0, 100, DataSetPorts(), meta);
    }

    // ── Sink node ─────────────────────────────────────────────────────────────

    internal static PipelineCanvasNode BuildSinkNode(string id = "sink", string dataSetName = "DestData")
    {
        var meta = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            [PipelineCanvasMetadataKeys.DataSetRole] = PipelineCanvasMetadataKeys.RoleSink,
            [PipelineCanvasMetadataKeys.DataSetName] = dataSetName,
        };
        return new PipelineCanvasNode(id, DataSetNodeType, dataSetName, "Sink", 400, 100, DataSetPorts(), meta);
    }

    // ── Transform node ────────────────────────────────────────────────────────

    internal static PipelineCanvasNode BuildTransformNode(
        string id,
        string operationType = "Map",
        string label = "Transform",
        double x = 200,
        double y = 100)
    {
        var meta = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            [PipelineCanvasMetadataKeys.OperationType] = operationType,
        };
        return new PipelineCanvasNode(id, TransformNodeType, label, operationType, x, y, TransformPorts(), meta);
    }

    internal static PipelineCanvasNode BuildTransformNodeNoOperationType(string id = "t1")
    {
        // Why: deliberately omits OperationType to trigger the validator rule.
        var meta = new Dictionary<string, string>(StringComparer.Ordinal);
        return new PipelineCanvasNode(id, TransformNodeType, "BadTransform", null, 200, 100, TransformPorts(), meta);
    }

    // ── Flow edge ─────────────────────────────────────────────────────────────

    internal static PipelineCanvasEdge BuildFlowEdge(string sourceId, string targetId) =>
        new($"edge-{sourceId}-{targetId}", sourceId, targetId, FlowEdgeType);

    // ── Full models ───────────────────────────────────────────────────────────

    /// <summary>
    /// Builds a valid model: source → N transforms → sink, all connected via Flow edges.
    /// </summary>
    internal static PipelineCanvasModel BuildValidModel(
        int transformCount = 0,
        string sourceName = "SourceData",
        string sinkName = "DestData")
    {
        var nodes = new List<PipelineCanvasNode>();
        var edges = new List<PipelineCanvasEdge>();

        var source = BuildSourceNode("source", sourceName);
        nodes.Add(source);

        var previousId = "source";

        for (var i = 0; i < transformCount; i++)
        {
            var transformId = $"t{i + 1}";
            nodes.Add(BuildTransformNode(transformId, "Map", $"T{i + 1}", (i + 1) * 200.0, 100));
            edges.Add(BuildFlowEdge(previousId, transformId));
            previousId = transformId;
        }

        var sink = BuildSinkNode("sink", sinkName);
        nodes.Add(sink);
        edges.Add(BuildFlowEdge(previousId, "sink"));

        return new PipelineCanvasModel("pipe-1", "Test Pipeline", EditMode, nodes, edges);
    }

    /// <summary>
    /// Builds a model with an empty graph (no nodes, no edges).
    /// </summary>
    internal static PipelineCanvasModel BuildEmptyModel() =>
        new("empty", "Empty", EditMode);

    /// <summary>
    /// Builds the issues list representing a graph with only warnings (no errors).
    /// </summary>
    internal static IReadOnlyList<PipelineGraphValidationIssue> MixedIssues()
    {
        return
        [
            new PipelineGraphValidationIssue(
                Fdw.UI.Abstractions.Components.ValidationSeverities.Error,
                "An error"),
            new PipelineGraphValidationIssue(
                Fdw.UI.Abstractions.Components.ValidationSeverities.Warning,
                "A warning"),
        ];
    }
}
