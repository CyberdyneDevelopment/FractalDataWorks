using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using ApexCharts;

namespace Fdw.UI.Charts.Blazor.Renderers.ApexChartsRender;

/// <summary>
/// Holds the data and selectors for a single ApexCharts series.
/// </summary>
/// <remarks>
/// This is an intermediate representation built by the per-chart-type strategy functions in
/// <see cref="ApexChartStrategyMap"/>. The <c>ApexChartsRenderer</c> razor markup iterates
/// <see cref="ApexChartConfiguration.SeriesItems"/> and emits an
/// <c>ApexPointSeries&lt;ChartDataRow&gt;</c> for each entry.
/// </remarks>
[ExcludeFromCodeCoverage]
public sealed class ApexChartSeries
{
    /// <summary>
    /// Gets or sets the series name displayed in the legend.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the series type (Bar, Line, Donut, etc.).
    /// </summary>
    public SeriesType SeriesType { get; set; }

    /// <summary>
    /// Gets or sets the data items for this series.
    /// </summary>
    public ICollection<ChartDataRow> Items { get; set; } = [];

    /// <summary>
    /// Gets or sets the lambda that extracts the X (category) value from a row.
    /// </summary>
    public Func<ChartDataRow, object?> XValueSelector { get; set; } = r => null;

    /// <summary>
    /// Gets or sets the lambda that extracts the Y (numeric) value from a row.
    /// </summary>
    public Func<ChartDataRow, decimal?> YValueSelector { get; set; } = _ => null;
}
