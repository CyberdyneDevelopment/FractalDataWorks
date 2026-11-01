using System;
using System.Collections.Generic;
using System.Linq;

namespace Fdw.Tds.Endpoints.Sql.Rows;

/// <summary>
/// Rows held in memory with the columns that name their values: a catalog view, a join of them, or the
/// rows a table's store returned.
/// </summary>
public sealed class SqlRowSet
{
    /// <summary>Initializes a new instance of the <see cref="SqlRowSet"/> class.</summary>
    /// <param name="columns">The columns, in value order.</param>
    /// <param name="rows">The rows, each holding one value per column.</param>
    public SqlRowSet(IReadOnlyList<SqlBoundColumn> columns, IReadOnlyList<object?[]> rows)
    {
        Columns = columns ?? throw new ArgumentNullException(nameof(columns));
        Rows = rows ?? throw new ArgumentNullException(nameof(rows));
    }

    /// <summary>Gets the columns, in value order.</summary>
    public IReadOnlyList<SqlBoundColumn> Columns { get; }

    /// <summary>Gets the rows.</summary>
    public IReadOnlyList<object?[]> Rows { get; }

    /// <summary>A row set with one empty row, which a SELECT with no FROM evaluates against.</summary>
    /// <returns>The row set.</returns>
    public static SqlRowSet SingleEmptyRow() => new([], [[]]);

    /// <summary>Joins two row sets: every pairing, or only those a condition keeps.</summary>
    /// <param name="left">The left row set.</param>
    /// <param name="right">The right row set.</param>
    /// <param name="keep">Answers whether a combined row is kept; null keeps every pairing.</param>
    /// <param name="leftOuter">Whether a left row nothing matched is kept, with NULLs on the right.</param>
    /// <returns>The joined row set.</returns>
    public static SqlRowSet Join(SqlRowSet left, SqlRowSet right, Func<object?[], bool>? keep, bool leftOuter)
    {
        ArgumentNullException.ThrowIfNull(left);
        ArgumentNullException.ThrowIfNull(right);
        var columns = left.Columns.Concat(right.Columns).ToList();
        var rows = new List<object?[]>();
        foreach (var l in left.Rows)
        {
            var matched = false;
            foreach (var r in right.Rows)
            {
                var combined = l.Concat(r).ToArray();
                if (keep is not null && !keep(combined))
                    continue;

                rows.Add(combined);
                matched = true;
            }

            if (leftOuter && !matched)
                rows.Add(l.Concat(new object?[right.Columns.Count]).ToArray());
        }

        return new SqlRowSet(columns, rows);
    }
}
