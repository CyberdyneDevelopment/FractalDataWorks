using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Collections.Attributes;
using Fdw.Results;
using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace Fdw.Tds.Endpoints.Sql.Statements.Handlers;

/// <summary>EXEC of a procedure this server runs.</summary>
[TypeOption(typeof(ExecutableEntityHandlers), "ExecutableProcedureReference")]
[ExcludeFromCodeCoverage]
public sealed class ExecutableProcedureReferenceHandler : ExecutableEntityHandlerBase<ExecutableProcedureReference>
{
    /// <summary>Initializes a new instance of the <see cref="ExecutableProcedureReferenceHandler"/> class.</summary>
    public ExecutableProcedureReferenceHandler()
        : base("ExecutableProcedureReference")
    {
    }

    /// <inheritdoc/>
    protected override Task<IGenericResult> Handle(ExecutableProcedureReference entity, SqlExecutionContext context, CancellationToken cancellationToken)
        => ExecuteHandler.Procedure(entity, context, cancellationToken);
}
