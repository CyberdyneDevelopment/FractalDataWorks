using System.Collections.Generic;
using Fdw.Collections;

namespace Fdw.Data.Abstractions;

/// <summary>
/// Interface for DataTypeConverter child collections (MsSql, JsonSchema, etc.).
/// </summary>
public interface IDataTypeConverters : ITypeOption<string, DataTypeConverterCollectionBase>
{
    /// <summary>
    /// Gets all converters in this type system.
    /// </summary>
    IReadOnlyList<IDataTypeConverter> All();

    /// <summary>
    /// Gets converter by ID.
    /// </summary>
    IDataTypeConverter ById(int id);

    /// <summary>
    /// Gets converter by name.
    /// </summary>
    IDataTypeConverter ByName(string name);

    /// <summary>
    /// Gets converter by source type name.
    /// </summary>
    IDataTypeConverter BySourceType(string sourceType);

    /// <summary>
    /// Gets the not-found sentinel.
    /// </summary>
    IDataTypeConverter NotFound { get; }
}
