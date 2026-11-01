using System;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Collections;
using Fdw.Results;
using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace Fdw.Tds.Endpoints.Sql.Statements;

/// <summary>
/// Runs one kind of T-SQL statement.
/// </summary>
public interface ISqlStatementHandler : ITypeOption<int, ISqlStatementHandler>
{
    /// <summary>Gets the parsed statement type this handler runs.</summary>
    Type StatementType { get; }

    /// <summary>Runs a statement.</summary>
    /// <param name="statement">The statement, of <see cref="StatementType"/>.</param>
    /// <param name="context">The request.</param>
    /// <param name="cancellationToken">A token the client's attention signal cancels.</param>
    /// <returns>How it ended, or the refusal.</returns>
    Task<IGenericResult<SqlStatementOutcome>> Execute(TSqlStatement statement, SqlExecutionContext context, CancellationToken cancellationToken = default);
}
