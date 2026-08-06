using System;
using System.Collections;
using System.Collections.Generic;
using System.Data.Common;
using Fdw.Collections;
using Fdw.Configuration;
using Fdw.Results;

namespace Fdw.Data.Abstractions.Mappers.PocoMappers;

/// <summary>
/// Maps database reader rows to POCO instances.
/// Part of the PocoMappers TypeCollection for compile-time discovery.
/// Key is the fully-qualified type name.
/// </summary>
public interface IPocoMapper : ITypeOption<string, PocoMapperBase>
{
    /// <summary>
    /// The .NET type this mapper creates.
    /// </summary>
    Type TargetType { get; }

    /// <summary>
    /// Maps a DbDataReader row to a POCO instance.
    /// </summary>
    /// <param name="reader">The data reader positioned at the row to map.</param>
    /// <param name="container">Container metadata with schema information.</param>
    /// <returns>Result containing the mapped POCO or failure information.</returns>
    IGenericResult<object> MapFromReader(DbDataReader reader, IStorageContainer container);

    /// <summary>
    /// Maps a dictionary to a POCO instance.
    /// Used for calculated field execution.
    /// </summary>
    /// <param name="data">Dictionary of field values.</param>
    /// <returns>Result containing the mapped POCO or failure information.</returns>
    IGenericResult<object> MapFromDictionary(IDictionary<string, object?> data);

    /// <summary>
    /// Gets the names of all mapped properties (columns) for this type.
    /// Used by translators to build column lists without reflection.
    /// </summary>
    /// <returns>The list of property names in declaration order.</returns>
    IReadOnlyList<string> GetPropertyNames();

    /// <summary>
    /// Extracts property values from an instance as a name-value dictionary.
    /// Used by translators to build SQL parameters without reflection.
    /// </summary>
    /// <param name="instance">The POCO instance to extract values from.</param>
    /// <returns>Dictionary of property name to value.</returns>
    IReadOnlyDictionary<string, object?> MapToParameters(object instance);

    /// <summary>
    /// Sets a single mapped property on an instance by column name (reflection-free generated switch).
    /// Used by the save cascade to stamp a child's foreign-key column whose name is only known at
    /// runtime. An unknown column name is a no-op (the column may not exist on every type).
    /// </summary>
    /// <param name="instance">The POCO instance to mutate.</param>
    /// <param name="columnName">The mapped column (property) name to set.</param>
    /// <param name="value">The value to assign.</param>
    void SetValue(object instance, string columnName, object? value);

    /// <summary>
    /// Creates an empty strongly-typed <see cref="List{T}"/> for this mapper's type.
    /// Reflection-free replacement for <c>Activator.CreateInstance(typeof(List&lt;&gt;).MakeGenericType(...))</c>
    /// when materializing a collection result.
    /// </summary>
    /// <returns>An empty <see cref="IList"/> whose element type is <see cref="TargetType"/>.</returns>
    IList CreateList();

    /// <summary>
    /// Creates a strongly-typed array of <see cref="TargetType"/> with the given length.
    /// Reflection-free replacement for <c>Array.CreateInstance(itemType, length)</c>.
    /// </summary>
    /// <param name="length">The array length.</param>
    /// <returns>An <see cref="Array"/> whose element type is <see cref="TargetType"/>.</returns>
    Array CreateArray(int length);

    /// <summary>
    /// Returns the single typed-body child configuration (the <c>Configuration</c> property) of a
    /// parent instance, or <see langword="null"/> when the parent has none. Reflection-free
    /// replacement for the typed-body <c>GetProperty("Configuration")</c> lookup in the save cascade.
    /// </summary>
    /// <param name="parent">The parent configuration instance.</param>
    IGenericConfiguration? GetTypedBody(object parent);

    /// <summary>
    /// Sets the single typed-body child configuration (the <c>Configuration</c> property) on a parent
    /// instance. Reflection-free mirror of <see cref="GetTypedBody"/> used by the configuration load
    /// path to attach a composed typed body. A no-op when the type has no typed-body property.
    /// </summary>
    /// <param name="parent">The parent configuration instance to mutate.</param>
    /// <param name="body">The typed-body configuration to assign, or <see langword="null"/>.</param>
    void SetTypedBody(object parent, IGenericConfiguration? body);

    /// <summary>
    /// The child relationships of this type that the configuration cascade loads and saves, each
    /// exposing reflection-free parent-side accessors. Empty when the type has no child collections
    /// or typed-body children.
    /// </summary>
    IReadOnlyList<IChildCascadeDescriptor> CascadeChildren { get; }
}
