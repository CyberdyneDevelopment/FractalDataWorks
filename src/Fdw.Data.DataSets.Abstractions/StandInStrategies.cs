using System.Diagnostics.CodeAnalysis;
using Fdw.Collections;
using Fdw.Collections.Attributes;

namespace Fdw.Data.DataSets.Abstractions;

/// <summary>The ways a sketched field's query can be answered without real data.</summary>
/// <remarks>
/// Extensible by design -- data.DataSetFieldStandIn's own DDL comment says so ("StandInStrategies
/// is an extensible TypeCollection, validated in the provider"), which is also why the table
/// carries no CHECK constraint over a fixed set of names the way MemberRole/State do.
/// </remarks>
[ExcludeFromCodeCoverage]
[TypeCollection(typeof(StandInStrategyBase), typeof(IStandInStrategy), typeof(StandInStrategies))]
public abstract partial class StandInStrategies : TypeCollectionBase<StandInStrategyBase, IStandInStrategy>
{
}
