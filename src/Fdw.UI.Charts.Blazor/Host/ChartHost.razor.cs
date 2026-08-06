using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Fdw.UI.Abstractions.Charts;
using Fdw.UI.Charts.Blazor.Logging;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.UI.Charts.Blazor.Host;

/// <summary>
/// Hosts an <see cref="IChartModel"/> in a Blazor page by selecting and rendering the
/// appropriate Blazor chart renderer via a runtime dropdown, with a second dropdown to switch
/// chart type filtered to the active renderer's supported types.
/// </summary>
/// <remarks>
/// <para>
/// The renderer dropdown is populated from <see cref="ChartRendererTypes.All()"/>. The default
/// renderer is the first registered renderer.
/// </para>
/// <para>
/// The chart-type dropdown is filtered to the selected renderer's
/// <see cref="IChartRendererType.SupportedChartTypes"/> — an empty list means "all registered
/// types". The intersection is computed from <see cref="ChartTypes.All()"/> each time the
/// selected renderer changes.
/// </para>
/// <para>
/// The selected renderer is rendered via <c>DynamicComponent</c>. Switching the renderer
/// dropdown re-filters the chart-type list and keeps the same <see cref="IChartModel"/>
/// instance so all in-model state is preserved. Switching the chart-type dropdown raises
/// <see cref="ChartTypeChanged"/> for the caller to update the model.
/// </para>
/// <para>
/// No <c>@page</c> directive — this is a headless host component, not a routable page.
/// </para>
/// </remarks>
[SuppressMessage("Meziantou.Analyzer", "MA0004",
    Justification = "Blazor UI event handlers run on sync context")]
public sealed partial class ChartHost : ComponentBase
{
    // ── Parameters ────────────────────────────────────────────────────────────────

    /// <summary>
    /// Gets or sets the chart model to display. Required.
    /// </summary>
    [Parameter, EditorRequired]
    public IChartModel Model { get; set; } = default!;

    /// <summary>
    /// Gets or sets the data rows to pass to the renderer.
    /// </summary>
    [Parameter]
    public IReadOnlyList<IReadOnlyDictionary<string, object?>> Rows { get; set; } = [];

    /// <summary>
    /// Gets or sets the callback raised when the user changes the chart type.
    /// </summary>
    /// <remarks>
    /// Why: <see cref="IChartModel.ChartType"/> is read-only on the interface — the host
    /// cannot mutate it directly. Instead it raises this callback so the caller updates
    /// the model and triggers a re-render via normal Blazor parameter flow.
    /// </remarks>
    [Parameter]
    public EventCallback<IChartType> ChartTypeChanged { get; set; }

    /// <summary>
    /// Gets or sets the optional logger. Falls back to <see cref="NullLogger{T}.Instance"/>.
    /// </summary>
    // Why: NullLogger fallback is the only acceptable ?? fallback per FDW conventions.
    [Parameter]
    public ILogger? Logger { get; set; }

    // ── Private State ─────────────────────────────────────────────────────────────

    private string _selectedRendererName = string.Empty;
    private string _selectedChartTypeName = string.Empty;
    private string? _errorMessage;

    // Why: cache lists so OnParametersSet does not re-allocate on every render.
    private List<IChartRendererType> _renderers = [];
    private List<IChartType> _compatibleChartTypes = [];

    // Why: DynamicComponent parameters must be Dictionary<string,object?>; rebuild only when
    // selectedRendererName or model changes to avoid allocating a new dictionary every render.
    private Dictionary<string, object?> _dynamicParams = new(StringComparer.Ordinal);

    private ILogger ResolvedLogger => Logger ?? NullLogger<ChartHost>.Instance;

    // ── Lifecycle ─────────────────────────────────────────────────────────────────

    /// <inheritdoc />
    protected override void OnParametersSet()
    {
        // Why: rebuild the renderer list each time parameters change so the dropdown reflects
        // any dynamically-loaded renderer assemblies.
        _renderers = ChartRendererTypes.All().ToList();

        // Why: fatal — no registered renderers means the host has nothing to render with.
        if (_renderers.Count == 0)
            ChartHostLog.NoRenderersRegistered(ResolvedLogger);

        if (string.IsNullOrEmpty(_selectedRendererName))
            _selectedRendererName = ChooseDefaultRenderer();

        // Synchronise chart-type dropdown to match the current model's chart type.
        RebuildCompatibleChartTypes(_selectedRendererName);

        if (string.IsNullOrEmpty(_selectedChartTypeName) && Model is not null)
            _selectedChartTypeName = Model.ChartType.Name;

        if (Model is not null)
            ChartHostLog.RenderingChart(
                ResolvedLogger, Model.Title, Model.ChartType.Name, _selectedRendererName);

        RebuildDynamicParams();
    }

