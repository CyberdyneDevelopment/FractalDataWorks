using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Calculations.Abstractions.CalculationTypeOptions;

/// <summary>
/// Count calculation type - returns the count of non-null values in the column.
/// </summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(CalculationTypes), "Count", RestrictToCurrentCompilation = true)]
public sealed class CountCalculationType : CalculationTypeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CountCalculationType"/> class.
    /// </summary>
    public CountCalculationType() : base(
        id: 5,
        name: "Count",
        category: "Aggregation",
        calculate: (rows, col, param) =>
        {
            var count = 0;
            foreach (var r in rows)
                if (r.TryGetValue<decimal>(col, out _)) count++;
            return count;
        },
        toSql: (col, _) => $"COUNT({col})")
    { }
}
