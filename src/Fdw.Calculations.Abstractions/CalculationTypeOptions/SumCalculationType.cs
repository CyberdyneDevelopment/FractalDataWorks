using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Fdw.Collections.Attributes;

namespace Fdw.Calculations.Abstractions.CalculationTypeOptions;

/// <summary>
/// Sum calculation type - adds all values together.
/// </summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(CalculationTypes), "Sum", RestrictToCurrentCompilation = true)]
public sealed class SumCalculationType : CalculationTypeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SumCalculationType"/> class.
    /// </summary>
    public SumCalculationType() : base(
        id: 1,
        name: "Sum",
        category: "Aggregation",
        calculate: (rows, col, _) => rows.Sum(r => r.TryGetValue<decimal>(col, out var v) ? v : 0m),
        toSql: (col, _) => $"SUM({col})")
    { }
}
