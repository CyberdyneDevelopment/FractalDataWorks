using System;
using System.Collections.Generic;
using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace Fdw.Tds.Endpoints.Sql.Queries;

/// <summary>
/// The questions a statement is asked before it runs.
/// </summary>
public static class FragmentSearch
{
    /// <summary>Gets the aggregate calls of one query level.</summary>
    /// <param name="fragment">The fragment.</param>
    /// <returns>The calls, in the order written.</returns>
    public static IReadOnlyList<FunctionCall> Aggregates(TSqlFragment fragment)
    {
        ArgumentNullException.ThrowIfNull(fragment);
        var finder = new AggregateFinder();
        fragment.Accept(finder);
        return finder.Found;
    }

    /// <summary>Gets a value indicating whether a fragment reads any column.</summary>
    /// <param name="fragment">The fragment.</param>
    /// <returns>True when it does.</returns>
    public static bool ReadsColumns(TSqlFragment fragment)
    {
        ArgumentNullException.ThrowIfNull(fragment);
        var finder = new ColumnReferenceFinder();
        fragment.Accept(finder);
        return finder.Found;
    }

    /// <summary>Gets a value indicating whether a statement reads the system catalog.</summary>
    /// <param name="fragment">The statement.</param>
    /// <returns>True when it does.</returns>
    public static bool ReadsCatalog(TSqlFragment fragment)
    {
        ArgumentNullException.ThrowIfNull(fragment);
        var finder = new CatalogNeedFinder();
        fragment.Accept(finder);
        return finder.Found;
    }
}
