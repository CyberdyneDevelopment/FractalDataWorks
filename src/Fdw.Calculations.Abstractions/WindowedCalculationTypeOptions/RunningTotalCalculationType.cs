using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Calculations.Abstractions.WindowedCalculationTypeOptions;

/// <summary>
/// Running total calculation type - computes the cumulative sum up to each row.
/// </summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(WindowedCalculationTypes), "RunningTotal", RestrictToCurrentCompilation = true)]
public sealed class RunningTotalCalculationType : WindowedCalculationTypeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="RunningTotalCalculationType"/> class.
    /// </summary>
    public RunningTotalCalculationType() : base(
        name: "RunningTotal",
        category: "Windowed",
        calculate: (rows, col, _) =>
        {
            var result = new List<decimal?>(rows.Count);
            var running = 0m;
            foreach (var v in WindowedColumn.Read(rows, col))
            {
                if (v.HasValue)
                    running += v.Value;
                result.Add(running);
            }
            return result;
        },
        toSql: (col, _) => $"SUM({col}) OVER (ORDER BY {col})")
    { }
}
