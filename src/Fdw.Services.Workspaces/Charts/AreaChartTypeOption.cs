using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Workspaces.Charts;

/// <summary>Shows how a number accumulates over time or across ordered categories.</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(ChartTypes), "Area")]
public sealed class AreaChartTypeOption : ChartTypeBase
{
    /// <summary>Initializes a new instance of the <see cref="AreaChartTypeOption"/> class.</summary>
    public AreaChartTypeOption()
        : base("Area", "Shows how a number accumulates over time or across ordered categories.",
        [
            new ChartBindingShape("area",
            [
                new ChartSlot("category", ["Time", "Dimension"], isRequired: true, allowsMany: false),
                new ChartSlot("value", ["Measure"], isRequired: true, allowsMany: false),
                new ChartSlot("series", ["Dimension"], isRequired: false, allowsMany: false),
            ]),
        ])
    {
    }
}
