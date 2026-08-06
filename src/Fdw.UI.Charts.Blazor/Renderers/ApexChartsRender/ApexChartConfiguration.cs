using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using ApexCharts;

namespace Fdw.UI.Charts.Blazor.Renderers.ApexChartsRender;

/// <summary>
/// Intermediate configuration object produced by a per-chart-type strategy function.
/// </summary>
/// <remarks>
/// <para>
/// Holds the <see cref="ApexChartOptions{TItem}"/> (title, theme, legend, tooltip) and the
/// list of series descriptors. Both are consumed by <c>ApexChartsRenderer.razor</c> to
/// construct the <c>ApexChart&lt;ChartDataRow&gt;</c> component tree.
/// </para>
/// </remarks>
[ExcludeFromCodeCoverage]
public sealed class ApexChartConfiguration
{
    /// <summary>
    /// Gets or sets the chart-library options (title, tooltip, legend, plotOptions).
    /// </summary>
    public ApexChartOptions<ChartDataRow> Options { get; set; } = new();

    /// <summary>
    /// Gets or sets the list of series to render.
    /// </summary>
    public IReadOnlyList<ApexChartSeries>? SeriesItems { get; set; }
}
