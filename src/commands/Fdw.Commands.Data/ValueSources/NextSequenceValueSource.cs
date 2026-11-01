using System;
using System.Collections.Generic;

namespace Fdw.Commands.Data.ValueSources;

/// <summary>
/// A column value that is the next value of a database sequence, handed out in the order of the listed
/// columns of the rows being written (the ordered batch form).
/// </summary>
/// <remarks>
/// The sequence name, its schema and database, and the ordering columns are all configuration the caller
/// passes. The order-by columns name columns of the rows being written (the source rows).
/// </remarks>
public sealed class NextSequenceValueSource : ColumnValueSource
{
    /// <summary>The kind name connection types render.</summary>
    public const string KindName = "NextSequenceValue";

    /// <summary>
    /// Initializes a new instance of the <see cref="NextSequenceValueSource"/> class.
    /// </summary>
    /// <param name="sequenceName">The sequence's name.</param>
    /// <param name="orderBy">The columns that order the rows receiving values; at least one.</param>
    /// <param name="schema">The sequence's schema, or <see langword="null"/> for the connection's default.</param>
    /// <param name="database">The sequence's database, or <see langword="null"/> for the connection's own.</param>
    public NextSequenceValueSource(
        string sequenceName,
        IReadOnlyList<string> orderBy,
        string? schema = null,
        string? database = null)
        : base(KindName)
    {
        SequenceName = string.IsNullOrWhiteSpace(sequenceName)
            ? throw new ArgumentException("A sequence value source names its sequence.", nameof(sequenceName))
            : sequenceName;
        OrderBy = orderBy;
        Schema = schema;
        Database = database;
    }

    /// <summary>Gets the sequence's name.</summary>
    public string SequenceName { get; }

    /// <summary>Gets the columns that order the rows receiving values.</summary>
    public IReadOnlyList<string> OrderBy { get; }

    /// <summary>Gets the sequence's schema, or <see langword="null"/> for the connection's default.</summary>
    public string? Schema { get; }

    /// <summary>Gets the sequence's database, or <see langword="null"/> for the connection's own.</summary>
    public string? Database { get; }
}
