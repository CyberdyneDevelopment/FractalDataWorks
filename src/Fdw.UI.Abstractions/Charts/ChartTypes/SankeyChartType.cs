using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.UI.Abstractions.Charts;

/// <summary>
/// Sankey / flow diagram — visualises flows and their magnitudes between nodes.
/// </summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(ChartTypes), "Sankey")]
public sealed class SankeyChartType : ChartTypeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SankeyChartType"/> class.
    /// </summary>
    public SankeyChartType()
        : base(
            id: 11,
            name: "Sankey",
            displayName: "Sankey",
            category: "Flow",
            iconHint: "workflow",
            requiredEncodings: ["Source", "Target", "Weight"],
            optionalEncodings: ["Color", "Tooltip"])
    {
    }
}
