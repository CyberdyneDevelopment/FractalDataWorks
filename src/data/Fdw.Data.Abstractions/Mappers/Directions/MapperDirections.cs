using System.Diagnostics.CodeAnalysis;
using Fdw.Collections;
using Fdw.Collections.Attributes;

namespace Fdw.Data;

/// <summary>
/// TypeCollection of the mapping directions a generated POCO mapper can emit. <c>[GenerateMapper(Direction = typeof(...))]</c> names
/// one of its option types, because an attribute argument must be a constant. PocoMapperGenerator, which cannot reference this
/// collection, reads the option type by its full name: <c>Fdw.Data.InputMapperDirection</c>, <c>Fdw.Data.OutputMapperDirection</c>
/// or <c>Fdw.Data.BothMapperDirection</c>.
/// </summary>
[TypeCollection(typeof(MapperDirectionBase), typeof(IMapperDirection), typeof(MapperDirections))]
[ExcludeFromCodeCoverage]
public abstract partial class MapperDirections : TypeCollectionBase<MapperDirectionBase, IMapperDirection>
{
}
