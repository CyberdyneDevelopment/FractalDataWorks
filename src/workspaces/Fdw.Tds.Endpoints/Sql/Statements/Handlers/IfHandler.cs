using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Collections.Attributes;
using Fdw.Results;
using Fdw.Tds.Endpoints.Sql.Expressions;
using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace Fdw.Tds.Endpoints.Sql.Statements.Handlers;

/// <summary>
/// IF condition statement [ELSE statement]: runs the branch the condition chooses; an unknown condition
/// takes the ELSE, as SQL Server's does.
/// </summary>
[TypeOption(typeof(SqlStatementHandlers), "If", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class IfHandler : SqlStatementHandlerBase<IfStatement>
{
    /// <summary>Initializes a new instance of the <see cref="IfHandler"/> class.</summary>
    public IfHandler()
        : base("If")
    {
    }

    /// <inheritdoc/>
    protected override async Task<IGenericResult<SqlStatementOutcome>> Run(IfStatement statement, SqlExecutionContext context, CancellationToken cancellationToken)
    {
        var holds = SqlPredicates.Evaluate(statement.Predicate, SqlScope.Of(context));
        if (holds.IsFailure)
            return holds.ToNewResult<SqlStatementOutcome>();

        var branch = holds.Value == true ? statement.ThenStatement : statement.ElseStatement;
        if (branch is null)
            return GenericResult<SqlStatementOutcome>.Success(SqlStatementOutcome.Ran());

        var ran = await SqlBatchRunner.Run([branch], context, cancellationToken).ConfigureAwait(false);
        return ran.IsFailure
            ? ran.ToNewResult<SqlStatementOutcome>()
            : GenericResult<SqlStatementOutcome>.Success(SqlStatementOutcome.Block());
    }
}
