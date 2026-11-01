using System.Collections.Generic;

namespace Fdw.Commands.Data.SoftDeletes;

/// <summary>
/// The keys of a source set. A soft delete over it flags the target rows whose keys are absent from the set (rows
/// that disappeared from the source).
/// </summary>
public sealed class AbsentKeySet
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AbsentKeySet"/> class.
    /// </summary>
    /// <param name="keyColumns">The key columns that identify a row in both the source set and the target.</param>
    /// <param name="rows">The source rows, each a dictionary holding at least the key columns.</param>
    public AbsentKeySet(IReadOnlyList<string> keyColumns, IEnumerable<IReadOnlyDictionary<string, object?>> rows)
    {
        KeyColumns = keyColumns;
        Rows = rows;
    }

    /// <summary>Gets the key columns that identify a row in both the source set and the target.</summary>
    public IReadOnlyList<string> KeyColumns { get; }

    /// <summary>Gets the source rows, each holding at least the key columns.</summary>
    public IEnumerable<IReadOnlyDictionary<string, object?>> Rows { get; }
}
