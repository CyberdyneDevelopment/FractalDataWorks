using Fdw.Collections;
using Fdw.Collections.Attributes;
using System.Diagnostics.CodeAnalysis;

namespace Fdw.Services.Abstractions.Health;

/// <summary>
/// Collection of health state TypeOptions.
/// </summary>
[ExcludeFromCodeCoverage]
[TypeCollection(typeof(HealthStateBase), typeof(IHealthState), typeof(HealthStates))]
public abstract partial class HealthStates : TypeCollectionBase<HealthStateBase, IHealthState>
{
}
