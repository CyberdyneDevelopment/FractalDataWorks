using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Fdw.UI.Abstractions.Charts;
using Fdw.UI.Charts.Blazor.Logging;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.UI.Charts.Blazor.Renderers.ApexChartsRender;

/// <summary>
/// Blazor renderer component for ApexCharts.
/// </summary>
/// <remarks>
/// <para>
/// Renders the <see cref="Model"/> spec against the provided <see cref="Rows"/> using the
/// Blazor-ApexCharts library. Chart-type dispatch is data-driven through
/// <see cref="ApexChartStrategyMap"/> — no switch/if-else chain on the chart type name (FDW019).
/// </para>
/// <para>
/// This component is mounted exclusively by <c>ChartHost</c> via <c>DynamicComponent</c>.
/// No <c>@page</c> directive — it is not a routable page.
/// </para>
/// </remarks>
[SuppressMessage("Meziantou.Analyzer", "MA0004",
    Justification = "Blazor UI event handlers run on sync context")]
public sealed partial class ApexChartsRenderer : ComponentBase
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
    // Why: NullLogger fallback is the only acceptable ?? fallback per FDW conventions.
    [Parameter]
    public ILogger? Logger { get; set; }

    // ── Private state ─────────────────────────────────────────────────────────────

    private ApexChartConfiguration? _chartConfig;
    private string? _errorMessage;
    private bool _isLoading = true;

    // Why: the ApexChart component ref is held so future methods (e.g. UpdateSeriesAsync)
    // can trigger data refreshes without a full re-render.
    private ApexCharts.ApexChart<ChartDataRow>? _apexChart;

    private ILogger ResolvedLogger => Logger ?? NullLogger<ApexChartsRenderer>.Instance;

    // ── Lifecycle ─────────────────────────────────────────────────────────────────

    /// <inheritdoc />
    protected override void OnParametersSet()
    {
        if (Model is null)
        {
            _isLoading = false;
            return;
        }

        ApexChartsRendererLog.RenderBegin(ResolvedLogger, Model.ChartType.Name, Rows.Count);

        var strategy = ApexChartStrategyMap.For(Model.ChartType.Name);
        if (strategy is null)
        {
            _errorMessage = ApexChartsRendererLog.UnsupportedChartType(ResolvedLogger, Model.ChartType.Name).Message;
            _chartConfig  = null;
            _isLoading    = false;
            return;
        }

        _errorMessage = null;
        _chartConfig  = strategy(Model, Rows);
        _isLoading    = false;

        ApexChartsRendererLog.Rendered(ResolvedLogger, Model.ChartType.Name, Rows.Count);
    }
}
