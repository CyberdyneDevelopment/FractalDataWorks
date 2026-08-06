using Fdw.Collections;
using Fdw.Collections.Attributes;

namespace Fdw.Calculations.Aggregations.Types;

/// <summary>
/// Collection of all aggregation types.
/// </summary>
/// <remarks>
/// This collection is populated by the source generator with all types
/// that inherit from AggregationTypeBase and implement IAggregationType.
/// Provides high-performance lookups for aggregation discovery.
/// </remarks>
[TypeCollection(typeof(AggregationTypeBase), typeof(IAggregationType), typeof(AggregationTypes))]
public abstract partial class AggregationTypes : TypeCollectionBase<AggregationTypeBase, IAggregationType>
{
}
