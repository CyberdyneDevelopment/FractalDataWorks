using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Fdw.UI.Charts.Blazor.Renderers.RadzenCharts;

/// <summary>
/// Thin adapter that projects raw field-dictionary values into the strongly-typed
/// <c>TItem</c> expected by Radzen series components.
/// </summary>
/// <remarks>
/// Pre-projecting rows to named <see cref="Category"/> / <see cref="Value"/> properties
/// lets Radzen's reflection-based binding use the fixed string names
/// <c>CategoryProperty="Category"</c> and <c>ValueProperty="Value"</c> without accessing
/// the raw dictionary at render time.
/// </remarks>
[ExcludeFromCodeCoverage]
public sealed class RadzenChartDataRow
{
    /// <summary>
    /// Initializes a new instance of the <see cref="RadzenChartDataRow"/> class.
    /// </summary>
    /// <param name="category">The string category label (X-axis value, pie/donut slice label).</param>
    /// <param name="value">
    /// The numeric value (Y-axis magnitude, pie/donut segment value).
    /// <see langword="null"/> represents a missing or non-numeric field — the chart renders a gap.
    /// </param>
    public RadzenChartDataRow(string category, double? value)
    {
        Category = category;
        Value = value;
    }

    /// <summary>
    /// Gets the string category label. Bound via <c>CategoryProperty="Category"</c>.
    /// </summary>
    public string Category { get; }

    /// <summary>
    /// Gets the numeric value. Bound via <c>ValueProperty="Value"</c>.
    /// </summary>
    public double? Value { get; }
}
