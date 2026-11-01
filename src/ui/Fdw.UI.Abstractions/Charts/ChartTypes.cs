using System.Diagnostics.CodeAnalysis;
using Fdw.Collections;
using Fdw.Collections.Attributes;

namespace Fdw.UI.Abstractions.Charts;

/// <summary>
/// TypeCollection for chart types — the enumerable registry of visualisation kinds.
/// </summary>
/// <remarks>
/// <para>
/// Seeded members cover the core chart vocabulary:
/// <list type="bullet">
/// <item><c>Bar</c> — vertical or horizontal bar chart (Comparison)</item>
/// <item><c>Line</c> — line / time-series chart (Trend)</item>
/// <item><c>Area</c> — filled area chart (Trend)</item>
/// <item><c>Pie</c> — pie chart (Part-to-Whole)</item>
/// <item><c>Donut</c> — donut chart with a centre value (Part-to-Whole)</item>
/// <item><c>Kpi</c> — single-metric KPI tile (Summary)</item>
/// <item><c>Scatter</c> — scatter / bubble plot (Correlation)</item>
/// <item><c>Table</c> — tabular data grid (Data)</item>
/// <item><c>Heatmap</c> — two-dimensional density grid (Distribution)</item>
/// <item><c>Geo</c> — geographic choropleth or point map (Spatial)</item>
/// <item><c>Sankey</c> — Sankey / flow diagram (Flow)</item>
/// </list>
/// </para>
/// <para>
/// Downstream assemblies extend this set by declaring their own <c>[TypeOption]</c> classes
/// that inherit <see cref="ChartTypeBase"/> — no changes to this file needed.
/// </para>
/// <para>
/// Usage:
/// <code>
/// var chartType = ChartTypes.ByName("Bar");
/// if (chartType == ChartTypes.NotFound)
///     // fail loud — the type name is not registered
///
/// foreach (var t in ChartTypes.All()) { ... }
/// </code>
/// </para>
/// </remarks>
[TypeCollection(typeof(ChartTypeBase), typeof(IChartType), typeof(ChartTypes))]
[ExcludeFromCodeCoverage]
public abstract partial class ChartTypes : TypeCollectionBase<ChartTypeBase, IChartType>
{
}
