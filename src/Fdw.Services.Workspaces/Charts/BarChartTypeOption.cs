using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Workspaces.Charts;

/// <summary>Compares a number across categories, side by side.</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(ChartTypes), "Bar")]
public sealed class BarChartTypeOption : ChartTypeBase
{
    /// <summary>Initializes a new instance of the <see cref="BarChartTypeOption"/> class.</summary>
    public BarChartTypeOption()
        : base("Bar", "Compares a number across categories, side by side.",
        [
            new ChartBindingShape("bar",
            [
                new ChartSlot("category", ["Dimension", "Time"], isRequired: true, allowsMany: false),
                new ChartSlot("value", ["Measure"], isRequired: true, allowsMany: true),
                new ChartSlot("series", ["Dimension"], isRequired: false, allowsMany: false),
            ]),
        ])
    {
    }
}
