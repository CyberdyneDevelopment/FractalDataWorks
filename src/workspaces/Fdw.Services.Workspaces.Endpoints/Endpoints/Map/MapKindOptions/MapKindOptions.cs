using System.Diagnostics.CodeAnalysis;
using Fdw.Collections;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Workspaces.Endpoints;

/// <summary>The kinds of node the map draws or counts, found by the string a node carries (see <see cref="MapKinds"/>).</summary>
/// <remarks>A kind no option holds is a kind a workspace's own resource carries (the set a map reads is open); the generated <c>ByName</c> answers <c>NotFound</c> for it and the caller refuses.</remarks>
[TypeCollection(typeof(MapKindOptionBase), typeof(IMapKindOption), typeof(MapKindOptions))]
[ExcludeFromCodeCoverage]
public abstract partial class MapKindOptions : TypeCollectionBase<MapKindOptionBase, IMapKindOption>
{
}
