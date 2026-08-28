using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.UI.Abstractions.Charts;
using Fdw.UI.Charts.Blazor.Renderers.ApexChartsRender;

namespace Fdw.UI.Charts.Blazor.RendererTypes;

/// <summary>
/// TypeOption descriptor for the built-in ApexCharts Blazor chart renderer.
/// </summary>
/// <remarks>
/// <para>
/// Capabilities: interactive hover tooltips, zoom/pan, responsive resize — provided out of the
/// box by the Blazor-ApexCharts library. Large series (&gt;~10 000 data points) are not
/// supported without the DataLabels overflow mode; <see cref="ChartRendererTypeBase.SupportsLargeSeries"/>
/// is <c>false</c>.
/// </para>
/// <para>
/// Supported chart types are the subset of <see cref="ChartTypes"/> that ApexCharts can render:
/// Bar, Line, Area, Pie, Donut, Scatter, Heatmap, Kpi. Geo, Sankey, and Table are excluded —
/// they require specialist rendering libraries beyond what ApexCharts provides.
/// </para>
/// <para>
/// The descriptor carries its Blazor component type via <see cref="ChartRendererTypeBase"/>'s
/// <c>RenderComponentType</c>, so <c>ChartHost</c> resolves the component straight from the
/// enumerable <see cref="ChartRendererTypes"/> registry — no separate map.
/// </para>
/// </remarks>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(ChartRendererTypes), "ApexCharts")]
public sealed class ApexChartsRendererType : ChartRendererTypeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ApexChartsRendererType"/> class.
    /// </summary>
    public ApexChartsRendererType()
        : base(
            id: 1,
            name: "ApexCharts",
            displayName: "ApexCharts",
            supportsInteraction: true,
            supportsLargeSeries: false,
            supportsEditing: false,
            supportedChartTypes: new List<string>
            {
                "Bar", "Line", "Area", "Pie", "Donut", "Scatter", "Heatmap", "Kpi",
            },
            renderComponentType: typeof(ApexChartsRenderer))
    {
    }
}
