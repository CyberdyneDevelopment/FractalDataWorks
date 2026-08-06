using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.UI.Abstractions.Charts;

/// <summary>
/// Scatter / bubble plot for visualising correlation between two numeric measures.
/// </summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(ChartTypes), "Scatter")]
public sealed class ScatterChartType : ChartTypeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ScatterChartType"/> class.
    /// </summary>
    public ScatterChartType()
        : base(
            id: 7,
            name: "Scatter",
            displayName: "Scatter Plot",
            category: "Correlation",
            iconHint: "scatter-chart",
            requiredEncodings: ["X", "Y"],
            optionalEncodings: ["Series", "Color", "Size", "Tooltip"])
    {
    }
}
