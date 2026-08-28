using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.UI.Abstractions.Canvas;
using Fdw.UI.Canvas.Blazor.Logging;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.UI.Canvas.Blazor.Renderers.Svg;

/// <summary>
/// Inline-SVG canvas renderer for the FDW render-agnostic canvas.
/// </summary>
/// <remarks>
/// <para>
/// Renders an <see cref="ICanvasModel"/> as an inline <c>&lt;svg&gt;</c> element with:
/// <list type="bullet">
///   <item>Pan and zoom via mouse drag and scroll wheel.</item>
///   <item>Node shapes and colours driven by <see cref="SvgNodeAppearanceMap"/> — no switch/if-else on type names.</item>
///   <item>Cubic-bezier edge paths with arrowhead markers, coloured via <see cref="SvgEdgeAppearanceMap"/>.</item>
///   <item>Background dot grid.</item>
///   <item>Edit-mode drag → <see cref="ICanvasEditContext.MoveNode"/>; selection; connect and delete wired to context.</item>
///   <item>Ports drawn in In/Out columns via <see cref="SvgPortGeometry"/>, with port-anchored edges and a
///         click-source-port/click-target-port gesture that authors <c>FieldMapping</c> edges.</item>
/// </list>
/// </para>
/// <para>
/// Field mappings are <em>self-loop</em> edges — source node == target node, running from an
/// <c>in:{field}</c> port to an <c>out:{field}</c> port on one Transform node. They render inside
/// that node's own translated group so they draw above the body they cross; every other edge
/// renders beneath the nodes, as before.
/// </para>
/// </remarks>
[System.Diagnostics.CodeAnalysis.SuppressMessage("Meziantou.Analyzer", "MA0004",
    Justification = "Blazor UI event handlers run on sync context")]
public sealed partial class SvgCanvasRenderer : ComponentBase
{
    // ── Static appearance maps — shared instances, allocated once ─────────────────

    private static readonly SvgNodeAppearanceMap NodeAppearances = new();
    private static readonly SvgEdgeAppearanceMap EdgeAppearances = new();

    // ── Parameters ────────────────────────────────────────────────────────────────

    /// <summary>
    /// Gets or sets the canvas model to render. Required.
    /// </summary>
    [Parameter, EditorRequired]
    public ICanvasModel Model { get; set; } = default!;

    /// <summary>
    /// Gets or sets the optional logger. Falls back to <see cref="NullLogger{T}.Instance"/> when not supplied.
    /// </summary>
    [Parameter]
    public ILogger? Logger { get; set; }

    // ── Private State ─────────────────────────────────────────────────────────────

    // Pan/zoom transform applied to the inner <g> element.
    private double _panX = 80;
    private double _panY = 80;
    private double _scale = 0.85;

    // Selection
    private string? _selectedId;

    // Node drag
    private ICanvasNode? _draggingNode;
    private double _dragOffsetX;
    private double _dragOffsetY;

    // Canvas pan
    private bool _isPanning;
    private double _panStartX;
    private double _panStartY;
    private double _panOriginX;
    private double _panOriginY;

    // Error banner from edit-context operations
    private string? _editErrorMessage;

    // Connect affordance state: first click sets the source, second click triggers Connect.
    private string? _connectSourceId;

    // Port-connect affordance state: first port click sets the source port, second completes a
    // FieldMapping edge. Held separately from _connectSourceId so a node-level connect in progress
    // and a port-level connect in progress can never be confused for one another.
    private string? _connectSourcePortNodeId;
    private string? _connectSourcePortId;

    // Highlight for the pending port-connect source — matches the node-level connect indicator.
    private const string PortConnectHighlightColor = "#06b6d4";

    // ── Private Helpers ───────────────────────────────────────────────────────────

    private ILogger ResolvedLogger => Logger ?? NullLogger<SvgCanvasRenderer>.Instance;
    private bool IsEditMode => Model.RenderMode.AllowsEditing && Model.EditContext is not null;
    private string Transform => $"translate({_panX:F1}, {_panY:F1}) scale({_scale:F3})";

