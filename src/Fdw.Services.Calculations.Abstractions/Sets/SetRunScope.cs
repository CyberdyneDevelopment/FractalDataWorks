using System;
using System.Collections.Generic;
using System.Globalization;
using Fdw.Results;

namespace Fdw.Services.Calculations.Abstractions.Sets;

/// <summary>
/// What one evaluation of a calculation set works on: the single values, the tables by name and the limits. It also holds the
/// lookup indexes built so far, so a table is indexed once per run.
/// </summary>
public sealed class SetRunScope
{
    private readonly IReadOnlyDictionary<string, IReadOnlyList<IReadOnlyDictionary<string, object?>>> _tables;
    private readonly Dictionary<string, Dictionary<string, IReadOnlyDictionary<string, object?>>> _indexes = new(StringComparer.Ordinal);

    /// <summary>Initializes a new instance of the <see cref="SetRunScope"/> class.</summary>
    /// <param name="scalars">The single values the calculation reads by name.</param>
    /// <param name="tables">The rows of each table input, by the table's name.</param>
    /// <param name="limits">The limits on rows.</param>
    public SetRunScope(
        IReadOnlyDictionary<string, object?> scalars,
        IReadOnlyDictionary<string, IReadOnlyList<IReadOnlyDictionary<string, object?>>> tables,
        SetLimits limits)
    {
        Scalars = scalars;
        _tables = tables;
        Limits = limits;
    }

    /// <summary>Gets the single values the calculation reads by name.</summary>
    public IReadOnlyDictionary<string, object?> Scalars { get; }

    /// <summary>Gets the totals worked out over a whole table so far, so one is worked out once per run and not once per row.</summary>
    public Dictionary<string, object?> Memo { get; } = new(StringComparer.Ordinal);

    /// <summary>Gets the limits on rows.</summary>
    public SetLimits Limits { get; }

    /// <summary>Turns a value into the text two rows are compared by: the same value written the same way.</summary>
    /// <param name="value">The key value.</param>
    /// <returns>The key text.</returns>
    public static string KeyText(object? value) => Convert.ToString(value, CultureInfo.InvariantCulture) ?? string.Empty;

    /// <summary>Gets the rows of a table input.</summary>
    /// <param name="output">The output asking, for a refusal.</param>
    /// <param name="name">The table's name.</param>
    /// <returns>The rows, or a refusal when the run supplied no such table.</returns>
    public IGenericResult<IReadOnlyList<IReadOnlyDictionary<string, object?>>> Table(string output, string name) =>
        _tables.TryGetValue(name, out var rows)
            ? GenericResult<IReadOnlyList<IReadOnlyDictionary<string, object?>>>.Success(rows)
            : SetFailure.Of<IReadOnlyList<IReadOnlyDictionary<string, object?>>>("SetTableMissing", output, $"table '{name}' was not supplied");

    /// <summary>Gets a table indexed by one column, building the index the first time.</summary>
    /// <param name="output">The output asking, for a refusal.</param>
    /// <param name="table">The table's name.</param>
    /// <param name="keyColumn">The column the rows are found by.</param>
    /// <returns>The index, or a refusal when the table is missing, too large, lacks the column, or has two rows for one key.</returns>
    public IGenericResult<Dictionary<string, IReadOnlyDictionary<string, object?>>> Index(string output, string table, string keyColumn)
    {
        var cacheKey = string.Concat(table, "\u001f", keyColumn);
        if (_indexes.TryGetValue(cacheKey, out var cached))
        {
            return GenericResult<Dictionary<string, IReadOnlyDictionary<string, object?>>>.Success(cached);
        }

        var rows = Table(output, table);
        if (rows.IsFailure)
        {
            return rows.ToNewResult<Dictionary<string, IReadOnlyDictionary<string, object?>>>();
        }

        var built = Build(output, table, keyColumn, rows.Value!);
        if (built.IsSuccess)
        {
            _indexes[cacheKey] = built.Value!;
        }

        return built;
    }

    private IGenericResult<Dictionary<string, IReadOnlyDictionary<string, object?>>> Build(
        string output, string table, string keyColumn, IReadOnlyList<IReadOnlyDictionary<string, object?>> rows)
    {
        if (rows.Count > Limits.MaxLookupRows)
        {
            return SetFailure.Of<Dictionary<string, IReadOnlyDictionary<string, object?>>>(
                "SetRowLimitExceeded", output, $"lookup table '{table}' has {rows.Count} rows; the limit is {Limits.MaxLookupRows}");
        }

        var index = new Dictionary<string, IReadOnlyDictionary<string, object?>>(rows.Count, StringComparer.Ordinal);
        foreach (var row in rows)
        {
            if (!row.TryGetValue(keyColumn, out var key))
            {
                return SetFailure.Of<Dictionary<string, IReadOnlyDictionary<string, object?>>>(
                    "SetColumnMissing", output, $"lookup table '{table}' has a row without column '{keyColumn}'");
            }

            var text = KeyText(key);
            if (index.ContainsKey(text))
            {
                return SetFailure.Of<Dictionary<string, IReadOnlyDictionary<string, object?>>>(
                    "SetLookupKeyAmbiguous", output, $"lookup table '{table}' has two rows with {keyColumn} '{text}'");
            }

            index[text] = row;
        }

        return GenericResult<Dictionary<string, IReadOnlyDictionary<string, object?>>>.Success(index);
    }
}
