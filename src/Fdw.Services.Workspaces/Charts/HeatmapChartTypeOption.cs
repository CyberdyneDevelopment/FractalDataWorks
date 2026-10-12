using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Workspaces.Charts;

/// <summary>Shows a number as colour across two categories or times.</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(ChartTypes), "Heatmap")]
public sealed class HeatmapChartTypeOption : ChartTypeBase
{
    /// <summary>Initializes a new instance of the <see cref="HeatmapChartTypeOption"/> class.</summary>
    public HeatmapChartTypeOption()
        : base("Heatmap", "Shows a number as colour across two categories or times.",
        [
            new ChartBindingShape("heatmap",
            [
                new ChartSlot("x", ["Dimension", "Time"], isRequired: true, allowsMany: false),
                new ChartSlot("y", ["Dimension", "Time"], isRequired: true, allowsMany: false),
                new ChartSlot("value", ["Measure"], isRequired: true, allowsMany: false),
            ]),
        ])
    {
    }
}
