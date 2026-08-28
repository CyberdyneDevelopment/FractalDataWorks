using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.Services.Pipelines.Clients.Abstractions;
using Fdw.Services.Pipelines.Components.Canvas.Projection;
using Fdw.Services.Pipelines.Components.Logging;
using Fdw.UI.Abstractions.Canvas;
using Fdw.UI.Abstractions.Canvas.EdgeTypes;
using Fdw.UI.Abstractions.Canvas.NodeTypes;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Services.Pipelines.Components.Canvas;

/// <summary>
/// In-memory edit context for a <see cref="PipelineCanvasModel"/>.
/// </summary>
/// <remarks>
/// <para>
/// Implements the <see cref="ICanvasEditContext"/> contract by mutating the model's node and edge
/// lists directly. The canvas is free-graph: nodes can be placed and connected without structural
/// constraints (validity is checked by <see cref="Validation.PipelineGraphValidator"/>).
/// </para>
/// <para>
/// This is a DRAFT editor — it does NOT persist. Persistence is the provider Save (wired later).
/// </para>
/// </remarks>
public sealed class PipelineCanvasEditContext : ICanvasEditContext
{
    private readonly PipelineCanvasModel _model;
    private readonly ILogger<PipelineCanvasEditContext> _logger;
    private int _nodeSeq;
    private int _edgeSeq;

    /// <summary>
    /// Initializes a new instance of the <see cref="PipelineCanvasEditContext"/> class.
    /// </summary>
    /// <param name="model">The canvas model to mutate.</param>
    /// <param name="logger">The logger for tracing canvas mutations. Defaults to <see cref="NullLogger"/>.</param>
    public PipelineCanvasEditContext(PipelineCanvasModel model, ILogger<PipelineCanvasEditContext>? logger = null)
    {
        _model = model;
        _logger = logger ?? NullLogger<PipelineCanvasEditContext>.Instance;
    }

    /// <inheritdoc />
    public Task<IGenericResult<string>> AddNode(
        ICanvasNodeType nodeType,
        string label,
        double x,
        double y,
        CancellationToken cancellationToken = default)
    {
        PipelineCanvasLog.AddingNode(_logger, nodeType.Name);

        var nodeId = NextNodeId();
        var ports = BuildPorts(nodeType);
        var metadata = BuildMetadataForNewNode(nodeType);

        var node = new PipelineCanvasNode(nodeId, nodeType, label, subLabel: null, x, y, ports, metadata);
        _model.MutableNodes.Add(node);

        PipelineCanvasLog.NodeAdded(_logger, nodeId, nodeType.Name);
        return Task.FromResult(GenericResult<string>.Success(nodeId));
    }

    /// <inheritdoc />
    public Task<IGenericResult<string>> Connect(
        string sourceNodeId,
        string targetNodeId,
        ICanvasEdgeType edgeType,
        string? sourcePortId,
        string? targetPortId,
        CancellationToken cancellationToken = default)
    {
        PipelineCanvasLog.ConnectingNodes(_logger, sourceNodeId, targetNodeId);

        if (_model.MutableNodes.All(n => !string.Equals(n.Id, sourceNodeId, StringComparison.Ordinal)))
            return Task.FromResult(GenericResult<string>.Failure(PipelineCanvasLog.NodeNotFound(_logger, sourceNodeId)));

        if (_model.MutableNodes.All(n => !string.Equals(n.Id, targetNodeId, StringComparison.Ordinal)))
            return Task.FromResult(GenericResult<string>.Failure(PipelineCanvasLog.NodeNotFound(_logger, targetNodeId)));

        var edgeId = NextEdgeId();
        var edge = new PipelineCanvasEdge(edgeId, sourceNodeId, targetNodeId, edgeType, sourcePortId, targetPortId);
        _model.MutableEdges.Add(edge);

        var reserializeResult = ReserializeMapPayloadIfNeeded(sourceNodeId, targetNodeId, edgeType);
        if (!reserializeResult.IsSuccess)
        {
            _model.MutableEdges.Remove(edge);
            return Task.FromResult(reserializeResult.ToNewResult<string>());
        }

        PipelineCanvasLog.EdgeCreated(_logger, edgeId);
        return Task.FromResult(GenericResult<string>.Success(edgeId));
    }

