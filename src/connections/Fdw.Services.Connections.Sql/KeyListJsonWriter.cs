using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.Json;

namespace Fdw.Services.Connections.Sql;

/// <summary>
/// Writes a key list as JSON for a dialect's spill form (FDW-4267).
/// </summary>
/// <remarks>
/// A Guid and a date are written as the text SQL reads back into the column's own type; a number as a JSON number; text as JSON text. A value whose JSON the
/// serializer cannot write is the serializer's own exception and is not hidden.
/// </remarks>
public static class KeyListJsonWriter
{
    /// <summary>Writes the values as a JSON array of scalars.</summary>
    /// <param name="values">The values of the list.</param>
    /// <returns>The JSON array.</returns>
    public static string Scalars(IReadOnlyList<object?> values)
    {
        var written = new List<object?>(values.Count);
        foreach (var value in values)
            written.Add(Written(value));

        return JsonSerializer.Serialize(written);
    }

    /// <summary>Writes the values as a JSON array of objects, each holding the value under the column's name.</summary>
    /// <param name="columnName">The unquoted name of the filtered column.</param>
    /// <param name="values">The values of the list.</param>
    /// <returns>The JSON array.</returns>
    public static string Objects(string columnName, IReadOnlyList<object?> values)
    {
        var written = new List<Dictionary<string, object?>>(values.Count);
        foreach (var value in values)
            written.Add(new Dictionary<string, object?>(StringComparer.Ordinal) { [columnName] = Written(value) });

        return JsonSerializer.Serialize(written);
    }

    internal static object? Written(object? value)
    {
        if (value is DateTimeOffset offset)
            return offset.ToString("O", CultureInfo.InvariantCulture);
        if (value is DateTime time)
            return time.ToString("O", CultureInfo.InvariantCulture);
        if (value is Guid id)
            return id.ToString("D", CultureInfo.InvariantCulture);

        return value;
    }
}
