using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Components;

namespace Fdw.UI.Charts.Blazor.Renderers.RadzenCharts;

/// <summary>
/// Intermediate configuration object produced by a per-chart-type strategy function.
/// </summary>
/// <remarks>
/// <para>
/// Carries display options (title, legend, tooltips) and a pre-built
/// <see cref="SeriesFragment"/> that renders the correct Radzen series component(s) inside
/// a <c>RadzenChart</c> container. The fragment is constructed by
/// <see cref="RadzenChartStrategyMap"/> at parameter-set time and evaluated at Blazor
/// render time inside <c>RadzenChartsRenderer.razor</c>.
/// </para>
/// <para>
/// Storing the series rendering as a <see cref="RenderFragment"/> keeps the razor markup
/// free of if/else chains on chart-type names, satisfying FDW019.
/// </para>
/// </remarks>
[ExcludeFromCodeCoverage]
public sealed class RadzenChartConfiguration
{
    /// <summary>
    /// Gets or sets the chart title shown above the Radzen chart.
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets a value indicating whether the chart legend is shown.
    /// </summary>
    public bool ShowLegend { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether hover tooltips are enabled.
    /// </summary>
    public bool EnableTooltips { get; set; }

    /// <summary>
    /// Gets or sets the pre-built render fragment that emits the Radzen series component(s).
    /// </summary>
    /// <remarks>
    /// Built by the strategy map from pre-projected <see cref="RadzenChartDataRow"/> lists.
    /// <see langword="null"/> indicates no series to render (e.g. empty row set).
    /// </remarks>
    public RenderFragment? SeriesFragment { get; set; }
}
