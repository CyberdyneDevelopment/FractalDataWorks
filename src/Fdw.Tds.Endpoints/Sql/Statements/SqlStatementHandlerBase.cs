using System;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Collections;
using Fdw.Results;
using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace Fdw.Tds.Endpoints.Sql.Statements;

/// <summary>
/// Base class every statement handler option derives from, through <see cref="SqlStatementHandlerBase{TStatement}"/>.
/// </summary>
public abstract class SqlStatementHandlerBase : TypeOptionBase<SqlStatementHandlerBase>, ISqlStatementHandler
{
    /// <summary>Initializes a new instance of the <see cref="SqlStatementHandlerBase"/> class.</summary>
    /// <param name="name">The option's name; must match its TypeOption attribute.</param>
    /// <param name="statementType">The parsed statement type the handler runs.</param>
    protected SqlStatementHandlerBase(string name, Type statementType)
        : base(name)
    {
        StatementType = statementType;
    }

    /// <inheritdoc/>
    public Type StatementType { get; }

    /// <inheritdoc/>
    public abstract Task<IGenericResult<SqlStatementOutcome>> Execute(TSqlStatement statement, SqlExecutionContext context, CancellationToken cancellationToken = default);
}

/// <summary>
/// Base class for the statement handlers: one per parsed statement type.
/// </summary>
/// <typeparam name="TStatement">The parsed statement type the handler runs.</typeparam>
public abstract class SqlStatementHandlerBase<TStatement> : SqlStatementHandlerBase
    where TStatement : TSqlStatement
{
    /// <summary>Initializes a new instance of the <see cref="SqlStatementHandlerBase{TStatement}"/> class.</summary>
    /// <param name="name">The option's name; must match its TypeOption attribute.</param>
    protected SqlStatementHandlerBase(string name)
        : base(name, typeof(TStatement))
    {
    }

    /// <inheritdoc/>
    public sealed override Task<IGenericResult<SqlStatementOutcome>> Execute(TSqlStatement statement, SqlExecutionContext context, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(statement);
        ArgumentNullException.ThrowIfNull(context);
        return Run((TStatement)statement, context, cancellationToken);
    }

    /// <summary>Runs a statement of this handler's type.</summary>
    /// <param name="statement">The statement.</param>
    /// <param name="context">The request.</param>
    /// <param name="cancellationToken">A token the client's attention signal cancels.</param>
    /// <returns>How it ended, or the refusal.</returns>
    protected abstract Task<IGenericResult<SqlStatementOutcome>> Run(TStatement statement, SqlExecutionContext context, CancellationToken cancellationToken);

    /// <summary>Answers that the statement ran and returned no rows.</summary>
    /// <returns>The outcome.</returns>
    protected static Task<IGenericResult<SqlStatementOutcome>> Ran()
        => Task.FromResult(GenericResult<SqlStatementOutcome>.Success(SqlStatementOutcome.Ran()));

    /// <summary>Refuses the statement with a SQL Server error.</summary>
    /// <param name="message">The error.</param>
    /// <param name="context">The request.</param>
    /// <returns>The refusal.</returns>
    protected static Task<IGenericResult<SqlStatementOutcome>> Refuse(Fdw.Protocols.Tds.SqlServerMessage message, SqlExecutionContext context)
        => Task.FromResult(SqlRefusal.Refuse<SqlStatementOutcome>(message, context.Logger));
}
