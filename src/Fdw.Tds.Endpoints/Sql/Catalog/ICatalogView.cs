using System.Collections.Generic;
using Fdw.Collections;

namespace Fdw.Tds.Endpoints.Sql.Catalog;

/// <summary>
/// A system catalog view: a sys or INFORMATION_SCHEMA view a client reads to learn what the server holds.
/// </summary>
public interface ICatalogView : ITypeOption<int, ICatalogView>
{
    /// <summary>Gets the view's schema: sys or INFORMATION_SCHEMA.</summary>
    string Schema { get; }

    /// <summary>Gets the view's name, such as tables.</summary>
    string ViewName { get; }

    /// <summary>Gets the view's columns, in SQL Server's order.</summary>
    IReadOnlyList<CatalogColumn> Columns { get; }

    /// <summary>Gets the view's rows for one request, one value per column, in column order.</summary>
    /// <param name="context">The request.</param>
    /// <param name="catalog">What the caller may see.</param>
    /// <returns>The rows.</returns>
    IEnumerable<object?[]> Rows(SqlExecutionContext context, CatalogSnapshot catalog);
}
