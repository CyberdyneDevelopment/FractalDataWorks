using System.Diagnostics.CodeAnalysis;
using Fdw.Collections;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Etl.Abstractions.OptionTypes;

/// <summary>
/// Collection of aggregate functions available to Aggregate transforms (Sum, Count, Avg, Min, Max,
/// First, Last). The runtime dispatches via <c>AggregateFunctions.ByName(fn).Apply(values)</c> instead
/// of a switch, so a consuming assembly can add a new function via a module initializer.
/// </summary>
[ExcludeFromCodeCoverage]
[TypeCollection(typeof(AggregateFunctionBase), typeof(IAggregateFunction), typeof(AggregateFunctions))]
public abstract partial class AggregateFunctions : TypeCollectionBase<AggregateFunctionBase, IAggregateFunction>
{
    // DO NOT IMPLEMENT BY HAND! Source generator populates ByName/ById/All/NotFound.
}
