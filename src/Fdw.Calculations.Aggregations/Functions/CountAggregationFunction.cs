using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Calculations.Aggregations.Functions;

/// <summary>
/// Count aggregation function - returns the count of values.
/// </summary>
[TypeOption(typeof(AggregationFunctions), "Count", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class CountAggregationFunction : AggregationFunctionBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CountAggregationFunction"/> class.
    /// </summary>
    public CountAggregationFunction() : base(id: 5, name: "Count")
    {
    }

    /// <inheritdoc/>
    public override decimal Aggregate(IReadOnlyList<decimal> values)
    {
        return values.Count;
    }
}