    /// <inheritdoc />
    public Task<IGenericResult> MoveNode(
        string nodeId,
        double x,
        double y,
        CancellationToken cancellationToken = default)
    {
        PipelineCanvasLog.MovingNode(_logger, nodeId);

        var node = _model.MutableNodes.FirstOrDefault(
            n => string.Equals(n.Id, nodeId, StringComparison.Ordinal));
        if (node is null)
            return Task.FromResult(GenericResult.Failure(PipelineCanvasLog.NodeNotFound(_logger, nodeId)));

        node.X = x;
        node.Y = y;
        return Task.FromResult(GenericResult.Success());
    }

    /// <inheritdoc />
    public Task<IGenericResult> DeleteNode(
        string nodeId,
        CancellationToken cancellationToken = default)
    {
        PipelineCanvasLog.DeletingNode(_logger, nodeId);

        var node = _model.MutableNodes.FirstOrDefault(
            n => string.Equals(n.Id, nodeId, StringComparison.Ordinal));
        if (node is null)
            return Task.FromResult(GenericResult.Failure(PipelineCanvasLog.NodeNotFound(_logger, nodeId)));

        var connectedEdges = _model.MutableEdges
            .Where(e => string.Equals(e.SourceNodeId, nodeId, StringComparison.Ordinal)
                        || string.Equals(e.TargetNodeId, nodeId, StringComparison.Ordinal))
            .ToList();

        foreach (var edge in connectedEdges)
            _model.MutableEdges.Remove(edge);

        _model.MutableNodes.Remove(node);

        PipelineCanvasLog.NodeDeleted(_logger, nodeId, connectedEdges.Count);
        return Task.FromResult(GenericResult.Success());
    }

    /// <inheritdoc />
    public Task<IGenericResult> DeleteEdge(
        string edgeId,
        CancellationToken cancellationToken = default)
    {
        var edge = _model.MutableEdges.FirstOrDefault(
            e => string.Equals(e.Id, edgeId, StringComparison.Ordinal));
        if (edge is null)
            return Task.FromResult(GenericResult.Failure(PipelineCanvasLog.EdgeNotFound(_logger, edgeId)));

        _model.MutableEdges.Remove(edge);

        var reserializeResult = ReserializeMapPayloadIfNeeded(edge.SourceNodeId, edge.TargetNodeId, edge.EdgeType);
        if (!reserializeResult.IsSuccess)
        {
            _model.MutableEdges.Add(edge);
            return Task.FromResult(reserializeResult);
        }

        PipelineCanvasLog.EdgeDeleted(_logger, edgeId);
        return Task.FromResult(GenericResult.Success());
    }

    /// <inheritdoc />
    public Task<IGenericResult> UpdateNodeMetadata(
        string nodeId,
        IReadOnlyDictionary<string, string> metadata,
        CancellationToken cancellationToken = default)
    {
        var node = _model.MutableNodes.FirstOrDefault(
            n => string.Equals(n.Id, nodeId, StringComparison.Ordinal));
        if (node is null)
            return Task.FromResult(GenericResult.Failure(
                PipelineCanvasLog.NodeMetadataUpdateNodeNotFound(_logger, nodeId)));

        foreach (var kv in metadata)
            node.MutableMetadata[kv.Key] = kv.Value;

        PipelineCanvasLog.NodeMetadataUpdated(_logger, nodeId, metadata.Count);
        return Task.FromResult(GenericResult.Success());
    }

    // ── Transform authoring (concrete-only — not part of ICanvasEditContext) ──