    // ── Private Helpers ───────────────────────────────────────────────────────────

    private string ChooseDefaultRenderer()
    {
        if (_renderers.Count == 0) return string.Empty;

        var chosen = _renderers[0].Name;
        ChartHostLog.DefaultRendererChosen(ResolvedLogger, chosen, _renderers.Count);
        return chosen;
    }

    // Why: re-compute the compatible chart type list each time the renderer selection changes
    // so the chart-type dropdown always reflects what the current renderer supports.
    private void RebuildCompatibleChartTypes(string rendererName)
    {
        if (string.IsNullOrEmpty(rendererName))
        {
            _compatibleChartTypes = [];
            return;
        }

        var descriptor = ChartRendererTypes.ByName(rendererName);
        if (descriptor == ChartRendererTypes.NotFound)
        {
            _compatibleChartTypes = [];
            return;
        }

        var all = ChartTypes.All();

        if (descriptor.SupportedChartTypes.Count == 0)
        {
            // Why: empty SupportedChartTypes means "supports all" — no filtering needed.
            _compatibleChartTypes = all.ToList();
            return;
        }

        // Why: filter uses Ordinal comparison (registry names are case-sensitive by convention).
        _compatibleChartTypes = all
            .Where(ct => descriptor.SupportedChartTypes.Contains(ct.Name, StringComparer.Ordinal))
            .ToList();
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
            ChartHostLog.RendererNotRegistered(ResolvedLogger, newName);
            return;
        }

        _errorMessage = null;
        _selectedRendererName = newName;
        ChartHostLog.RendererChanged(ResolvedLogger, newName);

        // Recompute compatible chart types for the new renderer.
        RebuildCompatibleChartTypes(newName);

        // If the currently-selected chart type is not in the new compatible list, reset to first.
        var isStillCompatible = _compatibleChartTypes.Any(
            ct => string.Equals(ct.Name, _selectedChartTypeName, StringComparison.Ordinal));
        if (!isStillCompatible && _compatibleChartTypes.Count > 0)
            _selectedChartTypeName = _compatibleChartTypes[0].Name;

        RebuildDynamicParams();
    }

    private void OnChartTypeChanged(string? newName)
    {
        if (string.IsNullOrEmpty(newName)) return;
        if (string.Equals(_selectedChartTypeName, newName, StringComparison.Ordinal)) return;

        var chartType = ChartTypes.ByName(newName);
        if (chartType == ChartTypes.NotFound) return;

        _selectedChartTypeName = newName;
        ChartHostLog.ChartTypeChanged(ResolvedLogger, newName, _selectedRendererName);
        RebuildDynamicParams();

        // Notify caller so it can update Model.ChartType (interface is read-only).
        _ = ChartTypeChanged.InvokeAsync(chartType);
    }

    private void RebuildDynamicParams()
    {
        // Why: DynamicComponent requires the parameter dictionary to contain the actual
        // parameter values keyed by parameter name (exact case). Pass Model + Rows so the
        // renderer component receives both the spec and the data to plot.
        _dynamicParams = new Dictionary<string, object?>(StringComparer.Ordinal)
        {
            ["Model"]  = Model,
            ["Rows"]   = Rows,
            ["Logger"] = ResolvedLogger,
        };
    }

    private Type? ActiveComponentType =>
        string.IsNullOrEmpty(_selectedRendererName)
            ? null
            : ResolveComponentType(_selectedRendererName);

    // Why: resolve the Blazor component straight from the enumerable ChartRendererTypes registry
    // (the descriptor carries its RenderComponentType) — no separate map, no reflection.
    // ByName returns the NotFound sentinel for an unknown name (never null).
    private Type? ResolveComponentType(string rendererName)
    {
        ChartHostLog.ResolvingRendererComponent(ResolvedLogger, rendererName);
        var descriptor = ChartRendererTypes.ByName(rendererName);
        return descriptor == ChartRendererTypes.NotFound ? null : descriptor.RenderComponentType;
    }
}
