using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Collections.Attributes;
using Fdw.Results;
using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace Fdw.Tds.Endpoints.Sql.Statements.Handlers;

/// <summary>EXEC('text'): the text runs as a batch of its own.</summary>
[TypeOption(typeof(ExecutableEntityHandlers), "ExecutableStringList")]
[ExcludeFromCodeCoverage]
public sealed class ExecutableStringListHandler : ExecutableEntityHandlerBase<ExecutableStringList>
{
    /// <summary>Initializes a new instance of the <see cref="ExecutableStringListHandler"/> class.</summary>
    public ExecutableStringListHandler()
        : base("ExecutableStringList")
    {
    }

    /// <inheritdoc/>
    protected override Task<IGenericResult> Handle(ExecutableStringList entity, SqlExecutionContext context, CancellationToken cancellationToken)
        => ExecuteHandler.Text(entity, context, cancellationToken);
}
