namespace Fdw.Data.Components.Models;

using Fdw.Collections;
using Fdw.Collections.Attributes;

/// <summary>
/// TypeCollection for field mapping types.
/// Use <c>ByName</c> for O(1) lookup from a string value.
/// </summary>
[TypeCollection(typeof(MappingTypeBase), typeof(IMappingType), typeof(MappingTypes))]
public abstract partial class MappingTypes : TypeCollectionBase<MappingTypeBase, IMappingType> { }
