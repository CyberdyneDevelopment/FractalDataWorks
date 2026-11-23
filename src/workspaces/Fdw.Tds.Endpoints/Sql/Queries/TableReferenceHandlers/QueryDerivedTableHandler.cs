using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;
using Fdw.Tds.Endpoints.Sql.Rows;
using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace Fdw.Tds.Endpoints.Sql.Queries;

/// <summary>Reads a derived table: a subquery in FROM.</summary>
[TypeOption(typeof(TableReferenceHandlers), "QueryDerivedTable")]
[ExcludeFromCodeCoverage]
public sealed class QueryDerivedTableHandler : TableReferenceHandlerBase<QueryDerivedTable>
{
    /// <summary>Initializes a new instance of the <see cref="QueryDerivedTableHandler"/> class.</summary>
    public QueryDerivedTableHandler()
        : base("QueryDerivedTable")
    {
    }

    /// <inheritdoc/>
    protected override IGenericResult<SqlRowSet> Handle(QueryDerivedTable reference, SqlScope outer)
        => InMemorySelect.Derived(reference, outer);
}
