using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Workspaces.Charts;

/// <summary>Shows each category as a share of a whole.</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(ChartTypes), "Donut")]
public sealed class DonutChartTypeOption : ChartTypeBase
{
    /// <summary>Initializes a new instance of the <see cref="DonutChartTypeOption"/> class.</summary>
    public DonutChartTypeOption()
        : base("Donut", "Shows each category as a share of a whole.",
        [
            new ChartBindingShape("donut",
            [
                new ChartSlot("slice", ["Dimension"], isRequired: true, allowsMany: false),
                new ChartSlot("value", ["Measure"], isRequired: true, allowsMany: false),
            ]),
        ])
    {
    }
}