    /// <summary>
    /// Populates (or repopulates) the in/out ports of a Transform node from explicit field-name lists.
    /// </summary>
    /// <remarks>
    /// Field lists are plain names (no DataType) — a caller that has full schema metadata (e.g. a
    /// provider resolving the bound source/sink DataSet's fields) builds the id/label from that;
    /// this method only knows the port id convention (<c>in:{field}</c> / <c>out:{field}</c>).
    /// </remarks>
    /// <param name="nodeId">The identifier of the Transform node whose ports should be populated.</param>
    /// <param name="inputFields">The input field names (become <c>in:{field}</c> ports).</param>
    /// <param name="outputFields">The output field names (become <c>out:{field}</c> ports).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A result indicating success or failure.</returns>
    public Task<IGenericResult> PopulateTransformPorts(
        string nodeId,
        IReadOnlyList<string> inputFields,
        IReadOnlyList<string> outputFields,
        CancellationToken cancellationToken = default)
    {
        var node = _model.MutableNodes.FirstOrDefault(
            n => string.Equals(n.Id, nodeId, StringComparison.Ordinal));
        if (node is null)
            return Task.FromResult(GenericResult.Failure(PipelineCanvasLog.NodeNotFound(_logger, nodeId)));

        if (inputFields.Count == 0 && outputFields.Count == 0)
            return Task.FromResult(GenericResult.Failure(TransformAuthoringLog.TransformPortsNoFields(_logger, nodeId)));

        var ports = new List<ICanvasPort>(inputFields.Count + outputFields.Count);
        foreach (var field in inputFields)
            ports.Add(new PipelineCanvasPort($"in:{field}", field, PortDirections.ByName("In")));
        foreach (var field in outputFields)
            ports.Add(new PipelineCanvasPort($"out:{field}", field, PortDirections.ByName("Out")));

        node.SetPorts(ports);
        return Task.FromResult(GenericResult.Success());
    }

    /// <summary>
    /// Sets the filter expression on a Filter transform node, serialising it into the node's
    /// ConfigPayload metadata via <see cref="TransformConfigPayloadSerializer"/>.
    /// </summary>
    /// <param name="nodeId">The identifier of the Filter transform node.</param>
    /// <param name="filterExpression">The filter expression to set.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A result indicating success or failure.</returns>
    public Task<IGenericResult> SetFilterExpression(
        string nodeId,
        string filterExpression,
        CancellationToken cancellationToken = default)
    {
        var node = _model.MutableNodes.FirstOrDefault(
            n => string.Equals(n.Id, nodeId, StringComparison.Ordinal));
        if (node is null)
            return Task.FromResult(GenericResult.Failure(PipelineCanvasLog.NodeNotFound(_logger, nodeId)));

        var operationTypeCheck = RequireOperationType(node, "Filter");
        if (!operationTypeCheck.IsSuccess)
            return Task.FromResult(operationTypeCheck);

        var payloadResult = TransformConfigPayloadSerializer.ToConfigPayload(
            "Filter", node, [], filterExpression: filterExpression, logger: _logger);
        if (!payloadResult.IsSuccess)
            return Task.FromResult<IGenericResult>(payloadResult);

        node.MutableMetadata[PipelineCanvasMetadataKeys.ConfigPayload] = payloadResult.Value!;
        return Task.FromResult(GenericResult.Success());
    }

    /// <summary>
    /// Sets the lookup configuration on a Lookup transform node, serialising it into the node's
    /// ConfigPayload metadata via <see cref="TransformConfigPayloadSerializer"/>.
    /// </summary>
    /// <param name="nodeId">The identifier of the Lookup transform node.</param>
    /// <param name="lookup">The lookup configuration to set.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A result indicating success or failure.</returns>
    public Task<IGenericResult> SetLookup(
        string nodeId,
        LookupClientRequest lookup,
        CancellationToken cancellationToken = default)
    {
        var node = _model.MutableNodes.FirstOrDefault(
            n => string.Equals(n.Id, nodeId, StringComparison.Ordinal));
        if (node is null)
            return Task.FromResult(GenericResult.Failure(PipelineCanvasLog.NodeNotFound(_logger, nodeId)));

        var operationTypeCheck = RequireOperationType(node, "Lookup");
        if (!operationTypeCheck.IsSuccess)
            return Task.FromResult(operationTypeCheck);

        var payloadResult = TransformConfigPayloadSerializer.ToConfigPayload(
            "Lookup", node, [], lookup: lookup, logger: _logger);
        if (!payloadResult.IsSuccess)
            return Task.FromResult<IGenericResult>(payloadResult);

        node.MutableMetadata[PipelineCanvasMetadataKeys.ConfigPayload] = payloadResult.Value!;
        return Task.FromResult(GenericResult.Success());
    }

