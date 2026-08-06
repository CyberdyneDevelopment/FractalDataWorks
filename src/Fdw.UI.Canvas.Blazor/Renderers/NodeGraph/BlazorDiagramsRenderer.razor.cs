using System.Collections.Generic;
using System.Threading.Tasks;
// Why: these vendor usings sit BEFORE the namespace declaration, so they resolve in the global
// namespace where the vendor's top-level "Blazor" wins — not the package's own
// Fdw.UI.Canvas.Blazor. A namespace-scoped razor @using cannot do this, which is why
// the <DiagramCanvas> component is emitted from here via RenderTreeBuilder rather than as markup.
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
    // Why: NullLogger fallback is the only acceptable ?? fallback per FDW conventions.
    [Parameter]
    public ILogger? Logger { get; set; }

    // ── Private state ─────────────────────────────────────────────────────────────

    private BlazorDiagram _diagram = null!;

    // Why: track the previous model reference so we only rebuild the BlazorDiagram when the
    // model instance actually changes — avoids unnecessary teardown/reconstruction on re-renders
    // triggered by parent state changes that don't affect the canvas model.
    private ICanvasModel? _lastModel;

    // ── Helpers ───────────────────────────────────────────────────────────────────

    private ILogger ResolvedLogger => Logger ?? NullLogger<BlazorDiagramsRenderer>.Instance;

    // Why: emit the vendor CascadingValue<BlazorDiagram> + DiagramCanvas from code rather than as
    // markup tags so the razor never needs a vendor @using (which would bind "Blazor" to this
    // package). DiagramCanvas reads the BlazorDiagram from the cascading value via its internal
    // [CascadingParameter]. Sequence numbers are constant per Blazor RenderTreeBuilder rules.
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

    // Why: internal (not private) so the mapping logic — nodes/edges → vendor models, orphan-edge
    // skip, lock-in-view-mode — can be unit-tested without mounting the vendor DiagramCanvas, which
    // requires a real browser DOM (ResizeObserver + getBoundingClientRect) and cannot render under
    // bUnit. Live DiagramCanvas rendering is covered by the Playwright E2E suite.
    internal BlazorDiagram BuildDiagram(ICanvasModel model)
    {
        BlazorDiagramsRendererLog.Building(ResolvedLogger, model.Nodes.Count, model.Edges.Count);

        var options = new BlazorDiagramOptions
        {
            AllowMultiSelection = model.RenderMode.AllowsEditing,
            AllowPanning = true,
        };

        // Why: prevent structural modifications in view mode — nodes stay locked in place
        // and deletion is blocked via the constraints funcs. ZBD v3 constraints are async
        // (Func<T, ValueTask<bool>>), so the deny-all predicate returns a completed ValueTask.
        if (!model.RenderMode.AllowsEditing)
        {
            options.Constraints.ShouldDeleteNode = _ => ValueTask.FromResult(false);
            options.Constraints.ShouldDeleteLink = _ => ValueTask.FromResult(false);
            options.Constraints.ShouldDeleteGroup = _ => ValueTask.FromResult(false);
        }

        var diagram = new BlazorDiagram(options);

        // Why: in read-only mode, unregister the default drag-new-link behavior so users
        // cannot accidentally draw connections when they intend to pan the canvas.
        if (!model.RenderMode.AllowsEditing)
            diagram.UnregisterBehavior<DragNewLinkBehavior>();

        // Build a node map keyed by ICanvasNode.Id for O(1) edge resolution.
        var nodeMap = new Dictionary<string, NodeModel>(model.Nodes.Count, System.StringComparer.Ordinal);

        foreach (var node in model.Nodes)
        {
            var nodeModel = new NodeModel(node.Id, new Point(node.X, node.Y))
            {
                Title = node.Label,
                // Why: lock nodes in read mode so they cannot be dragged;
                // leave unlocked in edit mode to preserve ZBD's default move behavior.
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
                // Why: warn but continue — a missing node reference means the edge is
                // orphaned in the model; skipping it keeps the diagram renderable.
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
