using Fdw.Collections;
using Fdw.Collections.Attributes;

namespace Fdw.Etl.Abstractions.Monitoring.HealthStateOptions;

/// <summary>
/// TypeCollection for health states.
/// </summary>
/// <remarks>
/// Provides compile-time discovery and O(1) lookup for health states.
/// Source generator creates static properties for each registered health state.
/// </remarks>
[TypeCollection(typeof(HealthStateBase), typeof(IHealthState), typeof(HealthStates))]
public sealed partial class HealthStates : TypeCollectionBase<HealthStateBase, IHealthState>
{
}
