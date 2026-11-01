using System;

namespace Fdw.Commands.Data.ValueSources;

/// <summary>
/// Binds a destination column to the <see cref="ColumnValueSource"/> that supplies its value.
/// </summary>
public sealed class ColumnValueBinding
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ColumnValueBinding"/> class.
    /// </summary>
    /// <param name="column">The destination column that receives the value.</param>
    /// <param name="source">Where the value comes from.</param>
    public ColumnValueBinding(string column, ColumnValueSource source)
    {
        Column = string.IsNullOrWhiteSpace(column)
            ? throw new ArgumentException("A value-source binding names its column.", nameof(column))
            : column;
        Source = source;
    }

    /// <summary>Gets the destination column that receives the value.</summary>
    public string Column { get; }

    /// <summary>Gets where the value comes from.</summary>
    public ColumnValueSource Source { get; }
}
