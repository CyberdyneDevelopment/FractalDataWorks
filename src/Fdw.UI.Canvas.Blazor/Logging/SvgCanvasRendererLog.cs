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
// Why: default TypeCode ("FDW") collided with Fdw.UI.Charts.Blazor's ChartHostLog, which
// independently reused EventIds 4720-4726 — both generated "FDW-4720".."FDW-4726" for unrelated
// messages. A distinct per-project TypeCode makes the generated Code unique even though the
// numeric EventId ranges still overlap across the two sibling UI projects.
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

    // Why: an edit operation that the edit context rejected (e.g. unknown node ref). Warning, not
    // Error — the canvas remains usable and the failure is surfaced to the operator inline.
    [MessageLogging(
        EventId = 4726,
        Level = LogLevel.Warning,
        Message = "Canvas edit operation '{operation}' failed: {reason}")]
    public static partial IGenericMessage EditOperationFailed(ILogger logger, string operation, string? reason);

    // Why: Error, not Critical — the canvas remains usable for everything except creating new
    // connections; the Flow edge type being unregistered is a handled deployment defect, not a
    // process-ending condition.
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

    // Why: Critical (fatal) — mirrors FlowEdgeTypeNotRegistered. FieldMapping is a framework-seeded
    // edge type; without it the port-connect gesture cannot author a mapping at all.
    [MessageLogging(
        EventId = 4732,
        Level = LogLevel.Critical,
        Message = "The 'FieldMapping' canvas edge type is not registered — port connections cannot be created")]
    public static partial IGenericMessage FieldMappingEdgeTypeNotRegistered(ILogger logger);

    // ── Unrenderable port geometry ────────────────────────────────────────────────

    // Why: the edge names a port its node does not expose, so there is no honest anchor for it.
    // Warning (not Error) — the rest of the canvas still renders. The edge is skipped rather than
    // anchored at the node centre: every unresolved mapping would otherwise collapse onto the same
    // wrong line and read as real. Naming the node and port keeps the underlying defect findable.
    [MessageLogging(
        EventId = 4733,
        Level = LogLevel.Warning,
        Message = "Canvas edge '{edgeId}' anchors to port '{portId}', which node '{nodeId}' does not expose — the edge is not rendered")]
    public static partial IGenericMessage PortAnchorUnresolvable(ILogger logger, string edgeId, string nodeId, string portId);

    // Why: PortDirections is an extensible TypeCollection — a downstream assembly may register a
    // direction (e.g. "Bidirectional") this renderer has no column geometry for. Report it rather
    // than guessing a side; guessing would place the port on an edge it does not belong to.
    [MessageLogging(
        EventId = 4734,
        Level = LogLevel.Warning,
        Message = "Port '{portId}' on node '{nodeId}' has direction '{directionName}', which the SVG renderer has no geometry for — the port is not rendered")]
    public static partial IGenericMessage PortDirectionNotRenderable(
        ILogger logger, string nodeId, string portId, string directionName);

    // Why: a self-loop edge is the field-mapping shape, which is defined entirely by its two port
    // anchors (in:{field} → out:{field}). Without them there is nothing to draw between.
    [MessageLogging(
        EventId = 4735,
        Level = LogLevel.Warning,
        Message = "Self-loop canvas edge '{edgeId}' on node '{nodeId}' has no source/target port anchors — the edge is not rendered")]
    public static partial IGenericMessage SelfLoopEdgeMissingPorts(ILogger logger, string edgeId, string nodeId);
}
