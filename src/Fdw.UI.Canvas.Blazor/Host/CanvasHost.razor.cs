using System;
using System.Collections.Generic;
using System.Linq;
using Fdw.UI.Abstractions.Canvas;
using Fdw.UI.Canvas.Blazor.Logging;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.UI.Canvas.Blazor.Host;

/// <summary>
/// Hosts an <see cref="ICanvasModel"/> in a Blazor page by selecting and rendering
/// the appropriate Blazor canvas renderer via a runtime dropdown.
/// </summary>
/// <remarks>
/// <para>
/// The dropdown is populated from <see cref="CanvasRendererTypes.All()"/>. The default
/// renderer is the first entry with <c>SupportsEditing = true</c> when the model's render
/// mode allows editing, otherwise the first registered renderer.
/// </para>
/// <para>
/// The selected renderer is rendered via <c>DynamicComponent</c>. Switching the dropdown
/// keeps the same <see cref="ICanvasModel"/> instance so all in-model state is preserved
/// across renderer switches.
/// </para>
/// <para>
/// No <c>@page</c> directive — this is a headless host component, not a routable page.
/// </para>
/// </remarks>
[System.Diagnostics.CodeAnalysis.SuppressMessage("Meziantou.Analyzer", "MA0004",
    Justification = "Blazor UI event handlers run on sync context")]
public sealed partial class CanvasHost : ComponentBase
{
    // ── Parameters ────────────────────────────────────────────────────────────────

    /// <summary>
    /// Gets or sets the canvas model to display. Required.
    /// </summary>
    [Parameter, EditorRequired]
    public ICanvasModel Model { get; set; } = default!;

    /// <summary>
    /// Gets or sets the optional logger. Falls back to <see cref="NullLogger{T}.Instance"/>.
    /// </summary>
    // Why: NullLogger fallback is the only acceptable ?? fallback per FDW conventions.
    [Parameter]
    public ILogger? Logger { get; set; }

    // ── Private State ─────────────────────────────────────────────────────────────

    private string _selectedRendererName = string.Empty;
    private string? _errorMessage;

    // Why: cache the renderers list so OnParametersSet does not re-allocate on every render.
    private List<ICanvasRendererType> _renderers = [];

    // Why: DynamicComponent parameters must be Dictionary<string,object?>; rebuild only when
    // selectedRendererName changes so we are not allocating a new dictionary every render.
    private Dictionary<string, object?> _dynamicParams = new(StringComparer.Ordinal);

    private ILogger ResolvedLogger => Logger ?? NullLogger<CanvasHost>.Instance;

    // ── Lifecycle ─────────────────────────────────────────────────────────────────

    /// <inheritdoc />
    protected override void OnParametersSet()
    {
        // Why: rebuild the renderer list each time Model changes so the dropdown reflects
        // the current set of registered renderers (which may have changed between navigations
        // if additional renderer assemblies were loaded dynamically). Order by descriptor Id so
        // dropdown order AND the default-renderer choice are deterministic regardless of assembly
        // /registration order. The built-in SVG renderer (Id 1) is dependency-free (pure markup,
        // no JS/DOM), so it sorts first and is the safe default for SSR/prerender; richer JS-backed
        // renderers (Diagrams, Cytoscape) are opt-in via the dropdown once the circuit is live.
        _renderers = CanvasRendererTypes.All().OrderBy(r => r.Id).ToList();

        // Why: fatal — no registered renderers means the host has nothing to render with.
        if (_renderers.Count == 0)
            CanvasHostLog.NoRenderersRegistered(ResolvedLogger);

        if (string.IsNullOrEmpty(_selectedRendererName))
            _selectedRendererName = ChooseDefaultRenderer();

        CanvasHostLog.RenderingCanvas(
            ResolvedLogger, Model.Title, Model.Nodes.Count, Model.Edges.Count, _selectedRendererName);

        RebuildDynamicParams();
    }

    // ── Private Helpers ───────────────────────────────────────────────────────────

    private string ChooseDefaultRenderer()
    {
        if (_renderers.Count == 0) return string.Empty;

        // Why: prefer a renderer that supports editing when the model is in edit mode so that
        // the canvas is immediately usable — no manual dropdown change needed.
        var preferEdit = Model.RenderMode.AllowsEditing;
        var preferred = preferEdit
            ? _renderers.FirstOrDefault(r => r.SupportsEditing)
            : null;

        var chosen = (preferred ?? _renderers[0]).Name;
        CanvasHostLog.DefaultRendererChosen(ResolvedLogger, chosen, preferEdit, _renderers.Count);
        return chosen;
    }

    // Why: param is nullable to match the @bind:set Action<string?> delegate; a null/empty
    // selection is a no-op (the dropdown only ever emits registered renderer names).
    private void OnRendererChanged(string? newName)
    {
        if (string.IsNullOrEmpty(newName)) return;
        if (string.Equals(_selectedRendererName, newName, StringComparison.Ordinal)) return;

        // Why: validate the new name resolves to a renderer with a Blazor component — fail loud
        // (no silent fallback) if the descriptor is missing or has no component type.
        if (ResolveComponentType(newName) is null)
        {
            _errorMessage = $"No Blazor component registered for renderer '{newName}'.";
            CanvasHostLog.RendererNotRegistered(ResolvedLogger, newName);
            return;
        }

        _errorMessage = null;
        _selectedRendererName = newName;
        CanvasHostLog.RendererChanged(ResolvedLogger, newName);
        RebuildDynamicParams();
    }

    private void RebuildDynamicParams()
    {
        // Why: DynamicComponent requires the parameter dictionary to contain the actual
        // parameter values keyed by parameter name (exact case). Pass Model so the renderer
        // component receives the same ICanvasModel instance across renderer switches.
        _dynamicParams = new Dictionary<string, object?>(StringComparer.Ordinal)
        {
            ["Model"] = Model,
        };
    }

    private Type? ActiveComponentType =>
        string.IsNullOrEmpty(_selectedRendererName) ? null : ResolveComponentType(_selectedRendererName);

    // Why: resolve the Blazor component straight from the enumerable CanvasRendererTypes registry
    // (the descriptor carries its RenderComponentType) — no separate map, no reflection.
    // ByName returns the NotFound sentinel for an unknown name (never null) — treat that as unresolved.
    private Type? ResolveComponentType(string rendererName)
    {
        CanvasHostLog.ResolvingRendererComponent(ResolvedLogger, rendererName);
        var descriptor = CanvasRendererTypes.ByName(rendererName);
        return descriptor == CanvasRendererTypes.NotFound ? null : descriptor.RenderComponentType;
    }
}
