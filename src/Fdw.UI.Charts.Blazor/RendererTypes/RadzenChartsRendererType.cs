using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.UI.Abstractions.Charts;
using Fdw.UI.Charts.Blazor.Renderers.RadzenCharts;

namespace Fdw.UI.Charts.Blazor.RendererTypes;

/// <summary>
/// TypeOption descriptor for the built-in Radzen Blazor chart renderer.
/// </summary>
/// <remarks>
/// <para>
/// Capabilities: interactive hover tooltips and a legend — provided out of the box by the
/// Radzen Blazor library. Large series (&gt;~5 000 data points) are not recommended;
/// <see cref="ChartRendererTypeBase.SupportsLargeSeries"/> is <c>false</c>.
/// </para>
/// <para>
/// Supported chart types are the subset of <see cref="ChartTypes"/> that Radzen can render:
/// Bar, Line, Area, Pie, Donut, Scatter. Heatmap, Kpi, Geo, Sankey, and Table are excluded —
/// they require chart types not available in the Radzen library.
/// </para>
/// <para>
/// The descriptor carries its Blazor component type via <see cref="ChartRendererTypeBase"/>'s
/// <c>RenderComponentType</c>, so <c>ChartHost</c> resolves the component straight from the
/// enumerable <see cref="ChartRendererTypes"/> registry — no separate map.
/// </para>
/// </remarks>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(ChartRendererTypes), "Radzen")]
public sealed class RadzenChartsRendererType : ChartRendererTypeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="RadzenChartsRendererType"/> class.
    /// </summary>
    public RadzenChartsRendererType()
        : base(
            id: 2,
            name: "Radzen",
            displayName: "Radzen",
            supportsInteraction: true,
            supportsLargeSeries: false,
            supportsEditing: false,
            // Why: explicit list so the ChartHost can filter the chart-type dropdown to only the
            // types this renderer handles — no reflection, no switch. Heatmap, Kpi, Geo, Sankey,
            // and Table are excluded; they require libraries beyond what Radzen provides.
            supportedChartTypes: new List<string>
            {
                "Bar", "Line", "Area", "Pie", "Donut", "Scatter",
            },
            renderComponentType: typeof(RadzenChartsRenderer))
    {
    }
}
