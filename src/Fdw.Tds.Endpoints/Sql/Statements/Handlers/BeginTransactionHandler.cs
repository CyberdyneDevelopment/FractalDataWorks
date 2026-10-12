using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Collections.Attributes;
using Fdw.Results;
using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace Fdw.Tds.Endpoints.Sql.Statements.Handlers;

/// <summary>
/// BEGIN TRANSACTION: opens a transaction level. A distributed transaction is refused.
/// </summary>
[TypeOption(typeof(SqlStatementHandlers), "BeginTransaction", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class BeginTransactionHandler : SqlStatementHandlerBase<BeginTransactionStatement>
{
    /// <summary>Initializes a new instance of the <see cref="BeginTransactionHandler"/> class.</summary>
    public BeginTransactionHandler()
        : base("BeginTransaction")
    {
    }

    /// <inheritdoc/>
    protected override Task<IGenericResult<SqlStatementOutcome>> Run(BeginTransactionStatement statement, SqlExecutionContext context, CancellationToken cancellationToken)
    {
        if (statement.Distributed)
            return Refuse(SqlServerErrors.NotSupported("BEGIN DISTRIBUTED TRANSACTION", statement.StartLine), context);

        TransactionTokens.Begin(context);
        return Ran();
    }
}
