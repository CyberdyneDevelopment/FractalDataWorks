using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.Json;
using Fdw.Data.Abstractions.Rows;

namespace Fdw.Services.Connections.Sql;

/// <summary>
/// The rows of a set-based write (FDW-4944): one statement applies a whole batch, so the batch is read once here, a key two
/// rows share is collapsed to the last of them (the row a row-by-row write would have left), and the rows are written as
/// one JSON array when the dialect stages them that way.
/// </summary>
public static class SetBasedRows
{
    /// <summary>
    /// Keeps the last row for each key: a set-based UPDATE that joins two source rows to one target row applies either, so
    /// the batch is made to hold one row for each key, the one a row-by-row update would have written last.
    /// </summary>
    /// <param name="rows">The source rows, dictionaries of column values or entities.</param>
    /// <param name="keyColumns">The key columns.</param>
    /// <param name="collapsed">How many rows were dropped because a later row has the same key.</param>
    /// <returns>The rows, one for each key, in the order each key's last row came.</returns>
    public static IReadOnlyList<object> LastPerKey(IEnumerable rows, IReadOnlyList<string> keyColumns, out int collapsed)
    {
        var all = rows.Cast<object>().ToList();
        var last = new Dictionary<string, int>(StringComparer.Ordinal);
        for (var index = 0; index < all.Count; index++)
            last[Key(all[index], keyColumns)] = index;

        collapsed = all.Count - last.Count;
        if (collapsed == 0)
            return all;

        var kept = new HashSet<int>(last.Values);
        return all.Where((_, index) => kept.Contains(index)).ToList();
    }

    /// <summary>
    /// Writes <paramref name="columns"/> of every row as one JSON array of objects, each value under its column's name. A row
    /// that lacks a column, or a binary value, is refused with a reason instead of being sent as a NULL.
    /// </summary>
    /// <param name="rows">The rows.</param>
    /// <param name="columns">The columns to read from each row, and the names the JSON gives them.</param>
    /// <param name="problem">Why the rows cannot be written, or <see langword="null"/>.</param>
    /// <returns>The JSON array, or <see langword="null"/> when <paramref name="problem"/> is set.</returns>
    public static string? Json(IReadOnlyList<object> rows, IReadOnlyList<string> columns, out string? problem)
    {
        var written = new List<Dictionary<string, object?>>(rows.Count);
        for (var index = 0; index < rows.Count; index++)
        {
            var values = new Dictionary<string, object?>(StringComparer.Ordinal);
            foreach (var column in columns)
            {
                if (!RowValues.TryRead(rows[index], column, out var value, out _))
                {
                    problem = $"source row {index + 1} has no column '{column}'";
                    return null;
                }

                if (value is byte[])
                {
                    problem = $"column '{column}' is binary, which a set-based write does not carry";
                    return null;
                }

                values[column] = KeyListJsonWriter.Written(value);
            }

            written.Add(values);
        }

        problem = null;
        return JsonSerializer.Serialize(written);
    }

    // A null (or absent) key value is its own part, so it never collapses with an empty text key; such a row matches nothing in the join anyway.
    private static string Key(object row, IReadOnlyList<string> keyColumns)
        => string.Join("\u001f", keyColumns.Select(column => KeyPart(row, column)));

    private static string KeyPart(object row, string column)
        => RowValues.TryRead(row, column, out var value, out _) && value is not null
            ? "v" + Convert.ToString(KeyListJsonWriter.Written(value), CultureInfo.InvariantCulture)
            : "n";
}
