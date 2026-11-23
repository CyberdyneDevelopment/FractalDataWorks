using System.Diagnostics.CodeAnalysis;
using Fdw.Collections;
using Fdw.Collections.Attributes;

namespace Fdw.Operations.Thresholds;

/// <summary>
/// The ways a stored filter group combines what is in it: <c>All</c> (every one holds) and <c>Any</c> (one holds).
/// </summary>
/// <remarks>A stored group names its operator by its word (<c>All</c>, <c>Any</c>), matched exactly through <c>ByWord</c>; a word that matches none gets <c>NotFound</c>, and the rule is refused.</remarks>
[ExcludeFromCodeCoverage]
[TypeCollection(typeof(ThresholdGroupOperatorBase), typeof(IThresholdGroupOperator), typeof(ThresholdGroupOperators))]
public abstract partial class ThresholdGroupOperators : TypeCollectionBase<ThresholdGroupOperatorBase, IThresholdGroupOperator>
{
}
