using System.Diagnostics.CodeAnalysis;
using Fdw.Collections;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Workspaces.Lineage;

/// <summary>
/// The directions a lineage walk can take from its focus (FDW-1091).
/// </summary>
/// <remarks>
/// A request names a direction by its lower-case word (<c>up</c>, <c>down</c>, <c>both</c>), matched exactly through
/// <c>ByWire</c>; one that matches none gets <c>NotFound</c> and is refused, never given a direction.
/// </remarks>
[ExcludeFromCodeCoverage]
[TypeCollection(typeof(LineageDirectionBase), typeof(ILineageDirection), typeof(LineageDirections))]
public abstract partial class LineageDirections : TypeCollectionBase<LineageDirectionBase, ILineageDirection>
{
}
