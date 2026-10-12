using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace Fdw.Tds.Endpoints.Sql.Queries;

/// <summary>
/// Finds a column read outside any aggregate, which an aggregated query without GROUP BY cannot answer.
/// </summary>
internal sealed class UngroupedColumnFinder : TSqlFragmentVisitor
{
    /// <summary>Gets the first such column, or null when every column is read inside an aggregate.</summary>
    public ColumnReferenceExpression? Found { get; private set; }

    /// <inheritdoc/>
    public override void Visit(ColumnReferenceExpression node)
    {
        if (Found is null)
            Found = node;
    }

    /// <inheritdoc/>
    /// <remarks>Columns inside an aggregate are aggregated, so the search does not descend into one.</remarks>
    public override void ExplicitVisit(FunctionCall node)
    {
        if (!SqlAggregates.IsAggregate(node.FunctionName.Value))
            base.ExplicitVisit(node);
    }

    /// <inheritdoc/>
    public override void ExplicitVisit(ScalarSubquery node)
    {
    }
}