    private static NodeAppearance GetNodeAppearance(ICanvasNode node) =>
        NodeAppearances.Get(node.NodeType.Name);

    private static EdgeAppearance GetEdgeAppearance(ICanvasEdge edge) =>
        EdgeAppearances.Get(edge.EdgeType.Name);

    private static string TruncateLabel(string label) =>
        label.Length > 14 ? string.Concat(label.AsSpan(0, 12), "..") : label;

    private static string TruncatePortLabel(string label) =>
        label.Length > 10 ? string.Concat(label.AsSpan(0, 8), "..") : label;

    private bool IsSelected(string id) =>
        _selectedId is not null && string.Equals(_selectedId, id, StringComparison.Ordinal);

    // ── Port Geometry ─────────────────────────────────────────────────────────────

    private static bool IsSelfLoop(ICanvasEdge edge) =>
        string.Equals(edge.SourceNodeId, edge.TargetNodeId, StringComparison.Ordinal);

    private IEnumerable<ICanvasEdge> SelfLoopEdgesFor(string nodeId) =>
        Model.Edges.Where(e => IsSelfLoop(e) && string.Equals(e.SourceNodeId, nodeId, StringComparison.Ordinal));

    /// <summary>
    /// Computes every node's port layout for this render pass, keyed by node id.
    /// </summary>
    /// <remarks>
    /// Rebuilt each pass rather than cached: the edit context mutates the model in place (AddNode,
    /// PopulateTransformPorts), so a cache keyed on node identity would silently serve stale port
    /// geometry after an edit. The work is a handful of arithmetic ops per port.
    /// </remarks>
    private Dictionary<string, NodePortLayout> BuildPortLayouts()
    {
        var layouts = new Dictionary<string, NodePortLayout>(Model.Nodes.Count, StringComparer.Ordinal);

        foreach (var node in Model.Nodes)
        {
            var layout = SvgPortGeometry.BuildLayout(node, GetNodeAppearance(node).PortAnchorHalfWidth);

            foreach (var port in layout.UnplaceablePorts)
                SvgCanvasRendererLog.PortDirectionNotRenderable(ResolvedLogger, node.Id, port.Id, port.Direction.Name);

            layouts[node.Id] = layout;
        }

        return layouts;
    }

    /// <summary>
    /// Resolves a node-to-node edge's endpoints in absolute canvas coordinates.
    /// </summary>
    /// <returns>The geometry, or null when an endpoint cannot be resolved (already reported).</returns>
    private EdgeGeometry? ResolveEdgeGeometry(ICanvasEdge edge, Dictionary<string, NodePortLayout> portLayouts)
    {
        var sourceNode = FindNode(edge.SourceNodeId);
        var targetNode = FindNode(edge.TargetNodeId);

        if (sourceNode is null || targetNode is null)
            return null;

        if (!portLayouts.TryGetValue(edge.SourceNodeId, out var sourceLayout)
            || !portLayouts.TryGetValue(edge.TargetNodeId, out var targetLayout))
        {
            return null;
        }

        if (!TryResolveEndpoint(sourceNode, sourceLayout, edge.SourcePortId, isSource: true, edge.Id, out var sx, out var sy))
            return null;

        if (!TryResolveEndpoint(targetNode, targetLayout, edge.TargetPortId, isSource: false, edge.Id, out var tx, out var ty))
            return null;

        return new EdgeGeometry(sx, sy, tx, ty);
    }

    private bool TryResolveEndpoint(
        ICanvasNode node,
        NodePortLayout layout,
        string? portId,
        bool isSource,
        string edgeId,
        out double x,
        out double y)
    {
        if (portId is null)
        {
            x = isSource ? node.X + SvgPortGeometry.NodeAnchorOffset : node.X - SvgPortGeometry.NodeAnchorOffset;
            y = node.Y;
            return true;
        }

        if (!layout.TryGetPlacement(portId, out var placement))
        {
            SvgCanvasRendererLog.PortAnchorUnresolvable(ResolvedLogger, edgeId, node.Id, portId);
            x = 0;
            y = 0;
            return false;
        }

        x = node.X + placement.Dx;
        y = node.Y + placement.Dy;
        return true;
    }

