using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Fdw.Collections.Attributes;

namespace Fdw.Calculations.Abstractions.CalculationTypeOptions;

/// <summary>
/// Percentile calculation type - returns the value at the given percentile rank.
/// Uses linear interpolation between adjacent values.
/// </summary>
/// <remarks>
/// The <c>parameter</c> value specifies the percentile as a fraction in [0.0, 1.0].
/// Defaults to 0.5 (median) when no parameter is supplied.
/// </remarks>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(CalculationTypes), "Percentile", RestrictToCurrentCompilation = true)]
public sealed class PercentileCalculationType : CalculationTypeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="PercentileCalculationType"/> class.
    /// </summary>
    public PercentileCalculationType() : base(
        id: 6,
        name: "Percentile",
        category: "Aggregation",
        calculate: (rows, col, parameter) =>
        {
            var values = new List<decimal>();
            foreach (var r in rows)
                if (r.TryGetValue<decimal>(col, out var v)) values.Add(v);

            if (values.Count == 0)
                return 0m;

            values.Sort();
            var p = (double)(parameter ?? 0.5);
            var index = p * (values.Count - 1);
            var lower = (int)index;
            var upper = lower + 1;
            var fraction = (decimal)(index - lower);

            if (upper >= values.Count)
                return values[lower];

            return values[lower] + fraction * (values[upper] - values[lower]);
        },
        toSql: (col, parameter) =>
        {
            var p = parameter ?? 0.5;
            return $"PERCENTILE_CONT({p}) WITHIN GROUP (ORDER BY {col})";
        })
    { }
}
