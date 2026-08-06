using System.Collections.Generic;
using Fdw.Collections;

namespace Fdw.Calculations.Aggregations;

/// <summary>
/// Base class for all aggregation functions.
/// </summary>
public abstract class AggregationFunctionBase : TypeOptionBase<int, AggregationFunctionBase>, IAggregationFunction
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AggregationFunctionBase"/> class.
    /// </summary>
    /// <param name="id">The unique identifier for the aggregation function.</param>
    /// <param name="name">The name of the aggregation function.</param>
    protected AggregationFunctionBase(int id, string name) : base(id, name)
    {
    }

    /// <summary>
    /// Applies the aggregation function to the provided values.
    /// </summary>
    /// <param name="values">The values to aggregate.</param>
    /// <returns>The aggregated result.</returns>
    public abstract decimal Aggregate(IReadOnlyList<decimal> values);
}