    /// <summary>
    /// Resolves a field-mapping self-loop edge's endpoints as offsets from its node's centre.
    /// </summary>
    /// <returns>The geometry, or null when a port anchor cannot be resolved (already reported).</returns>
    private EdgeGeometry? ResolveSelfLoopGeometry(ICanvasEdge edge, NodePortLayout layout, string nodeId)
    {
        if (edge.SourcePortId is null || edge.TargetPortId is null)
        {
            SvgCanvasRendererLog.SelfLoopEdgeMissingPorts(ResolvedLogger, edge.Id, nodeId);
            return null;
        }

        if (!layout.TryGetPlacement(edge.SourcePortId, out var source))
        {
            SvgCanvasRendererLog.PortAnchorUnresolvable(ResolvedLogger, edge.Id, nodeId, edge.SourcePortId);
            return null;
        }

        if (!layout.TryGetPlacement(edge.TargetPortId, out var target))
        {
            SvgCanvasRendererLog.PortAnchorUnresolvable(ResolvedLogger, edge.Id, nodeId, edge.TargetPortId);
            return null;
        }

        return new EdgeGeometry(source.Dx, source.Dy, target.Dx, target.Dy);
    }

    private bool IsPendingConnectSourcePort(string nodeId, string portId) =>
        _connectSourcePortNodeId is not null
        && _connectSourcePortId is not null
        && string.Equals(_connectSourcePortNodeId, nodeId, StringComparison.Ordinal)
        && string.Equals(_connectSourcePortId, portId, StringComparison.Ordinal);

    // ── Zoom/Pan ──────────────────────────────────────────────────────────────────

    /// <summary>Zooms in by one step.</summary>
    public void ZoomIn()
    {
        _scale = Math.Min(2.0, _scale + 0.1);
        StateHasChanged();
    }

    /// <summary>Zooms out by one step.</summary>
    public void ZoomOut()
    {
        _scale = Math.Max(0.3, _scale - 0.1);
        StateHasChanged();
    }

    /// <summary>Resets pan and zoom to initial values.</summary>
    public void ResetView()
    {
        _panX = 80;
        _panY = 80;
        _scale = 0.85;
        StateHasChanged();
    }

    // ── Mouse Handlers ────────────────────────────────────────────────────────────

    private void OnCanvasMouseDown(MouseEventArgs e)
    {
        if (_draggingNode is not null) return;
        _isPanning = true;
        _panStartX = e.ClientX;
        _panStartY = e.ClientY;
        _panOriginX = _panX;
        _panOriginY = _panY;
    }

    private void OnCanvasMouseMove(MouseEventArgs e)
    {
        if (_draggingNode is not null)
        {
            var newX = e.ClientX / _scale - _dragOffsetX;
            var newY = e.ClientY / _scale - _dragOffsetY;
            _draggingNode = PositionNode(_draggingNode, newX, newY);
            StateHasChanged();
            return;
        }

        if (_isPanning)
        {
            _panX = _panOriginX + (e.ClientX - _panStartX);
            _panY = _panOriginY + (e.ClientY - _panStartY);
            StateHasChanged();
        }
    }

    private async Task OnCanvasMouseUp(MouseEventArgs e)
    {
        if (_draggingNode is not null && IsEditMode && Model.EditContext is not null)
        {
            SvgCanvasRendererLog.MovingNode(ResolvedLogger, _draggingNode.Id, _draggingNode.X, _draggingNode.Y);
            var result = await Model.EditContext.MoveNode(
                _draggingNode.Id, _draggingNode.X, _draggingNode.Y,
                CancellationToken.None);

            if (result.IsSuccess)
                SvgCanvasRendererLog.NodeMoved(ResolvedLogger, _draggingNode.Id, _draggingNode.X, _draggingNode.Y);
            else
            {
                _editErrorMessage = result.CurrentMessage;
                SvgCanvasRendererLog.EditOperationFailed(ResolvedLogger, "MoveNode", result.CurrentMessage);
            }
        }

        _draggingNode = null;
        _isPanning = false;
    }

