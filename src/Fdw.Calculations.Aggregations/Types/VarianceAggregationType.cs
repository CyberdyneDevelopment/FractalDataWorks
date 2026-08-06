using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Fdw.Collections.Attributes;

namespace Fdw.Calculations.Aggregations.Types;

/// <summary>
/// Aggregation type that calculates the variance of values.
/// </summary>
[TypeOption(typeof(AggregationTypes), "Variance", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class VarianceAggregationType : AggregationTypeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="VarianceAggregationType"/> class.
    /// </summary>
    public VarianceAggregationType() : base(10, "Variance", values =>
    {
        if (values.Count == 0) return 0m;
        var average = values.Average();
        var sumOfSquares = values.Sum(v => (v - average) * (v - average));
        return sumOfSquares / values.Count;
    })
    { }
}
