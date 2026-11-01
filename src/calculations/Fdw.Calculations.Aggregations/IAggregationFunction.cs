using System.Collections.Generic;
using Fdw.Collections;

namespace Fdw.Calculations.Aggregations;

/// <summary>
/// Interface for aggregation functions.
/// Extends ITypeOption to enable TypeCollection discovery.
/// </summary>
public interface IAggregationFunction : ITypeOption<int, AggregationFunctionBase>
{
    /// <summary>
    /// Applies the aggregation function to the provided values.
    /// </summary>
    /// <param name="values">The values to aggregate.</param>
    /// <returns>The aggregated result.</returns>
    decimal Aggregate(IReadOnlyList<decimal> values);
}
