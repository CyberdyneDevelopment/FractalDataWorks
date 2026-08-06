using System.Diagnostics.CodeAnalysis;
using Fdw.Collections;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Etl.Abstractions.OptionTypes;

/// <summary>
/// Collection of lookup join types (Inner, Left) available to Lookup transforms. The runtime dispatches
/// join semantics via <c>LookupJoinTypes.ByName(joinType).FailOnMissing</c> instead of an if/else.
/// </summary>
[ExcludeFromCodeCoverage]
[TypeCollection(typeof(LookupJoinTypeBase), typeof(ILookupJoinType), typeof(LookupJoinTypes))]
public abstract partial class LookupJoinTypes : TypeCollectionBase<LookupJoinTypeBase, ILookupJoinType>
{
    // DO NOT IMPLEMENT BY HAND! Source generator populates ByName/ById/All/NotFound.
}