    private void OnCanvasWheel(WheelEventArgs e)
    {
        var delta = -Math.Sign(e.DeltaY) * 0.1;
        _scale = Math.Clamp(_scale + delta, 0.3, 2.0);
        StateHasChanged();
    }

    private void OnNodeMouseDown(ICanvasNode node, MouseEventArgs e)
    {
        _draggingNode = node;
        _dragOffsetX = e.ClientX / _scale - node.X;
        _dragOffsetY = e.ClientY / _scale - node.Y;
    }

    private void OnNodeClick(ICanvasNode node)
    {
        if (_connectSourceId is not null)
        {
            _ = ConnectNodes(_connectSourceId, node.Id);
            _connectSourceId = null;
            StateHasChanged();
            return;
        }

        // Toggle selection
        _selectedId = IsSelected(node.Id) ? null : node.Id;
        StateHasChanged();
    }

    private void OnEdgeClick(ICanvasEdge edge)
    {
        _selectedId = IsSelected(edge.Id) ? null : edge.Id;
        StateHasChanged();
    }

    private void OnPortClick(ICanvasNode node, ICanvasPort port)
    {
        if (!IsEditMode) return;

        if (IsPendingConnectSourcePort(node.Id, port.Id))
        {
            SvgCanvasRendererLog.PortConnectCancelled(ResolvedLogger, node.Id, port.Id);
            ClearPortConnect();
            StateHasChanged();
            return;
        }

        if (_connectSourcePortNodeId is null || _connectSourcePortId is null)
        {
            _connectSourcePortNodeId = node.Id;
            _connectSourcePortId = port.Id;
            SvgCanvasRendererLog.BeginningPortConnect(ResolvedLogger, node.Id, port.Id);
            StateHasChanged();
            return;
        }

        _ = ConnectPorts(_connectSourcePortNodeId, _connectSourcePortId, node.Id, port.Id);
        ClearPortConnect();
        StateHasChanged();
    }

    private void ClearPortConnect()
    {
        _connectSourcePortNodeId = null;
        _connectSourcePortId = null;
    }

    // ── Edit-Context Operations ───────────────────────────────────────────────────

    private async Task DeleteSelected()
    {
        if (_selectedId is null || !IsEditMode || Model.EditContext is null) return;

        var nodeMatch = Model.Nodes.FirstOrDefault(n => string.Equals(n.Id, _selectedId, StringComparison.Ordinal));
        if (nodeMatch is not null)
        {
            SvgCanvasRendererLog.DeletingSelection(ResolvedLogger, nodeMatch.Id, "node");
            var result = await Model.EditContext.DeleteNode(nodeMatch.Id, CancellationToken.None);
            if (result.IsSuccess)
                SvgCanvasRendererLog.SelectionDeleted(ResolvedLogger, nodeMatch.Id, "node");
            else
            {
                _editErrorMessage = result.CurrentMessage;
                SvgCanvasRendererLog.EditOperationFailed(ResolvedLogger, "DeleteNode", result.CurrentMessage);
            }
        }
        else
        {
            var edgeMatch = Model.Edges.FirstOrDefault(e => string.Equals(e.Id, _selectedId, StringComparison.Ordinal));
            if (edgeMatch is not null)
            {
                SvgCanvasRendererLog.DeletingSelection(ResolvedLogger, edgeMatch.Id, "edge");
                var result = await Model.EditContext.DeleteEdge(edgeMatch.Id, CancellationToken.None);
                if (result.IsSuccess)
                    SvgCanvasRendererLog.SelectionDeleted(ResolvedLogger, edgeMatch.Id, "edge");
                else
                {
                    _editErrorMessage = result.CurrentMessage;
                    SvgCanvasRendererLog.EditOperationFailed(ResolvedLogger, "DeleteEdge", result.CurrentMessage);
                }
            }
        }

        _selectedId = null;
        StateHasChanged();
    }

