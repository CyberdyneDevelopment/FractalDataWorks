using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Fdw.Messages;
using Fdw.Results;
using Fdw.Services.Pipelines.Clients.Abstractions;
using Fdw.Services.Pipelines.Components.Logging;
using Fdw.UI.Abstractions.Canvas;
using Fdw.UI.Abstractions.RenderModeOptions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Services.Pipelines.Components.Canvas.Projection;

/// <summary>
/// Pure, render-agnostic projection from <see cref="PipelineDetailResponse"/> (the real,
/// persistence-backed pipeline contract returned by <c>IPipelineClient.Get</c>) to a
/// <see cref="PipelineCanvasModel"/>.
/// </summary>
/// <remarks>
/// <para>
/// Distinct from <see cref="PipelineCanvasProjection.ToCanvas"/>, which projects the retired designer
/// contract (<see cref="Fdw.UI.Pipelines.Clients.Models.PipelineDetailPayload"/> — a task/connection graph
/// with no live persistence endpoint). This projection is the one production code actually reaches:
/// <c>PipelineBuilderProvider.LoadExisting</c> calls it after resolving the route's Guid id to a
/// pipeline name and fetching its <see cref="PipelineDetailResponse"/>.
/// </para>
/// <para>
/// Builds one Source DataSet node, one Sink DataSet node, and one Transform node per
/// <see cref="PipelineTransformClientRequest"/> (ordered by <c>ExecutionOrder</c>), chained by Flow
/// edges (source → transforms in order → sink) so the loaded canvas is immediately valid per
/// <see cref="Validation.PipelineGraphValidator"/> — re-saving an unedited, freshly-loaded pipeline
/// must not fail validation. Each Map transform's field mappings are additionally materialised as
/// self-loop <c>FieldMapping</c> edges on its Transform node (the same shape
/// <see cref="PipelineCanvasEditContext.Connect"/> produces for a live mapping gesture) so the
/// mappings remain editable after load.
/// </para>
/// <para>
/// Every required piece is validated before a value is produced — no fallback/defaulted values are
/// ever substituted for a missing piece; the first missing/invalid piece fails the whole projection.
/// </para>
/// </remarks>
public static class PipelineDetailCanvasProjection
{
    private const string InPortId = "in";
    private const string OutPortId = "out";
    private const string InPortPrefix = "in:";
    private const string OutPortPrefix = "out:";
    private const double ColumnWidth = 200.0;

    /// <summary>
    /// Projects a <see cref="PipelineDetailResponse"/> into a <see cref="PipelineCanvasModel"/>.
    /// </summary>
    /// <param name="detail">The loaded pipeline detail.</param>
    /// <param name="renderMode">The render mode for the resulting canvas.</param>
    /// <param name="logger">Optional logger; defaults to <see cref="NullLogger"/> when null.</param>
    /// <returns>A result containing the projected canvas model on success, or the first validation failure.</returns>
    public static IGenericResult<PipelineCanvasModel> ToCanvas(
        PipelineDetailResponse detail,
        IRenderMode renderMode,
        ILogger? logger = null)
    {
        var log = logger ?? NullLogger.Instance;
        PipelineCanvasLog.ProjectingPipelineDetailToCanvas(log, detail.Name, detail.Id);

        if (string.IsNullOrWhiteSpace(detail.PipelineType))
            return GenericResult<PipelineCanvasModel>.Failure(PipelineCanvasLog.LoadedPipelineTypeMissing(log, detail.Name));

        var dataSetNodeType = CanvasNodeTypes.ByName("DataSet");
        if (dataSetNodeType == CanvasNodeTypes.NotFound)
            return GenericResult<PipelineCanvasModel>.Failure(PipelineBuilderProviderLog.CanvasNodeTypeUnregistered(log, "DataSet"));

        var transformNodeType = CanvasNodeTypes.ByName("Transform");
        if (transformNodeType == CanvasNodeTypes.NotFound)
            return GenericResult<PipelineCanvasModel>.Failure(PipelineBuilderProviderLog.CanvasNodeTypeUnregistered(log, "Transform"));

        var flowEdgeType = CanvasEdgeTypes.ByName("Flow");
        if (flowEdgeType == CanvasEdgeTypes.NotFound)
            return GenericResult<PipelineCanvasModel>.Failure(PipelineBuilderProviderLog.CanvasNodeTypeUnregistered(log, "Flow"));

        var fieldMappingEdgeType = CanvasEdgeTypes.ByName("FieldMapping");
        if (fieldMappingEdgeType == CanvasEdgeTypes.NotFound)
            return GenericResult<PipelineCanvasModel>.Failure(PipelineBuilderProviderLog.CanvasNodeTypeUnregistered(log, "FieldMapping"));

        var orderedTransforms = (detail.Transforms ?? []).OrderBy(t => t.ExecutionOrder).ToList();

        var sourceNode = BuildDataSetNode(dataSetNodeType, "source", PipelineCanvasMetadataKeys.RoleSource, detail.SourceConnectionName, detail.SourceDataSet, x: 0);
        var sinkNode = BuildDataSetNode(dataSetNodeType, "sink", PipelineCanvasMetadataKeys.RoleSink, detail.DestinationConnectionName, detail.DestinationDataSet, x: (orderedTransforms.Count + 1) * ColumnWidth);

        var nodes = new List<PipelineCanvasNode> { sourceNode };
        var edges = new List<PipelineCanvasEdge>();
        var previousNodeId = sourceNode.Id;
        var edgeSeq = 0;
        var column = 1;

        foreach (var transform in orderedTransforms)
        {
            var transformNodeId = "transform-" + transform.ExecutionOrder.ToString(CultureInfo.InvariantCulture);
            var transformNode = BuildTransformNode(transformNodeType, transformNodeId, transform, x: column * ColumnWidth);
            column++;

            List<PipelineCanvasEdge> mappingEdges = string.Equals(transform.OperationType, "Map", StringComparison.Ordinal)
                ? BuildFieldMappingEdges(fieldMappingEdgeType, transformNodeId, transform.FieldMappings, ref edgeSeq)
                : [];

            var payloadResult = TransformConfigPayloadSerializer.ToConfigPayload(
                transform.OperationType,
                transformNode,
                mappingEdges,
                aggregation: transform.Aggregation,
                calculation: transform.Calculation,
                filterExpression: transform.FilterExpression,
                lookup: transform.Lookup,
                logger: log);
            if (!payloadResult.IsSuccess)
                return payloadResult.ToNewResult<PipelineCanvasModel>();

            transformNode.MutableMetadata[PipelineCanvasMetadataKeys.ConfigPayload] = payloadResult.Value!;

            nodes.Add(transformNode);
            edges.AddRange(mappingEdges);
            edges.Add(new PipelineCanvasEdge(NextEdgeId(ref edgeSeq), previousNodeId, transformNodeId, flowEdgeType, OutPortId, InPortId));
            previousNodeId = transformNodeId;
        }

        nodes.Add(sinkNode);
        edges.Add(new PipelineCanvasEdge(NextEdgeId(ref edgeSeq), previousNodeId, sinkNode.Id, flowEdgeType, OutPortId, InPortId));

        var model = new PipelineCanvasModel(
            id: detail.Id.ToString(),
            title: detail.Name,
            renderMode: renderMode,
            nodes: nodes,
            edges: edges,
            pipelineType: detail.PipelineType);

        PipelineCanvasLog.LoadedPipelineProjectedToCanvas(log, detail.Name, nodes.Count, edges.Count);
        return GenericResult<PipelineCanvasModel>.Success(model);
    }

