using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.UI.Abstractions.Charts;
using Fdw.UI.Charts.Blazor.Renderers.ECharts;

namespace Fdw.UI.Charts.Blazor.RendererTypes;

/// <summary>
/// TypeOption descriptor for the built-in ECharts Blazor chart renderer.
/// </summary>
/// <remarks>
/// <para>
/// Capabilities: interactive hover tooltips, zoom/pan, large-series virtualisation (ECharts
/// renders 100k+ data points efficiently via canvas) — provided out of the box by the Apache
/// ECharts library loaded via the vendored UMD bundle and the <c>echarts-interop.js</c> ES
/// module.
/// </para>
/// <para>
/// Supported chart types are the subset of <see cref="ChartTypes"/> that ECharts can render:
/// Bar, Line, Area, Pie, Donut, Scatter, Heatmap, Sankey. Kpi, Geo, and Table are excluded —
/// Kpi is an ApexCharts RadialBar pattern; Geo and Table require specialist libraries.
/// </para>
/// <para>
/// The descriptor carries its Blazor component type via <see cref="ChartRendererTypeBase"/>'s
/// <c>RenderComponentType</c>, so <c>ChartHost</c> resolves the component straight from the
/// enumerable <see cref="ChartRendererTypes"/> registry — no separate map.
/// </para>
/// </remarks>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(ChartRendererTypes), "ECharts")]
public sealed class EChartsRendererType : ChartRendererTypeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="EChartsRendererType"/> class.
    /// </summary>
    public EChartsRendererType()
        : base(
            id: 2,
            name: "ECharts",
            displayName: "ECharts",
            supportsInteraction: true,
            supportsLargeSeries: true,
            supportsEditing: false,
            supportedChartTypes: new List<string>
            {
                "Bar", "Line", "Area", "Pie", "Donut", "Scatter", "Heatmap", "Sankey",
            },
            renderComponentType: typeof(EChartsRenderer))
    {
    }
}
