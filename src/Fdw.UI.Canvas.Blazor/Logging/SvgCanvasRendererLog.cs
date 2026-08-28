#pragma warning disable CS1591
using System.Diagnostics.CodeAnalysis;
using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace Fdw.UI.Canvas.Blazor.Logging;

/// <summary>
/// MessageLogging for the inline-SVG canvas renderer (<c>SvgCanvasRenderer</c>).
/// EventId range: 4720–4739 (UI canvas SVG renderer layer).
/// </summary>
[ExcludeFromCodeCoverage]
[MessageLoggingTypeCode("CANVAS")]
public static partial class SvgCanvasRendererLog
{
    [MessageLogging(
        EventId = 4720,
        Level = LogLevel.Trace,
        Message = "Persisting moved node '{nodeId}' to ({x}, {y}) via the canvas edit context")]
    public static partial IGenericMessage MovingNode(ILogger logger, string nodeId, double x, double y);

    [MessageLogging(
        EventId = 4721,
        Level = LogLevel.Information,
        Message = "Node '{nodeId}' moved to ({x}, {y})")]
    public static partial IGenericMessage NodeMoved(ILogger logger, string nodeId, double x, double y);

    [MessageLogging(
        EventId = 4722,
        Level = LogLevel.Trace,
        Message = "Connecting nodes '{sourceId}' → '{targetId}' via the canvas edit context")]
    public static partial IGenericMessage ConnectingNodes(ILogger logger, string sourceId, string targetId);

    [MessageLogging(
        EventId = 4723,
        Level = LogLevel.Information,
        Message = "Connected nodes '{sourceId}' → '{targetId}'")]
    public static partial IGenericMessage NodesConnected(ILogger logger, string sourceId, string targetId);

    [MessageLogging(
        EventId = 4724,
        Level = LogLevel.Trace,
        Message = "Deleting selected canvas element '{elementId}' ({elementKind}) via the edit context")]
    public static partial IGenericMessage DeletingSelection(ILogger logger, string elementId, string elementKind);

    [MessageLogging(
        EventId = 4725,
        Level = LogLevel.Information,
        Message = "Deleted canvas element '{elementId}' ({elementKind})")]
    public static partial IGenericMessage SelectionDeleted(ILogger logger, string elementId, string elementKind);

    [MessageLogging(
        EventId = 4726,
        Level = LogLevel.Warning,
        Message = "Canvas edit operation '{operation}' failed: {reason}")]
    public static partial IGenericMessage EditOperationFailed(ILogger logger, string operation, string? reason);

    [MessageLogging(
        EventId = 4727,
        Level = LogLevel.Error,
        Message = "The 'Flow' canvas edge type is not registered — canvas connections cannot be created")]
    public static partial IGenericMessage FlowEdgeTypeNotRegistered(ILogger logger);

    // ── Port-level connect (field mapping) ────────────────────────────────────────

    [MessageLogging(
        EventId = 4728,
        Level = LogLevel.Trace,
        Message = "Port connect started at port '{portId}' on node '{nodeId}' — awaiting a target port")]
    public static partial IGenericMessage BeginningPortConnect(ILogger logger, string nodeId, string portId);

    [MessageLogging(
        EventId = 4729,
        Level = LogLevel.Trace,
        Message = "Port connect cancelled at source port '{portId}' on node '{nodeId}'")]
    public static partial IGenericMessage PortConnectCancelled(ILogger logger, string nodeId, string portId);

    [MessageLogging(
        EventId = 4730,
        Level = LogLevel.Trace,
        Message = "Connecting port '{sourcePortId}' on '{sourceNodeId}' → port '{targetPortId}' on '{targetNodeId}' via the canvas edit context")]
    public static partial IGenericMessage ConnectingPorts(
        ILogger logger, string sourceNodeId, string sourcePortId, string targetNodeId, string targetPortId);

    [MessageLogging(
        EventId = 4731,
        Level = LogLevel.Information,
        Message = "Connected port '{sourcePortId}' on '{sourceNodeId}' → port '{targetPortId}' on '{targetNodeId}'")]
    public static partial IGenericMessage PortsConnected(
        ILogger logger, string sourceNodeId, string sourcePortId, string targetNodeId, string targetPortId);

    [MessageLogging(
        EventId = 4732,
        Level = LogLevel.Critical,
        Message = "The 'FieldMapping' canvas edge type is not registered — port connections cannot be created")]
    public static partial IGenericMessage FieldMappingEdgeTypeNotRegistered(ILogger logger);

    // ── Unrenderable port geometry ────────────────────────────────────────────────

    [MessageLogging(
        EventId = 4733,
        Level = LogLevel.Warning,
        Message = "Canvas edge '{edgeId}' anchors to port '{portId}', which node '{nodeId}' does not expose — the edge is not rendered")]
    public static partial IGenericMessage PortAnchorUnresolvable(ILogger logger, string edgeId, string nodeId, string portId);

    [MessageLogging(
        EventId = 4734,
        Level = LogLevel.Warning,
        Message = "Port '{portId}' on node '{nodeId}' has direction '{directionName}', which the SVG renderer has no geometry for — the port is not rendered")]
    public static partial IGenericMessage PortDirectionNotRenderable(
        ILogger logger, string nodeId, string portId, string directionName);

    [MessageLogging(
        EventId = 4735,
        Level = LogLevel.Warning,
        Message = "Self-loop canvas edge '{edgeId}' on node '{nodeId}' has no source/target port anchors — the edge is not rendered")]
    public static partial IGenericMessage SelfLoopEdgeMissingPorts(ILogger logger, string edgeId, string nodeId);
}
