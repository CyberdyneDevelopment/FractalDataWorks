using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.UI.Abstractions.Charts;

/// <summary>
/// Donut chart — a pie chart with a hollow centre that optionally displays a total or KPI value.
/// </summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(ChartTypes), "Donut")]
public sealed class DonutChartType : ChartTypeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DonutChartType"/> class.
    /// </summary>
    public DonutChartType()
        : base(
            id: 5,
            name: "Donut",
            displayName: "Donut Chart",
            category: "Part-to-Whole",
            iconHint: "donut",
            requiredEncodings: ["X", "Y"],
            optionalEncodings: ["Measure", "Color", "Tooltip"])
    {
    }
}
