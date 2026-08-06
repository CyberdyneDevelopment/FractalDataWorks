using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Fdw.UI.Abstractions.Canvas;
using Fdw.UI.Canvas.Blazor.Logging;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.JSInterop;

namespace Fdw.UI.Canvas.Blazor.Renderers.Cytoscape;

/// <summary>
/// Cytoscape.js canvas renderer for the FDW render-agnostic canvas.
/// </summary>
/// <remarks>
/// <para>
/// Renders an <see cref="ICanvasModel"/> using Cytoscape.js via JS interop. The vendored
/// UMD script is injected on first render if <c>window.cytoscape</c> is not already present.
/// </para>
/// <para>
/// Supports large graphs (<see cref="ICanvasRendererType.SupportsLargeGraphs"/> is <c>true</c>).
/// Layout defaults to <c>preset</c> when nodes carry explicit positions; falls back to
/// <c>breadthfirst</c> when all positions are zero.
/// </para>
/// <para>
/// This component is mounted exclusively by <c>CanvasHost</c> via <c>DynamicComponent</c>.
/// No <c>@page</c> directive — it is not a routable page.
/// </para>
/// </remarks>
[System.Diagnostics.CodeAnalysis.SuppressMessage("Meziantou.Analyzer", "MA0004",
    Justification = "Blazor UI component lifecycle methods run on sync context")]
public sealed partial class CytoscapeRenderer : ComponentBase, IAsyncDisposable
{
    // ── Injected Services ─────────────────────────────────────────────────────────

    [Inject]
    private IJSRuntime JS { get; set; } = default!;

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

    // ── Private State ─────────────────────────────────────────────────────────────

    private ElementReference _el;
    private IJSObjectReference? _module;
    private bool _disposed;

    // ── Private Helpers ───────────────────────────────────────────────────────────

    private ILogger ResolvedLogger => Logger ?? NullLogger<CytoscapeRenderer>.Instance;

    // ── Blazor Lifecycle ──────────────────────────────────────────────────────────

    /// <inheritdoc />
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (_disposed) return;

        if (firstRender)
        {
            CytoscapeRendererLog.LoadingModule(ResolvedLogger);
            _module = await JS.InvokeAsync<IJSObjectReference>(
                "import",
                "./_content/Fdw.UI.Canvas.Blazor/js/cytoscape-interop.js");
            CytoscapeRendererLog.ModuleLoaded(ResolvedLogger);
        }

        if (_module is null) return;

        await RenderGraph();
    }

    // ── Graph Rendering ───────────────────────────────────────────────────────────

    private async Task RenderGraph()
    {
        if (_module is null || Model is null) return;

        var layoutName = SelectLayout();

        CytoscapeRendererLog.RenderingGraph(
            ResolvedLogger, Model.Nodes.Count, Model.Edges.Count, layoutName);

        try
        {
            await _module.InvokeVoidAsync("render", _el, BuildElements(), layoutName);
            CytoscapeRendererLog.GraphRendered(ResolvedLogger, Model.Nodes.Count, Model.Edges.Count);
        }
        catch (JSException ex)
        {
            CytoscapeRendererLog.RenderFailed(ResolvedLogger, ex, ex.Message);
        }
        catch (TaskCanceledException ex)
        {
            // Why: cancellation during render (e.g. component disposed mid-render) — observe at Trace, no state change.
            CytoscapeRendererLog.TeardownInterrupted(ResolvedLogger, ex);
        }
    }

    private List<object> BuildElements()
    {
        var elements = new List<object>(Model.Nodes.Count + Model.Edges.Count);

        foreach (var node in Model.Nodes)
        {
            elements.Add(new
            {
                data = new
                {
                    id = node.Id,
                    label = node.Label,
                },
                position = new
                {
                    x = node.X,
                    y = node.Y,
                },
            });
        }

        foreach (var edge in Model.Edges)
        {
            elements.Add(new
            {
                data = new
                {
                    id = edge.Id,
                    source = edge.SourceNodeId,
                    target = edge.TargetNodeId,
                    label = edge.Label,
                },
            });
        }

        return elements;
    }

    private string SelectLayout()
    {
        // Why: honour the model's layout hint when provided rather than overriding with a positional guess.
        if (Model.LayoutHint is not null)
            return Model.LayoutHint;

        // Why: use "preset" when at least one node has a non-origin position so Cytoscape respects
        // the domain-assigned coordinates. Fall back to "breadthfirst" when all positions are zero
        // (no explicit layout has been set) so Cytoscape auto-arranges the nodes sensibly.
        return Model.Nodes.Any(n => n.X != 0.0 || n.Y != 0.0) ? "preset" : "breadthfirst";
    }

    // ── IAsyncDisposable ──────────────────────────────────────────────────────────

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        if (_disposed) return;
        _disposed = true;

        if (_module is not null)
        {
            try
            {
                await _module.InvokeVoidAsync("dispose", _el);
            }
            catch (JSDisconnectedException ex)
            {
                // Why: the JS runtime has already disconnected (e.g. page unload) — dispose is best-effort, observe at Trace.
                CytoscapeRendererLog.TeardownInterrupted(ResolvedLogger, ex);
            }
            catch (TaskCanceledException ex)
            {
                // Why: cancellation during dispose — observe at Trace.
                CytoscapeRendererLog.TeardownInterrupted(ResolvedLogger, ex);
            }

            await _module.DisposeAsync();
            _module = null;
        }
    }
}
