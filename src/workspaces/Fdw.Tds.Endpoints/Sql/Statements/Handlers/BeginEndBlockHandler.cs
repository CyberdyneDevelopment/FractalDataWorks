using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Collections.Attributes;
using Fdw.Results;
using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace Fdw.Tds.Endpoints.Sql.Statements.Handlers;

/// <summary>
/// BEGIN ... END: runs the block's statements in order, each ending in its own DONE.
/// </summary>
[TypeOption(typeof(SqlStatementHandlers), "BeginEndBlock", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class BeginEndBlockHandler : SqlStatementHandlerBase<BeginEndBlockStatement>
{
    /// <summary>Initializes a new instance of the <see cref="BeginEndBlockHandler"/> class.</summary>
    public BeginEndBlockHandler()
        : base("BeginEndBlock")
    {
    }

    /// <inheritdoc/>
    protected override async Task<IGenericResult<SqlStatementOutcome>> Run(BeginEndBlockStatement statement, SqlExecutionContext context, CancellationToken cancellationToken)
    {
        var ran = await SqlBatchRunner.Run(statement.StatementList.Statements, context, cancellationToken).ConfigureAwait(false);
        return ran.IsFailure
            ? ran.ToNewResult<SqlStatementOutcome>()
            : GenericResult<SqlStatementOutcome>.Success(SqlStatementOutcome.Block());
    }
}
