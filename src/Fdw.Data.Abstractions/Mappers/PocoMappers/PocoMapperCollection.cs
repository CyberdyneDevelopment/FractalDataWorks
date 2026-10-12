using Fdw.Collections;
using Fdw.Collections.Attributes;

namespace Fdw.Data.Abstractions.Mappers.PocoMappers;

/// <summary>
/// TypeCollection of POCO mappers.
/// Mappers are registered via [TypeOption(typeof(PocoMapperCollection), "TypeName", RestrictToCurrentCompilation = true)] on generated mapper classes.
/// Generated mappers eliminate reflection overhead for DbDataReader to POCO mapping.
/// </summary>
[TypeCollection(typeof(PocoMapperBase), typeof(IPocoMapper), typeof(PocoMapperCollection))]
public abstract partial class PocoMapperCollection : TypeCollectionBase<PocoMapperBase, IPocoMapper>
{
}
