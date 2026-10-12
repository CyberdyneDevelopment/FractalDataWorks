using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Collections.Attributes;
using Fdw.Results;
using Fdw.Tds.Endpoints.Sql.Expressions;
using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace Fdw.Tds.Endpoints.Sql.Statements.Handlers;

/// <summary>
/// SET TEXTSIZE n: kept for @@TEXTSIZE. It bounds text, ntext and image values, which this server never
/// writes, so it changes no value returned.
/// </summary>
[TypeOption(typeof(SqlStatementHandlers), "SetTextSize", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class SetTextSizeHandler : SqlStatementHandlerBase<SetTextSizeStatement>
{
    // SET TEXTSIZE 0 resets to SQL Server's own 4,096; -1 is unlimited.
    private const int ResetSize = 4096;

    /// <summary>Initializes a new instance of the <see cref="SetTextSizeHandler"/> class.</summary>
    public SetTextSizeHandler()
        : base("SetTextSize")
    {
    }

    /// <inheritdoc/>
    protected override Task<IGenericResult<SqlStatementOutcome>> Run(SetTextSizeStatement statement, SqlExecutionContext context, CancellationToken cancellationToken)
    {
        var size = SqlExpressions.Whole(statement.TextSize, SqlScope.Of(context));
        if (size.IsFailure)
            return Task.FromResult(size.ToNewResult<SqlStatementOutcome>());

        context.Session.TextSize = size.Value switch
        {
            0 => ResetSize,
            < 0 or > int.MaxValue => int.MaxValue,
            _ => (int)size.Value,
        };
        return Ran();
    }
}
