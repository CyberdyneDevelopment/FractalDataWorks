using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Tds.Endpoints.Sql.Procedures.BuiltIns;

/// <summary>
/// sp_prepexec @handle OUTPUT, @params, @stmt [, value ...]: keeps a statement and runs it at once; how a
/// JDBC client and SqlClient's prepared commands send a query.
/// </summary>
[TypeOption(typeof(SqlProcedures), "PrepExec", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class PrepExecProcedure : SqlProcedureBase
{
    /// <summary>Initializes a new instance of the <see cref="PrepExecProcedure"/> class.</summary>
    public PrepExecProcedure()
        : base(name: "PrepExec", sqlName: "sp_prepexec")
    {
    }

    /// <inheritdoc/>
    public override async Task<IGenericResult<SqlProcedureResult>> Execute(SqlProcedureCall call, CancellationToken cancellationToken = default)
    {
        var prepared = Prepared.Keep(call);
        if (prepared.IsFailure)
            return prepared.ToNewResult<SqlProcedureResult>();

        var (handle, statement) = prepared.Value;
        var ran = await ParameterizedBatch.Run(call, statement.ParameterDeclarations, statement.Text, firstValue: 3, cancellationToken).ConfigureAwait(false);
        if (ran.IsFailure)
            return ran;

        var outputs = new List<SqlProcedureOutput> { ProcedureArguments.HandleOutput(call, handle) };
        outputs.AddRange(ran.Value!.Outputs);
        return GenericResult<SqlProcedureResult>.Success(new SqlProcedureResult(outputs));
    }
}
