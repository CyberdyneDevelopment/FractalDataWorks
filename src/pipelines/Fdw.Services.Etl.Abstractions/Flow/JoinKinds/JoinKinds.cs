using System.Diagnostics.CodeAnalysis;
using Fdw.Collections;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Etl.Abstractions.Flow;

/// <summary>
/// Which rows a merge join keeps when they find no partner: Inner, LeftOuter or FullOuter (FDW-1386).
/// </summary>
[ExcludeFromCodeCoverage]
[TypeCollection(typeof(JoinKindBase), typeof(IJoinKind), typeof(JoinKinds))]
public abstract partial class JoinKinds : TypeCollectionBase<JoinKindBase, IJoinKind>
{
    // DO NOT IMPLEMENT BY HAND! Source generator populates ByName/ById/All/NotFound.
}
