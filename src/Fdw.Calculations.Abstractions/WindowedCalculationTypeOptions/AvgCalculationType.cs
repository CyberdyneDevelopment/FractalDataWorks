using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Calculations.Abstractions.WindowedCalculationTypeOptions;

/// <summary>Running average of the column over the ordered rows; rows without a value are not counted, and the average is empty until a row has a value.</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(WindowedCalculationTypes), "Avg", RestrictToCurrentCompilation = true)]
public sealed class AvgCalculationType : WindowedCalculationTypeBase
{
    /// <summary>Initializes a new instance of the <see cref="AvgCalculationType"/> class.</summary>
    public AvgCalculationType() : base(
        name: "Avg",
        category: "Windowed",
        calculate: (rows, col, _) =>
        {
            var result = new List<decimal?>(rows.Count);
            var sum = 0m;
            var count = 0;
            foreach (var v in WindowedColumn.Read(rows, col))
            {
                if (v.HasValue)
                {
                    sum += v.Value;
                    count++;
                }
                result.Add(count > 0 ? sum / count : null);
            }
            return result;
        },
        toSql: (col, _) => $"AVG({col}) OVER (ORDER BY {col})")
    { }
}
