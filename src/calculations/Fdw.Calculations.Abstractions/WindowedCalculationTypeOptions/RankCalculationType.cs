using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Fdw.Collections.Attributes;

namespace Fdw.Calculations.Abstractions.WindowedCalculationTypeOptions;

/// <summary>
/// Rank calculation type - assigns a rank to each row based on column value ascending.
/// Rows with equal values receive the same rank; the next rank skips the tied positions.
/// </summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(WindowedCalculationTypes), "Rank", RestrictToCurrentCompilation = true)]
public sealed class RankCalculationType : WindowedCalculationTypeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="RankCalculationType"/> class.
    /// </summary>
    public RankCalculationType() : base(
        name: "Rank",
        category: "Windowed",
        calculate: (rows, col, _) =>
        {
            var values = WindowedColumn.Read(rows, col);
            var sorted = values.OfType<decimal>().OrderBy(v => v).ToList();
            return values.Select(v => v.HasValue ? (decimal?)(sorted.IndexOf(v.Value) + 1) : null).ToList();
        },
        toSql: (col, _) => $"RANK() OVER (ORDER BY {col})")
    { }
}
