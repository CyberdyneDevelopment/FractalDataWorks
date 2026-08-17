using System;
using System.Collections.Generic;
using System.Linq;
using Fdw.Data.Abstractions;

namespace Fdw.Data.DataContainers.Abstractions;

/// <summary>
/// Efficient implementation of IDataRow using array storage.
/// </summary>
public class DataRow : IDataRow
{
    private readonly object?[] _values;
    private readonly IDataSchema _schema;
    private Dictionary<string, object?>? _dictionaryCache;

    /// <summary>
    /// Initializes a new instance of the <see cref="DataRow"/> class.
    /// </summary>
    /// <param name="schema">The schema defining the row structure.</param>
    /// <param name="values">The field values.</param>
    public DataRow(IDataSchema schema, object?[] values)
    {
        _schema = schema;
        _values = values;

        if (values.Length != schema.Fields.Count)
            throw new ArgumentException(
                $"Values array length ({values.Length}) does not match schema field count ({schema.Fields.Count})",
                nameof(values));
    }

    /// <summary>
    /// Gets the number of fields in this row.
    /// </summary>
    public int FieldCount => _schema.Fields.Count;

    /// <summary>
    /// Gets the names of all fields in this row.
    /// </summary>
    public IReadOnlyList<string> FieldNames => _schema.FieldNames();

    /// <summary>
    /// Gets a typed field value by name.
    /// </summary>
    /// <typeparam name="T">The expected field type.</typeparam>
    /// <param name="fieldName">The field name.</param>
    /// <returns>The typed field value.</returns>
    /// <exception cref="KeyNotFoundException">Field not found.</exception>
    /// <exception cref="InvalidCastException">Type mismatch.</exception>
    // Why: LEFT throwing (not converted to IGenericResult<T>) — GetValue<T> is a widely-used
    // typed-accessor contract member declared on IDataRow (mirrors the ADO.NET
    // IDataRecord.GetValue convention) called in tight per-row hot loops (aggregation in
    // RuntimeDataSet.Sum/Average/Min/Max, CompiledFieldAccessor). Converting the interface
    // signature would ripple into every IDataRow consumer across the framework and force every
    // hot-loop caller to unwrap a result per field per row. Per the "indexer/contract that must
    // throw" carve-out, this stays as-is.
    public T GetValue<T>(string fieldName)
    {
        var ordinal = _schema.GetOrdinal(fieldName);
        return GetValue<T>(ordinal);
    }

    /// <summary>
    /// Gets a typed field value by ordinal position.
    /// </summary>
    /// <typeparam name="T">The expected field type.</typeparam>
    /// <param name="ordinal">The zero-based field index.</param>
    /// <returns>The typed field value.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Invalid ordinal.</exception>
    /// <exception cref="InvalidCastException">Type mismatch.</exception>
    // Why: LEFT throwing — same IDataRow hot-loop/interface-contract reasoning as the
    // string-keyed overload above (see its Why comment).
    public T GetValue<T>(int ordinal)
    {
        if (ordinal < 0 || ordinal >= _values.Length)
            throw new ArgumentOutOfRangeException(nameof(ordinal), ordinal, $"Ordinal {ordinal} out of range [0, {_values.Length})");

        var value = _values[ordinal];

        if (value == null)
        {
            if (default(T) != null)
                throw new InvalidOperationException($"Cannot cast null to non-nullable type {typeof(T).Name}");

            return default!;
        }

        if (value is T typed)
            return typed;

        // Attempt conversion
        try
        {
            return (T)Convert.ChangeType(value, typeof(T), System.Globalization.CultureInfo.InvariantCulture);
        }
        catch (Exception ex)
        {
            throw new InvalidCastException(
                $"Cannot cast field value of type {value.GetType().Name} to {typeof(T).Name}", ex);
        }
    }

    /// <summary>
    /// Tries to get a typed field value by name without throwing exceptions.
    /// </summary>
    /// <typeparam name="T">The expected field type.</typeparam>
    /// <param name="fieldName">The field name.</param>
    /// <param name="value">The field value if successful.</param>
    /// <returns>True if the field exists and conversion succeeded; otherwise false.</returns>
    public bool TryGetValue<T>(string fieldName, out T value)
    {
        if (!_schema.TryGetOrdinal(fieldName, out var ordinal))
        {
            value = default!;
            return false;
        }

        return TryGetValue(ordinal, out value);
    }

