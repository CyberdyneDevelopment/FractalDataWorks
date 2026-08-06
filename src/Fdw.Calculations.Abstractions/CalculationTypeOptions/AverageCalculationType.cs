using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Fdw.Collections.Attributes;

namespace Fdw.Calculations.Abstractions.CalculationTypeOptions;

/// <summary>
/// Average calculation type - calculates mean of all values.
/// </summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(CalculationTypes), "Average", RestrictToCurrentCompilation = true)]
public sealed class AverageCalculationType : CalculationTypeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AverageCalculationType"/> class.
    /// </summary>
    public AverageCalculationType() : base(
        id: 2,
        name: "Average",
        category: "Aggregation",
        calculate: (rows, col, _) =>
        {
            var values = new List<decimal>();
            foreach (var r in rows)
                if (r.TryGetValue<decimal>(col, out var v)) values.Add(v);
            return values.Count == 0 ? 0m : values.Average();
        },
        toSql: (col, _) => $"AVG({col})")
    { }
}
