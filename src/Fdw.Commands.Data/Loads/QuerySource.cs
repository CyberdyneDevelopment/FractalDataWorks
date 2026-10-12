using System.Collections.Generic;
using Fdw.Data.Abstractions;

namespace Fdw.Commands.Data.Loads;

/// <summary>
/// The source of an insert-from-query: one table or view on the same server as the target (it may be in another
/// database on that server), an optional filter and an optional order.
/// </summary>
/// <remarks>
/// Only names are passed; the connection type quotes them. Nothing here is a free-form query string.
/// </remarks>
public sealed class QuerySource
{
    /// <summary>
    /// Initializes a new instance of the <see cref="QuerySource"/> class.
    /// </summary>
    /// <param name="table">The source table or view name.</param>
    /// <param name="schema">The source schema, or <see langword="null"/> for the connection's default.</param>
    /// <param name="database">The source database on the same server, or <see langword="null"/> for the target's own.</param>
    public QuerySource(string table, string? schema = null, string? database = null)
    {
        Table = table;
        Schema = schema;
        Database = database;
    }

    /// <summary>Gets the source table or view name.</summary>
    public string Table { get; }

    /// <summary>Gets the source schema, or <see langword="null"/> for the connection's default.</summary>
    public string? Schema { get; }

    /// <summary>Gets the source database on the same server, or <see langword="null"/> for the target's own.</summary>
    public string? Database { get; }

    /// <summary>Gets the filter that picks the source rows, or <see langword="null"/> for every row.</summary>
    public IFilterExpression? Filter { get; init; }

    /// <summary>Gets the source columns the selected rows are ordered by, or <see langword="null"/> for no order.</summary>
    public IReadOnlyList<string>? OrderBy { get; init; }
}
