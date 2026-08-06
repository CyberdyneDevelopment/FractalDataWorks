using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.UI.Abstractions.Charts;

/// <summary>
/// Tabular data grid — displays raw or aggregated rows and columns without a visual encoding.
/// </summary>
/// <remarks>
/// A Table chart type carries no required encodings: the columns to display are derived from the
/// <see cref="IChartModel.Encodings"/> collection — each bound <c>Y</c> (or any non-X role) maps to
/// a displayed column. This is intentional: an empty encoding list renders all fields from the
/// data source, while explicit bindings control column selection and order.
/// </remarks>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(ChartTypes), "Table")]
public sealed class TableChartType : ChartTypeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="TableChartType"/> class.
    /// </summary>
    public TableChartType()
        : base(
            id: 8,
            name: "Table",
            displayName: "Table",
            category: "Data",
            iconHint: "table",
            requiredEncodings: [],
            optionalEncodings: ["X", "Y", "Series", "Tooltip"])
    {
    }
}
