using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Collections.Attributes;
using Fdw.Results;
using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace Fdw.Tds.Endpoints.Sql.Statements.Handlers;

/// <summary>
/// SAVE TRANSACTION: a savepoint, which holds nothing here because nothing is written.
/// </summary>
[TypeOption(typeof(SqlStatementHandlers), "SaveTransaction", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class SaveTransactionHandler : SqlStatementHandlerBase<SaveTransactionStatement>
{
    /// <summary>Initializes a new instance of the <see cref="SaveTransactionHandler"/> class.</summary>
    public SaveTransactionHandler()
        : base("SaveTransaction")
    {
    }

    /// <inheritdoc/>
    protected override Task<IGenericResult<SqlStatementOutcome>> Run(SaveTransactionStatement statement, SqlExecutionContext context, CancellationToken cancellationToken) => Ran();
}
