using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.UI.Abstractions.Charts;

/// <summary>
/// Pie chart for showing part-to-whole proportions of a single measure.
/// </summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(ChartTypes), "Pie")]
public sealed class PieChartType : ChartTypeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="PieChartType"/> class.
    /// </summary>
    public PieChartType()
        : base(
            id: 4,
            name: "Pie",
            displayName: "Pie Chart",
            category: "Part-to-Whole",
            iconHint: "pie-chart",
            requiredEncodings: ["X", "Y"],
            optionalEncodings: ["Color", "Tooltip"])
    {
    }
}
