using System.Diagnostics.CodeAnalysis;
using Fdw.Collections;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Workspaces.Endpoints;

/// <summary>The relations a map edge can have, found by the string an edge carries (see <see cref="MapRelations"/>).</summary>
/// <remarks>A relation no option holds is one the map did not build; the generated <c>ByName</c> answers <c>NotFound</c> and the lineage projection refuses.</remarks>
[TypeCollection(typeof(MapRelationOptionBase), typeof(IMapRelationOption), typeof(MapRelationOptions))]
[ExcludeFromCodeCoverage]
public abstract partial class MapRelationOptions : TypeCollectionBase<MapRelationOptionBase, IMapRelationOption>
{
}
