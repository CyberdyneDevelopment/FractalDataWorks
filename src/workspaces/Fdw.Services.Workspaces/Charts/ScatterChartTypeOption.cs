using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Workspaces.Charts;

/// <summary>Shows how two numbers relate, one point per row or per group.</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(ChartTypes), "Scatter")]
public sealed class ScatterChartTypeOption : ChartTypeBase
{
    /// <summary>Initializes a new instance of the <see cref="ScatterChartTypeOption"/> class.</summary>
    public ScatterChartTypeOption()
        : base("Scatter", "Shows how two numbers relate, one point per row or per group.",
        [
            new ChartBindingShape("scatter",
            [
                new ChartSlot("x", ["Measure"], isRequired: true, allowsMany: false),
                new ChartSlot("y", ["Measure"], isRequired: true, allowsMany: false),
                new ChartSlot("size", ["Measure"], isRequired: false, allowsMany: false),
                new ChartSlot("colour", ["Dimension"], isRequired: false, allowsMany: false),
            ]),
        ])
    {
    }
}