    // ── Private helpers ───────────────────────────────────────────────────────

    private static PipelineCanvasNode BuildDataSetNode(
        ICanvasNodeType dataSetNodeType,
        string nodeId,
        string role,
        string connectionName,
        string? dataSetName,
        double x)
    {
        var metadata = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            [PipelineCanvasMetadataKeys.DataSetRole] = role,
            [PipelineCanvasMetadataKeys.ConnectionName] = connectionName,
        };
        if (!string.IsNullOrEmpty(dataSetName))
            metadata[PipelineCanvasMetadataKeys.DataSetName] = dataSetName;

        var label = string.IsNullOrEmpty(dataSetName) ? connectionName : dataSetName;
        return new PipelineCanvasNode(nodeId, dataSetNodeType, label, role, x, 100, BuildPorts(), metadata);
    }

    private static PipelineCanvasNode BuildTransformNode(
        ICanvasNodeType transformNodeType,
        string nodeId,
        PipelineTransformClientRequest transform,
        double x)
    {
        var metadata = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            [PipelineCanvasMetadataKeys.OperationType] = transform.OperationType,
            [PipelineCanvasMetadataKeys.ExecutionOrder] = transform.ExecutionOrder.ToString(CultureInfo.InvariantCulture),
        };
        return new PipelineCanvasNode(nodeId, transformNodeType, transform.Name, transform.OperationType, x, 100, BuildPorts(), metadata);
    }

    private static List<PipelineCanvasEdge> BuildFieldMappingEdges(
        ICanvasEdgeType fieldMappingEdgeType,
        string transformNodeId,
        IList<PipelineFieldMappingClientRequest> fieldMappings,
        ref int edgeSeq)
    {
        var edges = new List<PipelineCanvasEdge>();
        foreach (var mapping in fieldMappings)
        {
            var metadata = new Dictionary<string, string>(StringComparer.Ordinal)
            {
                [PipelineCanvasEdgeMetadataKeys.MappingName] = mapping.Name,
                [PipelineCanvasEdgeMetadataKeys.IsRequired] = mapping.IsRequired ? "true" : "false",
                [PipelineCanvasEdgeMetadataKeys.IsEnabled] = mapping.IsEnabled ? "true" : "false",
            };
            if (!string.IsNullOrEmpty(mapping.TargetType))
                metadata[PipelineCanvasEdgeMetadataKeys.TargetType] = mapping.TargetType;
            if (!string.IsNullOrEmpty(mapping.TransformExpression))
                metadata[PipelineCanvasEdgeMetadataKeys.TransformExpression] = mapping.TransformExpression;
            if (mapping.DefaultValue is not null)
                metadata[PipelineCanvasEdgeMetadataKeys.DefaultValue] = mapping.DefaultValue;

            edges.Add(new PipelineCanvasEdge(
                NextEdgeId(ref edgeSeq),
                transformNodeId,
                transformNodeId,
                fieldMappingEdgeType,
                InPortPrefix + mapping.SourceField,
                OutPortPrefix + mapping.DestinationField,
                metadata: metadata));
        }

        return edges;
    }

    private static IReadOnlyList<ICanvasPort> BuildPorts()
    {
        return
        [
            new PipelineCanvasPort(InPortId, "Input", PortDirections.ByName("In")!),
            new PipelineCanvasPort(OutPortId, "Output", PortDirections.ByName("Out")!),
        ];
    }

    private static string NextEdgeId(ref int edgeSeq) =>
        "load-edge-" + (++edgeSeq).ToString(CultureInfo.InvariantCulture);
}
