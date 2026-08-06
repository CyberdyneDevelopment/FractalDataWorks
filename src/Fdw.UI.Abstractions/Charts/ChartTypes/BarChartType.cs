using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.UI.Abstractions.Charts;

/// <summary>
/// Vertical or horizontal bar chart for comparing values across categories.
/// </summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(ChartTypes), "Bar")]
public sealed class BarChartType : ChartTypeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="BarChartType"/> class.
    /// </summary>
    public BarChartType()
        : base(
            id: 1,
            name: "Bar",
            displayName: "Bar Chart",
            category: "Comparison",
            iconHint: "bar-chart-2",
            requiredEncodings: ["X", "Y"],
            optionalEncodings: ["Series", "Color", "Tooltip"])
    {
    }
}
