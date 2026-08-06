using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Calculations.Abstractions.CalculationTypeOptions;

/// <summary>
/// Maximum calculation type - finds the largest value.
/// </summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(CalculationTypes), "Max", RestrictToCurrentCompilation = true)]
public sealed class MaxCalculationType : CalculationTypeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="MaxCalculationType"/> class.
    /// </summary>
    public MaxCalculationType() : base(
        id: 4,
        name: "Max",
        category: "Aggregation",
        calculate: (rows, col, _) =>
        {
            var max = decimal.MinValue;
            var found = false;
            foreach (var r in rows)
                if (r.TryGetValue<decimal>(col, out var v)) { if (v > max) max = v; found = true; }
            return found ? max : 0m;
        },
        toSql: (col, _) => $"MAX({col})")
    { }
}
