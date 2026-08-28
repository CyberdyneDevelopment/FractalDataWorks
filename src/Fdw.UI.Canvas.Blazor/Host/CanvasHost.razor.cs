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
    [Parameter]
    public ILogger? Logger { get; set; }

    // ── Private State ─────────────────────────────────────────────────────────────

    private string _selectedRendererName = string.Empty;
    private string? _errorMessage;

    private List<ICanvasRendererType> _renderers = [];

    private Dictionary<string, object?> _dynamicParams = new(StringComparer.Ordinal);

    private ILogger ResolvedLogger => Logger ?? NullLogger<CanvasHost>.Instance;

    // ── Lifecycle ─────────────────────────────────────────────────────────────────

    /// <inheritdoc />
    protected override void OnParametersSet()
    {
        _renderers = CanvasRendererTypes.All().OrderBy(r => r.Id).ToList();

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

        var preferEdit = Model.RenderMode.AllowsEditing;
        var preferred = preferEdit
            ? _renderers.FirstOrDefault(r => r.SupportsEditing)
            : null;

        var chosen = (preferred ?? _renderers[0]).Name;
        CanvasHostLog.DefaultRendererChosen(ResolvedLogger, chosen, preferEdit, _renderers.Count);
        return chosen;
    }

    private void OnRendererChanged(string? newName)
    {
        if (string.IsNullOrEmpty(newName)) return;
        if (string.Equals(_selectedRendererName, newName, StringComparison.Ordinal)) return;

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
        _dynamicParams = new Dictionary<string, object?>(StringComparer.Ordinal)
        {
            ["Model"] = Model,
        };
    }

    private Type? ActiveComponentType =>
        string.IsNullOrEmpty(_selectedRendererName) ? null : ResolveComponentType(_selectedRendererName);

    private Type? ResolveComponentType(string rendererName)
    {
        CanvasHostLog.ResolvingRendererComponent(ResolvedLogger, rendererName);
        var descriptor = CanvasRendererTypes.ByName(rendererName);
        return descriptor == CanvasRendererTypes.NotFound ? null : descriptor.RenderComponentType;
    }
}
