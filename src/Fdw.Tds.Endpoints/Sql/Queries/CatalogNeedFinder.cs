using System;
using Fdw.Tds.Endpoints.Sql.Catalog;
using Fdw.Tds.Endpoints.Sql.Functions;
using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace Fdw.Tds.Endpoints.Sql.Queries;

/// <summary>
/// Finds whether a statement reads the system catalog: a sys or INFORMATION_SCHEMA view, or a function
/// such as OBJECT_ID that answers from it.
/// </summary>
internal sealed class CatalogNeedFinder : TSqlFragmentVisitor
{
    /// <summary>Gets a value indicating whether the statement reads the catalog.</summary>
    public bool Found { get; private set; }

    /// <inheritdoc/>
    public override void Visit(NamedTableReference node)
    {
        if (node.SchemaObject.SchemaIdentifier?.Value is { } schema && CatalogViews.IsCatalogSchema(schema))
            Found = true;
    }

    /// <inheritdoc/>
    public override void Visit(FunctionCall node)
    {
        if (CatalogLookup.Functions.Contains(node.FunctionName.Value))
            Found = true;
    }
}
