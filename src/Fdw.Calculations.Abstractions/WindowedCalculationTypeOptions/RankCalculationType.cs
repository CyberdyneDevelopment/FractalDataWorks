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
        id: 3,
        name: "Rank",
        category: "Windowed",
        calculate: (rows, col, _) =>
        {
            var values = rows
                .Select(r => r.TryGetValue<decimal>(col, out var v) ? v : 0m)
                .ToList();

            var sorted = values.OrderBy(v => v).ToList();

            var result = new List<decimal>(values.Count);
            foreach (var value in values)
                result.Add(sorted.IndexOf(value) + 1);

            return result;
        },
        toSql: (col, _) => $"RANK() OVER (ORDER BY {col})")
    { }
}
