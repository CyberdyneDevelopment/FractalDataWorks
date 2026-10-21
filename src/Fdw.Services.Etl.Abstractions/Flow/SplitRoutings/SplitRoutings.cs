using System.Diagnostics.CodeAnalysis;
using Fdw.Collections;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Etl.Abstractions.Flow;

/// <summary>
/// How a split sends a row that meets more than one filter: FirstMatch or AllMatches (FDW-1386).
/// </summary>
[ExcludeFromCodeCoverage]
[TypeCollection(typeof(SplitRoutingBase), typeof(ISplitRouting), typeof(SplitRoutings))]
public abstract partial class SplitRoutings : TypeCollectionBase<SplitRoutingBase, ISplitRouting>
{
    // DO NOT IMPLEMENT BY HAND! Source generator populates ByName/ById/All/NotFound.
}
