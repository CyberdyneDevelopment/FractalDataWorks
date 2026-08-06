using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.UI.Abstractions.Charts;

/// <summary>
/// KPI tile — a single-metric summary card showing a headline number with optional comparison value.
/// </summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(ChartTypes), "Kpi")]
public sealed class KpiChartType : ChartTypeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="KpiChartType"/> class.
    /// </summary>
    public KpiChartType()
        : base(
            id: 6,
            name: "Kpi",
            displayName: "KPI",
            category: "Summary",
            iconHint: "hash",
            requiredEncodings: ["Measure"],
            optionalEncodings: ["Color", "Tooltip"])
    {
    }
}
