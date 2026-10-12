using System;

namespace Fdw.Commands.Data.ValueSources;

/// <summary>
/// Where a column's value comes from when it is not read from a source row: a source field, a constant, the
/// current timestamp, or the next value of a sequence. Usable wherever a command supplies a column value.
/// </summary>
/// <remarks>
/// A value source is a description, not SQL. Each connection type renders it in its own dialect through its
/// own value-source collection, keyed by <see cref="Kind"/>.
/// </remarks>
public abstract class ColumnValueSource
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ColumnValueSource"/> class.
    /// </summary>
    /// <param name="kind">The kind name a connection type's value-source collection renders.</param>
    protected ColumnValueSource(string kind)
    {
        Kind = string.IsNullOrWhiteSpace(kind)
            ? throw new ArgumentException("A value source names its kind.", nameof(kind))
            : kind;
    }

    /// <summary>Gets the kind name: <c>Column</c>, <c>Constant</c>, <c>CurrentTimestamp</c> or <c>NextSequenceValue</c>.</summary>
    public string Kind { get; }
}
