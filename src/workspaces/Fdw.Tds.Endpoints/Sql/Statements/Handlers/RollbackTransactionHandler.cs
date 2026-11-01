using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Collections.Attributes;
using Fdw.Protocols.Tds;
using Fdw.Results;
using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace Fdw.Tds.Endpoints.Sql.Statements.Handlers;

/// <summary>
/// ROLLBACK TRANSACTION: closes every transaction level and reports the rollback. With none open it is
/// error 3903, as SQL Server reports it.
/// </summary>
[TypeOption(typeof(SqlStatementHandlers), "RollbackTransaction", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class RollbackTransactionHandler : SqlStatementHandlerBase<RollbackTransactionStatement>
{
    /// <summary>Initializes a new instance of the <see cref="RollbackTransactionHandler"/> class.</summary>
    public RollbackTransactionHandler()
        : base("RollbackTransaction")
    {
    }

    /// <inheritdoc/>
    protected override Task<IGenericResult<SqlStatementOutcome>> Run(RollbackTransactionStatement statement, SqlExecutionContext context, CancellationToken cancellationToken)
    {
        if (context.Session.TransactionCount == 0)
        {
            return Refuse(new SqlServerMessage(3903, 1, 16,
                "The ROLLBACK TRANSACTION request has no corresponding BEGIN TRANSACTION.", statement.StartLine), context);
        }

        TransactionTokens.End(context, committed: false);
        return Ran();
    }
}