    private void BeginConnect()
    {
        _connectSourceId = _selectedId;
        StateHasChanged();
    }

    private async Task ConnectNodes(string sourceId, string targetId)
    {
        if (Model.EditContext is null) return;

        var edgeType = Fdw.UI.Abstractions.Canvas.CanvasEdgeTypes.ByName("Flow");
        if (edgeType == Fdw.UI.Abstractions.Canvas.CanvasEdgeTypes.NotFound)
        {
            SvgCanvasRendererLog.FlowEdgeTypeNotRegistered(ResolvedLogger);
            _editErrorMessage = "Flow edge type is not registered.";
            return;
        }

        SvgCanvasRendererLog.ConnectingNodes(ResolvedLogger, sourceId, targetId);
        var result = await Model.EditContext.Connect(
            sourceId, targetId, edgeType,
            sourcePortId: null, targetPortId: null,
            CancellationToken.None);

        if (result.IsSuccess)
            SvgCanvasRendererLog.NodesConnected(ResolvedLogger, sourceId, targetId);
        else
        {
            _editErrorMessage = result.CurrentMessage;
            SvgCanvasRendererLog.EditOperationFailed(ResolvedLogger, "Connect", result.CurrentMessage);
        }

        StateHasChanged();
    }

    private async Task ConnectPorts(string sourceNodeId, string sourcePortId, string targetNodeId, string targetPortId)
    {
        if (Model.EditContext is null) return;

        var edgeType = Fdw.UI.Abstractions.Canvas.CanvasEdgeTypes.ByName("FieldMapping");
        if (edgeType == Fdw.UI.Abstractions.Canvas.CanvasEdgeTypes.NotFound)
        {
            SvgCanvasRendererLog.FieldMappingEdgeTypeNotRegistered(ResolvedLogger);
            _editErrorMessage = "FieldMapping edge type is not registered.";
            return;
        }

        SvgCanvasRendererLog.ConnectingPorts(ResolvedLogger, sourceNodeId, sourcePortId, targetNodeId, targetPortId);
        var result = await Model.EditContext.Connect(
            sourceNodeId, targetNodeId, edgeType,
            sourcePortId, targetPortId,
            CancellationToken.None);

        if (result.IsSuccess)
            SvgCanvasRendererLog.PortsConnected(ResolvedLogger, sourceNodeId, sourcePortId, targetNodeId, targetPortId);
        else
        {
            _editErrorMessage = result.CurrentMessage;
            SvgCanvasRendererLog.EditOperationFailed(ResolvedLogger, "ConnectPorts", result.CurrentMessage);
        }

        StateHasChanged();
    }

    // ── Helper — immutable-record node position update ────────────────────────────

    private static PositionedNode PositionNode(ICanvasNode node, double x, double y) =>
        new(node, x, y);

    /// <summary>
    /// Transient wrapper that overrides X/Y during a drag preview without mutating the domain model.
    /// </summary>
    private sealed class PositionedNode : ICanvasNode
    {
        private readonly ICanvasNode _inner;
        public PositionedNode(ICanvasNode inner, double x, double y)
        {
            _inner = inner;
            X = x;
            Y = y;
        }

        public string Id => _inner.Id;
        public ICanvasNodeType NodeType => _inner.NodeType;
        public string Label => _inner.Label;
        public string? SubLabel => _inner.SubLabel;
        public string? Status => _inner.Status;
        public double X { get; }
        public double Y { get; }
        public System.Collections.Generic.IReadOnlyList<ICanvasPort> Ports => _inner.Ports;
        public System.Collections.Generic.IReadOnlyDictionary<string, string> Metadata => _inner.Metadata;
    }
}
