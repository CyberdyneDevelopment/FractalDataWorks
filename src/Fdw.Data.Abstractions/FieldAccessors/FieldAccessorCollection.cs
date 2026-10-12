using Fdw.Collections;
using Fdw.Collections.Attributes;

namespace Fdw.Data.Abstractions.FieldAccessors;

/// <summary>
/// TypeCollection of field accessors for POCO types.
/// Accessors are registered via [TypeOption(typeof(FieldAccessorCollection), "TypeName", RestrictToCurrentCompilation = true)] on generated accessor classes.
/// Generated accessors eliminate reflection overhead for field/property value extraction.
/// </summary>
/// <remarks>
/// Usage example:
/// <code>
/// var accessor = FieldAccessorCollection.ByName("TeamStats");
/// var result = accessor.GetDecimalValue(record, "YardsPerGame");
/// </code>
/// </remarks>
[TypeCollection(typeof(FieldAccessorBase), typeof(IFieldAccessor), typeof(FieldAccessorCollection))]
public abstract partial class FieldAccessorCollection : TypeCollectionBase<FieldAccessorBase, IFieldAccessor>
{
}
