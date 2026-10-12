using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Workspaces.Charts;

/// <summary>Shows how one or more numbers change over time or across ordered categories.</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(ChartTypes), "Line")]
public sealed class LineChartTypeOption : ChartTypeBase
{
    /// <summary>Initializes a new instance of the <see cref="LineChartTypeOption"/> class.</summary>
    public LineChartTypeOption()
        : base("Line", "Shows how one or more numbers change over time or across ordered categories.",
        [
            new ChartBindingShape("line",
            [
                new ChartSlot("category", ["Time", "Dimension"], isRequired: true, allowsMany: false),
                new ChartSlot("value", ["Measure"], isRequired: true, allowsMany: true),
                new ChartSlot("series", ["Dimension"], isRequired: false, allowsMany: false),
            ]),
        ])
    {
    }
}
