using System;
using System.Collections.Generic;

namespace Fdw.Data.DataContainers.Abstractions;

/// <summary>
/// Represents a single row of data with field access by name or ordinal.
/// Provides high-performance access for both streaming and materialized scenarios.
/// </summary>
public interface IDataRow
{
    /// <summary>
    /// Get field value by name (slower - requires dictionary lookup).
    /// </summary>
    /// <typeparam name="T">Expected field type</typeparam>
    /// <param name="fieldName">Field name</param>
    /// <returns>Typed field value</returns>
    /// <exception cref="KeyNotFoundException">Field not found</exception>
    /// <exception cref="InvalidCastException">Type mismatch</exception>
    T GetValue<T>(string fieldName);

    /// <summary>
    /// Get field value by ordinal (faster - direct array access).
    /// </summary>
    /// <typeparam name="T">Expected field type</typeparam>
    /// <param name="ordinal">Zero-based field index</param>
    /// <returns>Typed field value</returns>
    /// <exception cref="IndexOutOfRangeException">Invalid ordinal</exception>
    /// <exception cref="InvalidCastException">Type mismatch</exception>
    T GetValue<T>(int ordinal);

    /// <summary>
    /// Try get field value by name (safe, no exceptions).
    /// </summary>
    bool TryGetValue<T>(string fieldName, out T value);

    /// <summary>
    /// Try get field value by ordinal (safe, no exceptions).
    /// </summary>
    bool TryGetValue<T>(int ordinal, out T value);

    /// <summary>
    /// Get untyped field value by name.
    /// </summary>
    object? GetValue(string fieldName);

    /// <summary>
    /// Get untyped field value by ordinal.
    /// </summary>
    object? GetValue(int ordinal);

    /// <summary>
    /// Check if field exists.
    /// </summary>
    bool HasField(string fieldName);

    /// <summary>
    /// Get all field names.
    /// </summary>
    IReadOnlyList<string> FieldNames { get; }

    /// <summary>
    /// Get number of fields.
    /// </summary>
    int FieldCount { get; }

    /// <summary>
    /// Get field value as dictionary (for dynamic scenarios).
    /// </summary>
    IReadOnlyDictionary<string, object?> AsDictionary();
}