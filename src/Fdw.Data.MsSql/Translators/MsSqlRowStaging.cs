using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using Fdw.Data.Abstractions.Rows;
using Fdw.Services.Connections.Abstractions;

namespace Fdw.Data.MsSql.Translators;

/// <summary>
/// Stages source rows for a set-based statement: the rows travel as one JSON parameter and are read back with
/// OPENJSON into a temporary table that has exactly the target's column types, so SQL Server's own assignment
/// conversion types every value. Nothing is concatenated into the statement except validated, quoted names.
/// </summary>
internal static class MsSqlRowStaging
{
    /// <summary>The name of the single parameter that carries the rows.</summary>
    internal const string RowsParameter = "@fdw_rows";

    /// <summary>Returns a temporary-table name that cannot collide with another step on the same session.</summary>
    internal static string NewTempTableName() => $"#fdw_{Guid.NewGuid():N}";

    /// <summary>
    /// Builds the statements that create <paramref name="tempTable"/> typed like <paramref name="targetTable"/>'s
    /// <paramref name="columns"/> and fill it from the rows parameter. Row counts of these statements are not reported.
    /// </summary>
    internal static string Sql(
        string tempTable,
        string targetTable,
        IReadOnlyList<string> columns,
        ISqlDialect dialect,
        bool identityInsert)
    {
        var list = string.Join(", ", columns.Select(dialect.QuoteIdentifier));
        var withList = string.Join(", ", columns.Select(c => $"{dialect.QuoteIdentifier(c)} nvarchar(max) '$.\"{c}\"'"));
        var identityOn = identityInsert ? $"SET IDENTITY_INSERT {tempTable} ON; " : string.Empty;
        var identityOff = identityInsert ? $"SET IDENTITY_INSERT {tempTable} OFF; " : string.Empty;
        return $"SELECT TOP 0 {list} INTO {tempTable} FROM {targetTable}; "
               + identityOn
               + $"INSERT INTO {tempTable} ({list}) SELECT {list} FROM OPENJSON({RowsParameter}) WITH ({withList}); "
               + identityOff;
    }

    /// <summary>
    /// Serializes <paramref name="columns"/> of every row to one JSON array. A row that lacks a column, or a binary
    /// value, is refused with a reason instead of being sent as a NULL.
    /// </summary>
    /// <param name="rows">The source rows: dictionaries of column values or entities with matching properties.</param>
    /// <param name="columns">The columns to read from each row.</param>
    /// <param name="problem">Why the rows cannot be staged, or <see langword="null"/>.</param>
    /// <returns>The JSON array, or <see langword="null"/> when <paramref name="problem"/> is set.</returns>
    internal static string? Serialize(IEnumerable rows, IReadOnlyList<string> columns, out string? problem)
    {
        var all = new List<Dictionary<string, object?>>();
        var rowNumber = 0;
        foreach (var row in rows)
        {
            rowNumber++;
            var values = new Dictionary<string, object?>(StringComparer.Ordinal);
            foreach (var column in columns)
            {
                if (!RowValues.TryRead(row, column, out var value, out _))
                {
                    problem = $"source row {rowNumber} has no column '{column}'";
                    return null;
                }

                if (value is byte[])
                {
                    problem = $"column '{column}' is binary, which a set-based load does not carry";
                    return null;
                }

                values[column] = value;
            }

            all.Add(values);
        }

        problem = null;
        return JsonSerializer.Serialize(all);
    }
}
