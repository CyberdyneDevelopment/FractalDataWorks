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
        id: 1,
        name: "RunningTotal",
        category: "Windowed",
        calculate: (rows, col, _) =>
        {
            var result = new List<decimal>(rows.Count);
            var running = 0m;
            foreach (var r in rows)
            {
                running += r.TryGetValue<decimal>(col, out var v) ? v : 0m;
                result.Add(running);
            }
            return result;
        },
        toSql: (col, _) => $"SUM({col}) OVER (ORDER BY {col})")
    { }
}
