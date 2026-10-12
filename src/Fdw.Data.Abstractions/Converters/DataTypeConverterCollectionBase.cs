using Fdw.Collections;

namespace Fdw.Data.Abstractions;

/// <summary>
/// Base class for nested DataTypeConverter child collections (MsSql, JsonSchema, etc.).
/// </summary>
public abstract class DataTypeConverterCollectionBase(string id, string name)
    : TypeOptionBase<string, DataTypeConverterCollectionBase>(id, name),
      IDataTypeConverters
{
    // Explicit interface implementation - child classes implement via source generator
    System.Collections.Generic.IReadOnlyList<IDataTypeConverter> IDataTypeConverters.All() =>
        throw new System.NotSupportedException("Implemented by source generator in derived class");

    IDataTypeConverter IDataTypeConverters.ById(int id) =>
        throw new System.NotSupportedException("Implemented by source generator in derived class");

    IDataTypeConverter IDataTypeConverters.ByName(string name) =>
        throw new System.NotSupportedException("Implemented by source generator in derived class");

    IDataTypeConverter IDataTypeConverters.BySourceType(string sourceType) =>
        throw new System.NotSupportedException("Implemented by source generator in derived class");

    IDataTypeConverter IDataTypeConverters.NotFound =>
        throw new System.NotSupportedException("Implemented by source generator in derived class");
}
