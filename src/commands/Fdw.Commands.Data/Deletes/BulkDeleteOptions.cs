using System;
using System.Collections.Generic;
using System.Linq;
using Fdw.Commands.Data.Logging;
using Fdw.Messages;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Commands.Data.Deletes;

/// <summary>
/// The configuration of a <see cref="BulkDeleteCommand{T}"/>: the columns whose values select the rows a batch removes (or
/// flags). A connection stages every source row's keys and deletes the matched rows in one set-based statement (FDW-4954).
/// </summary>
public sealed class BulkDeleteOptions
{
    /// <summary>Initializes a new instance of the <see cref="BulkDeleteOptions"/> class.</summary>
    /// <param name="keyColumns">The columns whose values select the row each source row deletes.</param>
    public BulkDeleteOptions(IReadOnlyList<string> keyColumns)
    {
        KeyColumns = keyColumns;
    }

    /// <summary>Gets the columns whose values select the row each source row deletes.</summary>
    public IReadOnlyList<string> KeyColumns { get; }

    /// <summary>Says what is wrong with these options, or nothing when they are consistent.</summary>
    /// <returns>The first problem, or <see langword="null"/>.</returns>
    public IGenericMessage? FirstProblem()
    {
        if (KeyColumns.Count == 0)
            return Problem("a bulk delete names at least one key column");
        if (KeyColumns.Any(string.IsNullOrWhiteSpace))
            return Problem("a bulk delete key column needs a name");
        return KeyColumns.Distinct(StringComparer.OrdinalIgnoreCase).Count() != KeyColumns.Count
            ? Problem("a key column is listed twice")
            : null;
    }

    private static IGenericMessage Problem(string reason)
        => CommandOptionsLog.BulkDeleteOptionsInconsistent(NullLogger.Instance, reason);
}
