using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Workspaces.Charts;

/// <summary>Compares a number across categories, with each bar split by another category or by several numbers.</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(ChartTypes), "StackedBar")]
public sealed class StackedBarChartTypeOption : ChartTypeBase
{
    /// <summary>Initializes a new instance of the <see cref="StackedBarChartTypeOption"/> class.</summary>
    public StackedBarChartTypeOption()
        : base("StackedBar", "Compares a number across categories, with each bar split by another category or by several numbers.",
        [
            new ChartBindingShape("stackedBar",
            [
                new ChartSlot("category", ["Dimension", "Time"], isRequired: true, allowsMany: false),
                new ChartSlot("value", ["Measure"], isRequired: true, allowsMany: true),
                new ChartSlot("series", ["Dimension"], isRequired: false, allowsMany: false),
            ]),
        ])
    {
    }
}
