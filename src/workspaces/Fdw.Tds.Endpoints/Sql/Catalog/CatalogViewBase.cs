using System.Collections.Generic;
using Fdw.Collections;

namespace Fdw.Tds.Endpoints.Sql.Catalog;

/// <summary>
/// Base class for the system catalog views.
/// </summary>
public abstract class CatalogViewBase : TypeOptionBase<CatalogViewBase>, ICatalogView
{
    /// <summary>Initializes a new instance of the <see cref="CatalogViewBase"/> class.</summary>
    /// <param name="name">The option's name; must match its TypeOption attribute.</param>
    /// <param name="schema">The view's schema.</param>
    /// <param name="viewName">The view's name.</param>
    protected CatalogViewBase(string name, string schema, string viewName)
        : base(name)
    {
        Schema = schema;
        ViewName = viewName;
    }

    /// <inheritdoc/>
    public string Schema { get; }

    /// <inheritdoc/>
    public string ViewName { get; }

    /// <inheritdoc/>
    public abstract IReadOnlyList<CatalogColumn> Columns { get; }

    /// <inheritdoc/>
    public abstract IEnumerable<object?[]> Rows(SqlExecutionContext context, CatalogSnapshot catalog);
}
