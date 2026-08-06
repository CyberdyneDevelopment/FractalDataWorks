using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Fdw.Collections.Attributes;

namespace Fdw.Calculations.Aggregations.Types;

/// <summary>
/// Aggregation type that calculates the standard deviation of values.
/// </summary>
[TypeOption(typeof(AggregationTypes), "StandardDeviation", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class StandardDeviationAggregationType : AggregationTypeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="StandardDeviationAggregationType"/> class.
    /// </summary>
    public StandardDeviationAggregationType() : base(8, "StandardDeviation", values =>
    {
        if (values.Count == 0) return 0m;
        var average = values.Average();
        var sumOfSquares = values.Sum(v => (v - average) * (v - average));
        var variance = sumOfSquares / values.Count;
        return (decimal)Math.Sqrt((double)variance);
    })
    { }
}
