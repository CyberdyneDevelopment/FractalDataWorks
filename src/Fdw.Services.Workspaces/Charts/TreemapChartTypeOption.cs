using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Workspaces.Charts;

/// <summary>Shows a number as area, nested by one or two categories.</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(ChartTypes), "Treemap")]
public sealed class TreemapChartTypeOption : ChartTypeBase
{
    /// <summary>Initializes a new instance of the <see cref="TreemapChartTypeOption"/> class.</summary>
    public TreemapChartTypeOption()
        : base("Treemap", "Shows a number as area, nested by one or two categories.",
        [
            new ChartBindingShape("treemap",
            [
                new ChartSlot("group", ["Dimension"], isRequired: true, allowsMany: false),
                new ChartSlot("subgroup", ["Dimension"], isRequired: false, allowsMany: false),
                new ChartSlot("size", ["Measure"], isRequired: true, allowsMany: false),
            ]),
        ])
    {
    }
}
