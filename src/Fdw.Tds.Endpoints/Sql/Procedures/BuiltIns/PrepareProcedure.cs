using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Collections.Attributes;
using Fdw.Results;
using Fdw.Tds.Endpoints.Hosting;

namespace Fdw.Tds.Endpoints.Sql.Procedures.BuiltIns;

/// <summary>
/// sp_prepare @handle OUTPUT, @params, @stmt [, @options]: keeps a statement for sp_execute and hands back
/// its handle.
/// </summary>
[TypeOption(typeof(SqlProcedures), "Prepare", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class PrepareProcedure : SqlProcedureBase
{
    /// <summary>Initializes a new instance of the <see cref="PrepareProcedure"/> class.</summary>
    public PrepareProcedure()
        : base(name: "Prepare", sqlName: "sp_prepare")
    {
    }

    /// <inheritdoc/>
    public override Task<IGenericResult<SqlProcedureResult>> Execute(SqlProcedureCall call, CancellationToken cancellationToken = default)
    {
        var prepared = Prepared.Keep(call);
        return Task.FromResult(prepared.IsFailure
            ? prepared.ToNewResult<SqlProcedureResult>()
            : GenericResult<SqlProcedureResult>.Success(new SqlProcedureResult([ProcedureArguments.HandleOutput(call, prepared.Value.Handle)])));
    }
}
