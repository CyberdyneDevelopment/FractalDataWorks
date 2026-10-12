using System;
using System.Collections.Generic;
using System.Linq;
using Fdw.Results;
using Fdw.Services.Etl.Logging;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Etl.Pipelines.Loads;

/// <summary>
/// Shapes the pipeline's transformed output rows for one load: restricts them to the columns the load names and adds the
/// values its value sources give the rows (FDW-1109).
/// </summary>
/// <remarks>
/// Why a copy for every load: several steps work on the same output rows, and each names its own columns and value sources,
/// so none may change the rows another reads. A column name is the destination container's column name; rows are keyed by
/// it already, as the pipeline's output is. Matching a row's field to a column ignores case, as the database does.
/// </remarks>
public static class LoadRows
{
    /// <summary>Shapes the rows for a load.</summary>
    /// <param name="owner">What the load is, for refusal messages.</param>
    /// <param name="rows">The transformed output rows.</param>
    /// <param name="columns">The columns to keep, or <see langword="null"/> to keep every field of each row.</param>
    /// <param name="values">The column values the value sources give every row.</param>
    /// <param name="logger">The logger the refusals are written to.</param>
    /// <returns>New rows, or a failure naming the column a row lacks or a column supplied twice.</returns>
    public static IGenericResult<List<IDictionary<string, object?>>> Shape(
        string owner,
        IReadOnlyList<IDictionary<string, object?>> rows,
        IReadOnlyList<string>? columns,
        IReadOnlyList<KeyValuePair<string, object?>> values,
        ILogger logger)
    {
        var twice = FirstSuppliedTwice(columns, values);
        if (twice is not null)
            return GenericResult<List<IDictionary<string, object?>>>.Failure(PipelineRunLog.ColumnSuppliedTwice(logger, owner, twice));

        if (columns is null && values.Count == 0)
            return GenericResult<List<IDictionary<string, object?>>>.Success(rows.ToList());

        var shaped = new List<IDictionary<string, object?>>(rows.Count);
        foreach (var row in rows)
        {
            string? missing = null;
            var one = columns is null ? Copy(row) : Pick(row, columns, out missing);
            if (one is null)
                return GenericResult<List<IDictionary<string, object?>>>.Failure(PipelineRunLog.RowColumnMissing(logger, owner, missing!));
            if (columns is null && values.Any(pair => one.ContainsKey(pair.Key)))
                return GenericResult<List<IDictionary<string, object?>>>.Failure(
                    PipelineRunLog.ColumnSuppliedTwice(logger, owner, values.First(pair => one.ContainsKey(pair.Key)).Key));
            foreach (var pair in values)
                one[pair.Key] = pair.Value;
            shaped.Add(one);
        }

        return GenericResult<List<IDictionary<string, object?>>>.Success(shaped);
    }

    /// <summary>Finds a field of a row by column name, ignoring case.</summary>
    /// <param name="row">The row.</param>
    /// <param name="column">The column name.</param>
    /// <param name="value">The value found.</param>
    /// <returns><see langword="true"/> when the row carries the column.</returns>
    public static bool TryFind(IDictionary<string, object?> row, string column, out object? value)
    {
        if (row.TryGetValue(column, out value))
            return true;
        foreach (var pair in row)
        {
            if (string.Equals(pair.Key, column, StringComparison.OrdinalIgnoreCase))
            {
                value = pair.Value;
                return true;
            }
        }

        value = null;
        return false;
    }

    private static string? FirstSuppliedTwice(IReadOnlyList<string>? columns, IReadOnlyList<KeyValuePair<string, object?>> values)
        => columns is null
            ? null
            : values.Select(pair => pair.Key).FirstOrDefault(name => columns.Contains(name, StringComparer.OrdinalIgnoreCase));

    private static Dictionary<string, object?> Copy(IDictionary<string, object?> row)
        => new(row, StringComparer.OrdinalIgnoreCase);

    private static Dictionary<string, object?>? Pick(IDictionary<string, object?> row, IReadOnlyList<string> columns, out string? missing)
    {
        var picked = new Dictionary<string, object?>(columns.Count, StringComparer.OrdinalIgnoreCase);
        foreach (var column in columns)
        {
            if (!TryFind(row, column, out var value))
            {
                missing = column;
                return null;
            }

            picked[column] = value;
        }

        missing = null;
        return picked;
    }
}
