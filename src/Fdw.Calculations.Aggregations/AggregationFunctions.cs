using Fdw.Collections;
using Fdw.Collections.Attributes;

namespace Fdw.Calculations.Aggregations;

/// <summary>
/// TypeCollection for aggregation functions.
/// </summary>
/// <remarks>
/// Provides compile-time discovery and O(1) lookup for aggregation functions.
/// Source generator creates static properties for each aggregation function.
/// </remarks>
[TypeCollection(typeof(AggregationFunctionBase), typeof(IAggregationFunction), typeof(AggregationFunctions))]
public sealed partial class AggregationFunctions : TypeCollectionBase<AggregationFunctionBase, IAggregationFunction>
{
}
