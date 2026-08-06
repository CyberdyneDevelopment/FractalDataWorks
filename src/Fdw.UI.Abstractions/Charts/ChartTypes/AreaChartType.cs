using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.UI.Abstractions.Charts;

/// <summary>
/// Filled area chart — like a line chart but with the region below the line filled.
/// </summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(ChartTypes), "Area")]
public sealed class AreaChartType : ChartTypeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AreaChartType"/> class.
    /// </summary>
    public AreaChartType()
        : base(
            id: 3,
            name: "Area",
            displayName: "Area Chart",
            category: "Trend",
            iconHint: "area-chart",
            requiredEncodings: ["X", "Y"],
            optionalEncodings: ["Series", "Color", "Tooltip"])
    {
    }
}
