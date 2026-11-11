using System;
using System.Collections.Generic;
using System.Linq;
using Fdw.Commands.Data.Logging;
using Fdw.Messages;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Commands.Data.Updates;

/// <summary>
/// The configuration of a <see cref="BulkUpdateCommand{T}"/>: the columns that identify the row each source row updates,
/// and the columns written to it. A connection stages every source row's key and write columns and updates the matched
/// rows in one set-based statement (FDW-4953).
/// </summary>
public sealed class BulkUpdateOptions
{
    /// <summary>Initializes a new instance of the <see cref="BulkUpdateOptions"/> class.</summary>
    /// <param name="keyColumns">The columns whose values select the row a source row updates.</param>
    /// <param name="writeColumns">The columns written to the matched row. A key column listed here is matched on, never set.</param>
    public BulkUpdateOptions(IReadOnlyList<string> keyColumns, IReadOnlyList<string> writeColumns)
    {
        KeyColumns = keyColumns;
        WriteColumns = writeColumns;
    }

    /// <summary>Gets the columns whose values select the row a source row updates.</summary>
    public IReadOnlyList<string> KeyColumns { get; }

    /// <summary>Gets the columns written to the matched row.</summary>
    public IReadOnlyList<string> WriteColumns { get; }

    /// <summary>Gets the write columns that are set: the write columns that are not key columns, in order.</summary>
    public IReadOnlyList<string> SetColumns
        => WriteColumns.Where(column => !KeyColumns.Contains(column, StringComparer.OrdinalIgnoreCase)).ToList();

    /// <summary>Gets every column a source row carries for the update: the key columns, then the set columns.</summary>
    public IReadOnlyList<string> StagedColumns => [.. KeyColumns, .. SetColumns];

    /// <summary>Says what is wrong with these options, or nothing when they are consistent.</summary>
    /// <returns>The first problem, or <see langword="null"/>.</returns>
    public IGenericMessage? FirstProblem()
    {
        if (KeyColumns.Count == 0)
            return Problem("a bulk update names at least one key column");
        if (KeyColumns.Any(string.IsNullOrWhiteSpace) || WriteColumns.Any(string.IsNullOrWhiteSpace))
            return Problem("a bulk update column needs a name");
        if (KeyColumns.Distinct(StringComparer.OrdinalIgnoreCase).Count() != KeyColumns.Count
            || WriteColumns.Distinct(StringComparer.OrdinalIgnoreCase).Count() != WriteColumns.Count)
            return Problem("a column is listed twice");
        return SetColumns.Count == 0
            ? Problem("a bulk update writes at least one column that is not a key column")
            : null;
    }

    private static IGenericMessage Problem(string reason)
        => CommandOptionsLog.BulkUpdateOptionsInconsistent(NullLogger.Instance, reason);
}
