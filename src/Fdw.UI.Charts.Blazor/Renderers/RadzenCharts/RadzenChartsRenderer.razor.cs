using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Fdw.UI.Abstractions.Charts;
using Fdw.UI.Charts.Blazor.Logging;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.UI.Charts.Blazor.Renderers.RadzenCharts;

/// <summary>
/// Blazor renderer component for Radzen charts.
/// </summary>
/// <remarks>
/// <para>
/// Renders the <see cref="Model"/> spec against the provided <see cref="Rows"/> using the
/// Radzen Blazor chart library. Chart-type dispatch is data-driven through
/// <see cref="RadzenChartStrategyMap"/> — no switch/if-else chain on the chart type name (FDW019).
/// </para>
/// <para>
/// This component is mounted exclusively by <c>ChartHost</c> via <c>DynamicComponent</c>.
/// No <c>@page</c> directive — it is not a routable page.
/// </para>
/// </remarks>
[SuppressMessage("Meziantou.Analyzer", "MA0004",
    Justification = "Blazor UI event handlers run on sync context")]
public sealed partial class RadzenChartsRenderer : ComponentBase
{
    // ── Parameters ────────────────────────────────────────────────────────────────

    /// <summary>
    /// Gets or sets the render-agnostic chart model. Required.
    /// </summary>
    [Parameter, EditorRequired]
    public IChartModel Model { get; set; } = default!;

    /// <summary>
    /// Gets or sets the data rows to plot.
    /// </summary>
    /// <remarks>
    /// Each row is a field-name-to-value dictionary. The renderer extracts values by the
    /// field names declared in <see cref="IChartModel.Encodings"/>.
    /// </remarks>
    [Parameter]
    public IReadOnlyList<IReadOnlyDictionary<string, object?>> Rows { get; set; } = [];

    /// <summary>
    /// Gets or sets the optional logger. Falls back to <see cref="NullLogger{T}.Instance"/>.
    /// </summary>
    [Parameter]
    public ILogger? Logger { get; set; }

    // ── Private state ─────────────────────────────────────────────────────────────

    private RadzenChartConfiguration? _chartConfig;
    private string? _errorMessage;
    private bool _isLoading = true;

    private ILogger ResolvedLogger => Logger ?? NullLogger<RadzenChartsRenderer>.Instance;

    // ── Lifecycle ─────────────────────────────────────────────────────────────────

    /// <inheritdoc />
    protected override void OnParametersSet()
    {
        if (Model is null)
        {
            _isLoading = false;
            return;
        }

        RadzenChartsRendererLog.RenderBegin(ResolvedLogger, Model.ChartType.Name, Rows.Count);

        var strategy = RadzenChartStrategyMap.For(Model.ChartType.Name);
        if (strategy is null)
        {
            _errorMessage = RadzenChartsRendererLog.UnsupportedChartType(ResolvedLogger, Model.ChartType.Name).Message;
            _chartConfig  = null;
            _isLoading    = false;
            return;
        }

        _errorMessage = null;
        _chartConfig  = strategy(Model, Rows);
        _isLoading    = false;

        RadzenChartsRendererLog.Rendered(ResolvedLogger, Model.ChartType.Name, Rows.Count);
    }
}
