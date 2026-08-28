using System.Collections.Generic;
using System.Threading.Tasks;
using Blazor.Diagrams;
using Blazor.Diagrams.Components;
using Blazor.Diagrams.Core.Behaviors;
using Blazor.Diagrams.Core.Geometry;
using Blazor.Diagrams.Core.Models;
using Blazor.Diagrams.Options;
using Fdw.UI.Abstractions.Canvas;
using Fdw.UI.Canvas.Blazor.Logging;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.UI.Canvas.Blazor.Renderers.NodeGraph;

/// <summary>
/// Interactive node-graph canvas renderer backed by <c>Z.Blazor.Diagrams</c>.
/// </summary>
/// <remarks>
/// <para>
/// Translates an <see cref="ICanvasModel"/> into a <c>BlazorDiagram</c> instance:
/// each <see cref="ICanvasNode"/> becomes a <c>NodeModel</c> placed at
/// (<see cref="ICanvasNode.X"/>, <see cref="ICanvasNode.Y"/>); each <see cref="ICanvasEdge"/>
/// becomes a <c>LinkModel</c> connecting the corresponding node models.
/// </para>
/// <para>
/// When <see cref="ICanvasModel.RenderMode"/> has <c>AllowsEditing = false</c>, all nodes are
/// locked (preventing drag) and the link-drag behavior is unregistered so users cannot draw new
/// connections.  Pan and zoom are always enabled.
/// </para>
/// <para>
/// CSS: the consuming application must include
/// <c>_content/Z.Blazor.Diagrams/style.min.css</c> in its host page or layout — the renderer
/// does not inject it automatically.
/// </para>
/// </remarks>
[System.Diagnostics.CodeAnalysis.SuppressMessage("Meziantou.Analyzer", "MA0004",
    Justification = "Blazor lifecycle methods run on the sync rendering context")]
public sealed partial class BlazorDiagramsRenderer : ComponentBase
{
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

    // ── Private state ─────────────────────────────────────────────────────────────

    private BlazorDiagram _diagram = null!;

    private ICanvasModel? _lastModel;

    // ── Helpers ───────────────────────────────────────────────────────────────────

    private ILogger ResolvedLogger => Logger ?? NullLogger<BlazorDiagramsRenderer>.Instance;

    private RenderFragment RenderDiagramCanvas => builder =>
    {
        builder.OpenComponent<CascadingValue<BlazorDiagram>>(0);
        builder.AddAttribute(1, "Value", _diagram);
        builder.AddAttribute(2, "ChildContent", (RenderFragment)(inner =>
        {
            inner.OpenComponent<DiagramCanvas>(0);
            inner.CloseComponent();
        }));
        builder.CloseComponent();
    };

    // ── Lifecycle ─────────────────────────────────────────────────────────────────

    /// <inheritdoc />
    protected override void OnParametersSet()
    {
        if (ReferenceEquals(Model, _lastModel))
            return;

        _lastModel = Model;
        _diagram = BuildDiagram(Model);

        BlazorDiagramsRendererLog.DiagramRebuilt(ResolvedLogger);
    }

    // ── Build ─────────────────────────────────────────────────────────────────────

    internal BlazorDiagram BuildDiagram(ICanvasModel model)
    {
        BlazorDiagramsRendererLog.Building(ResolvedLogger, model.Nodes.Count, model.Edges.Count);

        var options = new BlazorDiagramOptions
        {
            AllowMultiSelection = model.RenderMode.AllowsEditing,
            AllowPanning = true,
        };

        if (!model.RenderMode.AllowsEditing)
        {
            options.Constraints.ShouldDeleteNode = _ => ValueTask.FromResult(false);
            options.Constraints.ShouldDeleteLink = _ => ValueTask.FromResult(false);
            options.Constraints.ShouldDeleteGroup = _ => ValueTask.FromResult(false);
        }

        var diagram = new BlazorDiagram(options);

        if (!model.RenderMode.AllowsEditing)
            diagram.UnregisterBehavior<DragNewLinkBehavior>();

        // Build a node map keyed by ICanvasNode.Id for O(1) edge resolution.
        var nodeMap = new Dictionary<string, NodeModel>(model.Nodes.Count, System.StringComparer.Ordinal);

        foreach (var node in model.Nodes)
        {
            var nodeModel = new NodeModel(node.Id, new Point(node.X, node.Y))
            {
                Title = node.Label,
                Locked = !model.RenderMode.AllowsEditing,
            };
            diagram.Nodes.Add(nodeModel);
            nodeMap[node.Id] = nodeModel;
        }

        foreach (var edge in model.Edges)
        {
            if (!nodeMap.TryGetValue(edge.SourceNodeId, out var sourceNode) ||
                !nodeMap.TryGetValue(edge.TargetNodeId, out var targetNode))
            {
                BlazorDiagramsRendererLog.EdgeNodeNotFound(
                    ResolvedLogger, edge.Id, edge.SourceNodeId, edge.TargetNodeId);
                continue;
            }

            var link = new LinkModel(edge.Id, sourceNode, targetNode);
            diagram.Links.Add(link);
        }

        BlazorDiagramsRendererLog.Built(ResolvedLogger, model.Nodes.Count, model.Edges.Count);
        return diagram;
    }
}
