using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Fdw.Collections.Attributes;

namespace Fdw.Calculations.Abstractions.CalculationTypeOptions;

/// <summary>
/// Minimum calculation type - finds the smallest value.
/// </summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(CalculationTypes), "Min", RestrictToCurrentCompilation = true)]
public sealed class MinCalculationType : CalculationTypeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="MinCalculationType"/> class.
    /// </summary>
    public MinCalculationType() : base(
        id: 3,
        name: "Min",
        category: "Aggregation",
        calculate: (rows, col, _) =>
        {
            var min = decimal.MaxValue;
            var found = false;
            foreach (var r in rows)
                if (r.TryGetValue<decimal>(col, out var v)) { if (v < min) min = v; found = true; }
            return found ? min : 0m;
        },
        toSql: (col, _) => $"MIN({col})")
    { }
}
