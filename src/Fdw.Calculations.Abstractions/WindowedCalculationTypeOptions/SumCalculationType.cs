using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Calculations.Abstractions.WindowedCalculationTypeOptions;

/// <summary>Running sum of the column over the ordered rows; rows without a value add nothing, and the sum is empty until a row has a value.</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(WindowedCalculationTypes), "Sum", RestrictToCurrentCompilation = true)]
public sealed class SumCalculationType : WindowedCalculationTypeBase
{
    /// <summary>Initializes a new instance of the <see cref="SumCalculationType"/> class.</summary>
    public SumCalculationType() : base(
        name: "Sum",
        category: "Windowed",
        calculate: (rows, col, _) =>
        {
            var result = new List<decimal?>(rows.Count);
            decimal? sum = null;
            foreach (var v in WindowedColumn.Read(rows, col))
            {
                if (v.HasValue)
                    sum = sum.HasValue ? sum.Value + v.Value : v.Value;
                result.Add(sum);
            }
            return result;
        },
        toSql: (col, _) => $"SUM({col}) OVER (ORDER BY {col})")
    { }
}
