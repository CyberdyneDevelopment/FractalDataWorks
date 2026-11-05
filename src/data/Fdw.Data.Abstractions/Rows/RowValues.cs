using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Fdw.Data.Abstractions.Rows;

/// <summary>
/// Reads one column's value out of a row, the single way every database translator reads a row it writes (FDW-1173).
/// </summary>
/// <remarks>
/// <para>
/// The rows a command carries are of two shapes: a dictionary of column values (what a pipeline load produces) and an entity
/// whose properties are the columns. A translator that read only one of them silently wrote NULL for every column of the other,
/// and two routes that read rows differently stored different rows from the same input. Both routes of a bulk insert, the
/// serial INSERT statements and the bulk copy, now read through this one method, so they store the same rows.
/// </para>
/// <para>
/// A column is found by its exact name first and then regardless of case. A row that has no such column is reported as
/// not found, never as a null value: the caller decides what an absent column means for its statement.
/// </para>
/// </remarks>
public static class RowValues
{
    /// <summary>Reads <paramref name="column"/> from <paramref name="row"/>.</summary>
    /// <param name="row">The row: a dictionary of column values, or an entity with a property per column.</param>
    /// <param name="column">The column name.</param>
    /// <param name="value">The column's value when found; <see langword="null"/> when the column is absent or holds a null.</param>
    /// <param name="declaredType">The declared type of the entity property the value was read from; <see langword="null"/> for a dictionary row, which declares none.</param>
    /// <returns><see langword="true"/> when the row has the column; <see langword="false"/> when it does not.</returns>
    public static bool TryRead(object? row, string column, out object? value, out Type? declaredType)
    {
        value = null;
        declaredType = null;
        switch (row)
        {
            case null:
                return false;
            case IDictionary<string, object?> dictionary:
                return TryFind(dictionary.Keys, key => dictionary[key], column, out value);
            case IReadOnlyDictionary<string, object?> readOnly:
                return TryFind(readOnly.Keys, key => readOnly[key], column, out value);
        }

        var property = row.GetType().GetProperty(column, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
        if (property is null)
            return false;

        value = property.GetValue(row);
        declaredType = property.PropertyType;
        return true;
    }

    /// <summary>
    /// Reads <paramref name="column"/> from <paramref name="row"/> with the type a SQL parameter binds it as (FDW-918, FDW-3580): an entity's
    /// property type; for a dictionary row, which declares none, the column's own type when the value is null, and none for a value, which
    /// carries its own.
    /// </summary>
    /// <param name="row">The row: a dictionary of column values, or an entity with a property per column.</param>
    /// <param name="column">The column name.</param>
    /// <param name="columnType">The column's CLR type, which a null read from a dictionary row binds as.</param>
    /// <param name="value">The column's value when found; <see langword="null"/> when the column is absent or holds a null.</param>
    /// <param name="bindingType">The type to bind the value as; <see langword="null"/> when the column is absent or the value needs none.</param>
    /// <returns><see langword="true"/> when the row has the column; <see langword="false"/> when it does not.</returns>
    /// <remarks>Why here: each kind of row says what it declares, so the translators read one binding type instead of choosing between two.</remarks>
    public static bool TryReadForBinding(object? row, string column, Type columnType, out object? value, out Type? bindingType)
    {
        if (columnType == null)
            throw new ArgumentNullException(nameof(columnType));

        bindingType = null;
        switch (row)
        {
            case null:
                value = null;
                return false;
            case IDictionary<string, object?> dictionary:
                return Bound(TryFind(dictionary.Keys, key => dictionary[key], column, out value), value, columnType, out bindingType);
            case IReadOnlyDictionary<string, object?> readOnly:
                return Bound(TryFind(readOnly.Keys, key => readOnly[key], column, out value), value, columnType, out bindingType);
        }

        if (!TryRead(row, column, out value, out var declared))
            return false;

        bindingType = declared;
        return true;
    }

    // A dictionary row's binding type: the column's for a null it holds, none for a value or an absent column.
    private static bool Bound(bool found, object? value, Type columnType, out Type? bindingType)
    {
        bindingType = found && value is null ? columnType : null;
        return found;
    }

    // The exact key first, which is the common case and one lookup; otherwise the key that differs only by case.
    private static bool TryFind(IEnumerable<string> keys, Func<string, object?> read, string column, out object? value)
    {
        value = null;
        var key = keys.FirstOrDefault(k => string.Equals(k, column, StringComparison.Ordinal));
        if (key is null)
            key = keys.FirstOrDefault(k => string.Equals(k, column, StringComparison.OrdinalIgnoreCase));
        if (key is null)
            return false;

        value = read(key);
        return true;
    }
}
