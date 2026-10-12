using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Collections.Attributes;
using Fdw.Results;
using Fdw.Tds.Endpoints.Sql.Expressions;
using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace Fdw.Tds.Endpoints.Sql.Statements.Handlers;

/// <summary>
/// SET ROWCOUNT n: the most rows each later SELECT returns; 0 lifts the limit.
/// </summary>
[TypeOption(typeof(SqlStatementHandlers), "SetRowCount", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class SetRowCountHandler : SqlStatementHandlerBase<SetRowCountStatement>
{
    /// <summary>Initializes a new instance of the <see cref="SetRowCountHandler"/> class.</summary>
    public SetRowCountHandler()
        : base("SetRowCount")
    {
    }

    /// <inheritdoc/>
    protected override Task<IGenericResult<SqlStatementOutcome>> Run(SetRowCountStatement statement, SqlExecutionContext context, CancellationToken cancellationToken)
    {
        var rows = SqlExpressions.Whole(statement.NumberRows, SqlScope.Of(context));
        if (rows.IsFailure)
            return Task.FromResult(rows.ToNewResult<SqlStatementOutcome>());

        context.Session.RowCountLimit = rows.Value < 0 ? 0 : rows.Value;
        return Ran();
    }
}