    /// <summary>
    /// Tries to get a typed field value by ordinal without throwing exceptions.
    /// </summary>
    /// <typeparam name="T">The expected field type.</typeparam>
    /// <param name="ordinal">The zero-based field index.</param>
    /// <param name="value">The field value if successful.</param>
    /// <returns>True if the ordinal is valid and conversion succeeded; otherwise false.</returns>
    public bool TryGetValue<T>(int ordinal, out T value)
    {
        if (ordinal < 0 || ordinal >= _values.Length)
        {
            value = default!;
            return false;
        }

        var rawValue = _values[ordinal];

        if (rawValue == null)
        {
            value = default!;
            return default(T) == null; // Success only if T is nullable
        }

        if (rawValue is T typed)
        {
            value = typed;
            return true;
        }

        try
        {
            value = (T)Convert.ChangeType(rawValue, typeof(T), System.Globalization.CultureInfo.InvariantCulture);
            return true;
        }
        catch (InvalidCastException ex)
        {
            // Why: conversion between incompatible types is a known failure for TryGetValue — return false.
            // ex is observed so the exception is not silently discarded.
            _ = ex;
            value = default!;
            return false;
        }
        catch (OverflowException ex)
        {
            // Why: numeric overflow during conversion is a known failure for TryGetValue — return false.
            _ = ex;
            value = default!;
            return false;
        }
        catch (FormatException ex)
        {
            // Why: format mismatch during conversion is a known failure for TryGetValue — return false.
            _ = ex;
            value = default!;
            return false;
        }
    }

    /// <summary>
    /// Gets an untyped field value by name.
    /// </summary>
    /// <param name="fieldName">The field name.</param>
    /// <returns>The untyped field value.</returns>
    /// <exception cref="KeyNotFoundException">Field not found.</exception>
    public object? GetValue(string fieldName)
    {
        var ordinal = _schema.GetOrdinal(fieldName);
        return GetValue(ordinal);
    }

    /// <summary>
    /// Gets an untyped field value by ordinal position.
    /// </summary>
    /// <param name="ordinal">The zero-based field index.</param>
    /// <returns>The untyped field value.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Invalid ordinal.</exception>
    public object? GetValue(int ordinal)
    {
        if (ordinal < 0 || ordinal >= _values.Length)
            throw new ArgumentOutOfRangeException(nameof(ordinal), ordinal, $"Ordinal {ordinal} out of range [0, {_values.Length})");

        return _values[ordinal];
    }

    /// <summary>
    /// Checks if a field with the specified name exists in this row.
    /// </summary>
    /// <param name="fieldName">The field name to check.</param>
    /// <returns>True if the field exists; otherwise false.</returns>
    public bool HasField(string fieldName)
    {
        return _schema.HasField(fieldName);
    }

    /// <summary>
    /// Converts the row to a dictionary for dynamic access scenarios.
    /// </summary>
    /// <returns>A read-only dictionary mapping field names to values.</returns>
    public IReadOnlyDictionary<string, object?> AsDictionary()
    {
        if (_dictionaryCache != null)
            return _dictionaryCache;

        var dict = new Dictionary<string, object?>(_schema.Fields.Count, StringComparer.Ordinal);
        for (int i = 0; i < _schema.Fields.Count; i++)
        {
            dict[_schema.Fields[i].Name] = _values[i];
        }

        _dictionaryCache = dict;
        return dict;
    }

    /// <summary>
    /// Get row from dictionary.
    /// </summary>
    public static DataRow FromDictionary(IDataSchema schema, IDictionary<string, object?> dictionary)
    {
        var values = new object?[schema.Fields.Count];

        for (int i = 0; i < schema.Fields.Count; i++)
        {
            var fieldName = schema.Fields[i].Name;
            if (dictionary.TryGetValue(fieldName, out var value))
                values[i] = value;
            else
                values[i] = null;
        }

        return new DataRow(schema, values);
    }

    /// <summary>
    /// Builds rows for a whole result set from dictionaries, all sharing one schema derived from the
    /// first. Returns an empty list for an empty input — a result set with no rows has no schema to
    /// derive and needs none.
    /// </summary>
    /// <remarks>
    /// Why one schema for the set rather than one per row: every row of a result set has the same
    /// columns, and a <see cref="DataRow"/> addresses its values positionally against its schema, so a
    /// per-row schema is both wasted work and a chance for the two to disagree. Each row is projected
    /// through the first row's field order, so a row that happens to be missing a key contributes a
    /// null in that position instead of shifting every later value left.
    /// <para>
    /// Lives here, beside the type it builds, because both the dataset execution path and the HTTP
    /// connection need it and neither can reference the other.
    /// </para>
    /// </remarks>
    /// <param name="rows">The source dictionaries, one per row.</param>
    /// <returns>One <see cref="IDataRow"/> per input dictionary.</returns>
    public static IReadOnlyList<IDataRow> FromDictionaries(IReadOnlyList<IDictionary<string, object?>> rows)
    {
        if (rows.Count == 0)
            return [];

        var schema = DataSchema.FromFields(
            rows[0].Select((kvp, index) =>
                (ISchemaField)new SchemaField(kvp.Key, kvp.Value?.GetType() ?? typeof(object), index)).ToList());

        var result = new List<IDataRow>(rows.Count);
        foreach (var row in rows)
            result.Add(FromDictionary(schema, row));

        return result;
    }

    /// <summary>
    /// Get single-field row.
    /// </summary>
    public static DataRow SingleField(string fieldName, object? value)
    {
        var schema = DataSchema.FromFields([new SchemaField(fieldName, value?.GetType() ?? typeof(object), 0)]);
        return new DataRow(schema, [value]);
    }
}