using System.Collections.Generic;
using Fdw.Collections;

namespace Fdw.Calculations.Aggregations;

/// <summary>
/// Base class for all aggregation functions.
/// </summary>
public abstract class AggregationFunctionBase : TypeOptionBase<AggregationFunctionBase>, IAggregationFunction
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AggregationFunctionBase"/> class.
    /// </summary>
    /// <param name="name">The name of the aggregation function.</param>
    protected AggregationFunctionBase(string name) : base(name)
    {
    }

    /// <summary>
    /// Applies the aggregation function to the provided values.
    /// </summary>
    /// <param name="values">The values to aggregate.</param>
    /// <returns>The aggregated result.</returns>
    public abstract decimal Aggregate(IReadOnlyList<decimal> values);
}
