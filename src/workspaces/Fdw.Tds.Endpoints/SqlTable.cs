using System;
using System.Collections.Generic;

namespace Fdw.Tds.Endpoints;

/// <summary>
/// A table this endpoint exposes: where it sits, its columns, and what the closure reads it by.
/// </summary>
public sealed class SqlTable
{
    /// <summary>Initializes a new instance of the <see cref="SqlTable"/> class.</summary>
    /// <param name="schema">The schema it sits in.</param>
    /// <param name="name">Its name.</param>
    /// <param name="columns">Its columns, in ordinal order.</param>
    /// <param name="pagesInStore">Whether its store applies ordering and paging itself.</param>
    /// <param name="target">What the closure reads it by; opaque to the endpoint.</param>
    /// <param name="created">When it was created, as <c>sys.objects.create_date</c> reports.</param>
    /// <param name="modified">When it last changed, as <c>sys.objects.modify_date</c> reports.</param>
    public SqlTable(string schema, string name, IReadOnlyList<SqlTableColumn> columns, bool pagesInStore, string target, DateTime created, DateTime modified)
    {
        Schema = schema ?? throw new ArgumentNullException(nameof(schema));
        Name = name ?? throw new ArgumentNullException(nameof(name));
        Columns = columns ?? throw new ArgumentNullException(nameof(columns));
        PagesInStore = pagesInStore;
        Target = target ?? throw new ArgumentNullException(nameof(target));
        Created = created;
        Modified = modified;
    }

    /// <summary>Gets the schema it sits in.</summary>
    public string Schema { get; }

    /// <summary>Gets its name.</summary>
    public string Name { get; }

    /// <summary>Gets its columns, in ordinal order.</summary>
    public IReadOnlyList<SqlTableColumn> Columns { get; }

    /// <summary>Gets a value indicating whether its store applies ordering and paging itself.</summary>
    /// <remarks>
    /// When it does not, a query that orders or skips rows is refused: the store would hand back rows in
    /// its own order, and an ORDER BY the client wrote would silently not hold.
    /// </remarks>
    public bool PagesInStore { get; }

    /// <summary>Gets what the closure reads it by.</summary>
    public string Target { get; }

    /// <summary>Gets when it was created.</summary>
    public DateTime Created { get; }

    /// <summary>Gets when it last changed.</summary>
    public DateTime Modified { get; }

    /// <summary>Gets the name as T-SQL writes it: [schema].[name].</summary>
    /// <returns>The two-part name.</returns>
    public override string ToString() => $"[{Schema}].[{Name}]";
}
