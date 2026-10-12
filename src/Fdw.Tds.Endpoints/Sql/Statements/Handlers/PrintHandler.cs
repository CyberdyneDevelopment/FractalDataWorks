using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Collections.Attributes;
using Fdw.Protocols.Tds;
using Fdw.Results;
using Fdw.Tds.Endpoints.Sql.Expressions;
using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace Fdw.Tds.Endpoints.Sql.Statements.Handlers;

/// <summary>
/// PRINT expression: an informational message, number 0 and class 0, as SQL Server sends one.
/// </summary>
[TypeOption(typeof(SqlStatementHandlers), "Print", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class PrintHandler : SqlStatementHandlerBase<PrintStatement>
{
    /// <summary>Initializes a new instance of the <see cref="PrintHandler"/> class.</summary>
    public PrintHandler()
        : base("Print")
    {
    }

    /// <inheritdoc/>
    protected override Task<IGenericResult<SqlStatementOutcome>> Run(PrintStatement statement, SqlExecutionContext context, CancellationToken cancellationToken)
    {
        var value = SqlExpressions.Evaluate(statement.Expression, SqlScope.Of(context));
        if (value.IsFailure)
            return Task.FromResult(value.ToNewResult<SqlStatementOutcome>());

        var text = value.Value is null ? string.Empty : SqlValues.ToText(value.Value);
        TdsTokenWriter.Message(context.Response.Payload, new SqlServerMessage(0, 1, 0, text, statement.StartLine), context.Server.ServerName);
        return Ran();
    }
}
