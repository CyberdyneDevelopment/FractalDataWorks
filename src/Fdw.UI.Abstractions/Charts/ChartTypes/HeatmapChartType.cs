using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.UI.Abstractions.Charts;

/// <summary>
/// Heatmap — two-dimensional density grid where cell colour encodes a numeric measure.
/// </summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(ChartTypes), "Heatmap")]
public sealed class HeatmapChartType : ChartTypeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="HeatmapChartType"/> class.
    /// </summary>
    public HeatmapChartType()
        : base(
            id: 9,
            name: "Heatmap",
            displayName: "Heatmap",
            category: "Distribution",
            iconHint: "grid-3x3",
            requiredEncodings: ["X", "Y", "Color"],
            optionalEncodings: ["Tooltip"])
    {
    }
}
