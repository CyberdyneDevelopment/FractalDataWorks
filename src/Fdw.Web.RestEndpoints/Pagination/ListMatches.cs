using System.Diagnostics.CodeAnalysis;
using Fdw.Collections;
using Fdw.Collections.Attributes;

namespace Fdw.Web.RestEndpoints.Pagination;

/// <summary>
/// The ways a list's search text is compared with a name (FDW-1194).
/// </summary>
/// <remarks>
/// A request names a match by its word (<c>contains</c>, <c>startsWith</c>, <c>exact</c>), matched exactly through
/// <c>ByWire</c>; one that matches none gets <c>NotFound</c> and is refused, never given a match. Each option states the
/// filter operator it means, so a list builds its filter without switching on the word.
/// </remarks>
[ExcludeFromCodeCoverage]
[TypeCollection(typeof(ListMatchBase), typeof(IListMatch), typeof(ListMatches))]
public abstract partial class ListMatches : TypeCollectionBase<ListMatchBase, IListMatch>
{
}
