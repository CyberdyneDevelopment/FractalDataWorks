using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Fdw.UI.Charts.Blazor.Renderers.ApexChartsRender;

/// <summary>
/// Thin adapter type bridging a raw <c>IReadOnlyDictionary&lt;string, object?&gt;</c> row to the
/// strongly-typed <c>TItem</c> expected by <c>ApexChart&lt;TItem&gt;</c>.
/// </summary>
/// <remarks>
/// Carrying the original row dictionary lets the per-type selector lambdas (X, Y, Series)
/// extract values by field name at series-construction time without re-projecting the raw data
/// into multiple typed lists.
/// </remarks>
[ExcludeFromCodeCoverage]
public sealed class ChartDataRow
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ChartDataRow"/> class.
    /// </summary>
    /// <param name="fields">The raw field dictionary for this data row.</param>
    public ChartDataRow(IReadOnlyDictionary<string, object?> fields)
    {
        Fields = fields;
    }

    /// <summary>
    /// Gets the raw field dictionary for this data row.
    /// </summary>
    public IReadOnlyDictionary<string, object?> Fields { get; }
}
