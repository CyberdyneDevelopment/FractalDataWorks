using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Tds.Endpoints.Sql.Procedures.BuiltIns;

/// <summary>
/// sp_execute @handle [, value ...]: runs a statement sp_prepare kept. A handle never prepared, or since
/// unprepared, is error 8179.
/// </summary>
[TypeOption(typeof(SqlProcedures), "Execute", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class ExecuteProcedure : SqlProcedureBase
{
    /// <summary>Initializes a new instance of the <see cref="ExecuteProcedure"/> class.</summary>
    public ExecuteProcedure()
        : base(name: "Execute", sqlName: "sp_execute")
    {
    }

    /// <inheritdoc/>
    public override async Task<IGenericResult<SqlProcedureResult>> Execute(SqlProcedureCall call, CancellationToken cancellationToken = default)
    {
        var handle = ProcedureArguments.Handle(call, "@handle", 0);
        if (handle.IsFailure)
            return handle.ToNewResult<SqlProcedureResult>();

        if (!call.Context.Session.TryGetPrepared(handle.Value, out var prepared))
            return SqlRefusal.Refuse<SqlProcedureResult>(SqlServerErrors.UnknownPreparedHandle(handle.Value), call.Context.Logger);

        return await ParameterizedBatch.Run(call, prepared.ParameterDeclarations, prepared.Text, firstValue: 1, cancellationToken).ConfigureAwait(false);
    }
}
