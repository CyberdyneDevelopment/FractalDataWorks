using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Fdw.Collections.Attributes;

namespace Fdw.Calculations.Abstractions.WindowedCalculationTypeOptions;

/// <summary>Dense rank of each row's value; equal values share a rank and ranks have no gaps. A row without a value has no rank.</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(WindowedCalculationTypes), "DenseRank", RestrictToCurrentCompilation = true)]
public sealed class DenseRankCalculationType : WindowedCalculationTypeBase
{
    /// <summary>Initializes a new instance of the <see cref="DenseRankCalculationType"/> class.</summary>
    public DenseRankCalculationType() : base(
        name: "DenseRank",
        category: "Windowed",
        calculate: (rows, col, _) =>
        {
            var values = WindowedColumn.Read(rows, col);
            var distinct = values.OfType<decimal>().Distinct().OrderBy(v => v).ToList();
            return values.Select(v => v.HasValue ? (decimal?)(distinct.IndexOf(v.Value) + 1) : null).ToList();
        },
        toSql: (col, _) => $"DENSE_RANK() OVER (ORDER BY {col})")
    { }
}
