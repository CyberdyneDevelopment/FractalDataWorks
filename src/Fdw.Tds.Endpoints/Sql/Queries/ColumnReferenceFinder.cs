using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace Fdw.Tds.Endpoints.Sql.Queries;

/// <summary>
/// Finds whether an expression reads any column, so a value can be computed before the rows are read.
/// </summary>
internal sealed class ColumnReferenceFinder : TSqlFragmentVisitor
{
    /// <summary>Gets a value indicating whether a column reference was found.</summary>
    public bool Found { get; private set; }

    /// <inheritdoc/>
    public override void Visit(ColumnReferenceExpression node) => Found = true;
}
