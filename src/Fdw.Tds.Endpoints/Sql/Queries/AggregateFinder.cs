using System.Collections.Generic;
using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace Fdw.Tds.Endpoints.Sql.Queries;

/// <summary>
/// Collects the aggregate calls of one query level, leaving those of its subqueries to them.
/// </summary>
internal sealed class AggregateFinder : TSqlFragmentVisitor
{
    /// <summary>Gets the aggregate calls found.</summary>
    public List<FunctionCall> Found { get; } = [];

    /// <inheritdoc/>
    public override void Visit(FunctionCall node)
    {
        if (node is { OverClause: null } && SqlAggregates.IsAggregate(node.FunctionName.Value))
            Found.Add(node);
    }

    /// <inheritdoc/>
    /// <remarks>A subquery's aggregates are its own, so the search does not descend into one.</remarks>
    public override void ExplicitVisit(ScalarSubquery node)
    {
    }
}