    // ── Private helpers ───────────────────────────────────────────────────────

    private string NextNodeId() => $"pnode-{System.Threading.Interlocked.Increment(ref _nodeSeq)}";
    private string NextEdgeId() => $"pedge-{System.Threading.Interlocked.Increment(ref _edgeSeq)}";

    private static IReadOnlyList<ICanvasPort> BuildPorts(ICanvasNodeType nodeType)
    {
        var inPort = new PipelineCanvasPort("in", "Input", Fdw.UI.Abstractions.Canvas.PortDirections.ByName("In")!);
        var outPort = new PipelineCanvasPort("out", "Output", Fdw.UI.Abstractions.Canvas.PortDirections.ByName("Out")!);

        // All node types get both ports in edit mode — the validator enforces which are used.
        return [inPort, outPort];
    }

    private static Dictionary<string, string> BuildMetadataForNewNode(ICanvasNodeType nodeType)
    {
        return new Dictionary<string, string>(StringComparer.Ordinal);
    }

    private IGenericResult RequireOperationType(PipelineCanvasNode node, string expectedOperationType)
    {
        if (!node.Metadata.TryGetValue(PipelineCanvasMetadataKeys.OperationType, out var operationType)
            || string.IsNullOrWhiteSpace(operationType))
        {
            return GenericResult.Failure(TransformAuthoringLog.OperationTypeMetadataMissing(_logger, node.Id, expectedOperationType));
        }

        if (!string.Equals(operationType, expectedOperationType, StringComparison.Ordinal))
        {
            return string.Equals(expectedOperationType, "Filter", StringComparison.Ordinal)
                ? GenericResult.Failure(TransformAuthoringLog.SetFilterExpressionWrongOperationType(_logger, node.Id, operationType))
                : GenericResult.Failure(TransformAuthoringLog.SetLookupWrongOperationType(_logger, node.Id, operationType));
        }

        return GenericResult.Success();
    }

    private IGenericResult ReserializeMapPayloadIfNeeded(string sourceNodeId, string targetNodeId, ICanvasEdgeType edgeType)
    {
        if (!string.Equals(edgeType.Name, "FieldMapping", StringComparison.Ordinal))
            return GenericResult.Success();

        if (!string.Equals(sourceNodeId, targetNodeId, StringComparison.Ordinal))
            return GenericResult.Success();

        var node = _model.MutableNodes.FirstOrDefault(
            n => string.Equals(n.Id, sourceNodeId, StringComparison.Ordinal));
        if (node is null || !string.Equals(node.NodeType.Name, "Transform", StringComparison.Ordinal))
            return GenericResult.Success();

        if (!node.Metadata.TryGetValue(PipelineCanvasMetadataKeys.OperationType, out var operationType)
            || !string.Equals(operationType, "Map", StringComparison.Ordinal))
            return GenericResult.Success();

        var mappingEdges = _model.MutableEdges
            .Where(e => string.Equals(e.EdgeType.Name, "FieldMapping", StringComparison.Ordinal)
                        && string.Equals(e.SourceNodeId, node.Id, StringComparison.Ordinal)
                        && string.Equals(e.TargetNodeId, node.Id, StringComparison.Ordinal))
            .ToList();

        var payloadResult = TransformConfigPayloadSerializer.ToConfigPayload("Map", node, mappingEdges, logger: _logger);
        if (!payloadResult.IsSuccess)
            return payloadResult;

        node.MutableMetadata[PipelineCanvasMetadataKeys.ConfigPayload] = payloadResult.Value!;
        return GenericResult.Success();
    }
}
