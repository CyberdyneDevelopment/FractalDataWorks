using Fdw.Collections;
using Fdw.Collections.Attributes;
using System.Diagnostics.CodeAnalysis;

namespace Fdw.Data.Abstractions;

/// <summary>
/// Parent TypeCollection for all DataTypeConverter child collections.
/// Contains child collections: MsSql, JsonSchema, etc.
/// Uses source generator for compile-time discovery.
/// </summary>
[ExcludeFromCodeCoverage]
[TypeCollection(typeof(DataTypeConverterCollectionBase),
                typeof(IDataTypeConverters),
                typeof(DataTypeConverters))]
public abstract partial class DataTypeConverters
    : TypeCollectionBase<DataTypeConverterCollectionBase, IDataTypeConverters>
{
    // Source generator creates:
    // - public static IDataTypeConverters MsSql { get; }
    // - public static IDataTypeConverters JsonSchema { get; } (future)
    // - public static IDataTypeConverters ById(string id) { ... }
    // - public static IDataTypeConverters ByName(string name) { ... }
    // - public static IReadOnlyList<IDataTypeConverters> All() { ... }
    // - public static IDataTypeConverters NotFound() { ... }
}
