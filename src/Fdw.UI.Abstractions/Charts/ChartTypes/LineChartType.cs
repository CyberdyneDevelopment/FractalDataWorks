using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.UI.Abstractions.Charts;

/// <summary>
/// Line chart for visualising trends over a continuous dimension such as time.
/// </summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(ChartTypes), "Line")]
public sealed class LineChartType : ChartTypeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="LineChartType"/> class.
    /// </summary>
    public LineChartType()
        : base(
            id: 2,
            name: "Line",
            displayName: "Line Chart",
            category: "Trend",
            iconHint: "line-chart",
            requiredEncodings: ["X", "Y"],
            optionalEncodings: ["Series", "Color", "Tooltip"])
    {
    }
}
