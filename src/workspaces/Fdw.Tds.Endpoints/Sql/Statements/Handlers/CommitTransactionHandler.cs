using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Collections.Attributes;
using Fdw.Protocols.Tds;
using Fdw.Results;
using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace Fdw.Tds.Endpoints.Sql.Statements.Handlers;

/// <summary>
/// COMMIT TRANSACTION: closes a transaction level; the outermost reports the commit. With none open it is
/// error 3902, as SQL Server reports it.
/// </summary>
[TypeOption(typeof(SqlStatementHandlers), "CommitTransaction", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class CommitTransactionHandler : SqlStatementHandlerBase<CommitTransactionStatement>
{
    /// <summary>Initializes a new instance of the <see cref="CommitTransactionHandler"/> class.</summary>
    public CommitTransactionHandler()
        : base("CommitTransaction")
    {
    }

    /// <inheritdoc/>
    protected override Task<IGenericResult<SqlStatementOutcome>> Run(CommitTransactionStatement statement, SqlExecutionContext context, CancellationToken cancellationToken)
    {
        var session = context.Session;
        if (session.TransactionCount == 0)
        {
            return Refuse(new SqlServerMessage(3902, 1, 16,
                "The COMMIT TRANSACTION request has no corresponding BEGIN TRANSACTION.", statement.StartLine), context);
        }

        if (session.TransactionCount > 1)
            session.TransactionCount--;
        else
            TransactionTokens.End(context, committed: true);

        return Ran();
    }
}
