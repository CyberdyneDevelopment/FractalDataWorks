using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Tds.Endpoints.Sql.Procedures.BuiltIns;

/// <summary>
/// sp_unprepare @handle: forgets a statement sp_prepare or sp_prepexec kept.
/// </summary>
[TypeOption(typeof(SqlProcedures), "Unprepare", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class UnprepareProcedure : SqlProcedureBase
{
    /// <summary>Initializes a new instance of the <see cref="UnprepareProcedure"/> class.</summary>
    public UnprepareProcedure()
        : base(name: "Unprepare", sqlName: "sp_unprepare")
    {
    }

    /// <inheritdoc/>
    public override Task<IGenericResult<SqlProcedureResult>> Execute(SqlProcedureCall call, CancellationToken cancellationToken = default)
    {
        var handle = ProcedureArguments.Handle(call, "@handle", 0);
        if (handle.IsFailure)
            return Task.FromResult(handle.ToNewResult<SqlProcedureResult>());

        return Task.FromResult(call.Context.Session.Unprepare(handle.Value)
            ? GenericResult<SqlProcedureResult>.Success(SqlProcedureResult.Nothing())
            : SqlRefusal.Refuse<SqlProcedureResult>(SqlServerErrors.UnknownPreparedHandle(handle.Value), call.Context.Logger));
    }
}
