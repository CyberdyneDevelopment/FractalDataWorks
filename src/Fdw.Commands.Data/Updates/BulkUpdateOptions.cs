using System;
using System.Collections.Generic;
using System.Linq;

namespace Fdw.Commands.Data.Updates;

/// <summary>
/// The configuration of a <see cref="BulkUpdateCommand{T}"/>: the columns that identify the row each source row updates.
/// Every other column a source row carries is written.
/// </summary>
public sealed class BulkUpdateOptions
{
    /// <summary>Initializes a new instance of the <see cref="BulkUpdateOptions"/> class.</summary>
    /// <param name="keyColumns">The columns whose values select the row a source row updates.</param>
    public BulkUpdateOptions(IReadOnlyList<string> keyColumns)
    {
        KeyColumns = keyColumns;
    }

    /// <summary>Gets the columns whose values select the row a source row updates.</summary>
    public IReadOnlyList<string> KeyColumns { get; }

    /// <summary>Says what is wrong with these options, or nothing when they are consistent.</summary>
    /// <returns>The first problem, or <see langword="null"/>.</returns>
    public string? FirstProblem()
    {
        if (KeyColumns.Count == 0)
            return "a bulk update names at least one key column";
        if (KeyColumns.Any(string.IsNullOrWhiteSpace))
            return "a bulk update key column needs a name";
        return KeyColumns.Distinct(StringComparer.OrdinalIgnoreCase).Count() != KeyColumns.Count
            ? "a key column is listed twice"
            : null;
    }
}
