using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Fdw.Collections;
using Fdw.Collections.Attributes;
using Fdw.Tds.Endpoints.Sql.Rows;

namespace Fdw.Tds.Endpoints.Sql.Catalog;

/// <summary>
/// The system catalog views this endpoint answers, found by schema and name.
/// </summary>
/// <remarks>
/// A view lists only what the caller may read. A view SQL Server has and this collection does not is
/// error 208, as any object that does not exist is; a column a view does not carry is error 207. Both name
/// exactly what a client asked for, which is how the set grows: a client's own queries show what it needs.
/// </remarks>
[TypeCollection(typeof(CatalogViewBase), typeof(ICatalogView), typeof(CatalogViews))]
[ExcludeFromCodeCoverage]
public abstract partial class CatalogViews : TypeCollectionBase<CatalogViewBase, ICatalogView>
{
    private static readonly Lazy<Dictionary<string, ICatalogView>> ByQualifiedName =
        new(static () => All().ToDictionary(v => Key(v.Schema, v.ViewName), v => (ICatalogView)v, StringComparer.OrdinalIgnoreCase));

    /// <summary>Gets a value indicating whether a schema is one of SQL Server's catalog schemas.</summary>
    /// <param name="schema">The schema's name.</param>
    /// <returns>True for sys and INFORMATION_SCHEMA.</returns>
    public static bool IsCatalogSchema(string schema)
        => string.Equals(schema, "sys", StringComparison.OrdinalIgnoreCase)
           || string.Equals(schema, "INFORMATION_SCHEMA", StringComparison.OrdinalIgnoreCase);

    /// <summary>Finds a view.</summary>
    /// <param name="schema">The schema written.</param>
    /// <param name="name">The view's name written.</param>
    /// <param name="view">The view, when there is one.</param>
    /// <returns>True when found.</returns>
    public static bool TryFind(string schema, string name, [NotNullWhen(true)] out ICatalogView? view)
        => ByQualifiedName.Value.TryGetValue(Key(schema, name), out view);

    /// <summary>Reads a view as a row set a query can name.</summary>
    /// <param name="view">The view.</param>
    /// <param name="qualifier">The alias or name the query qualifies its columns by.</param>
    /// <param name="context">The request.</param>
    /// <param name="catalog">What the caller may see.</param>
    /// <returns>The row set.</returns>
    public static SqlRowSet Read(ICatalogView view, string qualifier, SqlExecutionContext context, CatalogSnapshot catalog)
    {
        ArgumentNullException.ThrowIfNull(view);
        var columns = view.Columns.Select(c => new SqlBoundColumn(qualifier, view.Schema, c.Name, c.Type, c.IsNullable)).ToList();
        return new SqlRowSet(columns, view.Rows(context, catalog).ToList());
    }

    private static string Key(string schema, string name) => schema + "." + name;
}
