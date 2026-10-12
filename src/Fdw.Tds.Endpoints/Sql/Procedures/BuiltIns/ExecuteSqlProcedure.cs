using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Tds.Endpoints.Sql.Procedures.BuiltIns;

/// <summary>
/// sp_executesql @stmt [, @params [, @p = value ...]]: runs a statement with declared parameters; what every
/// parameterized query from a .NET, ODBC or JDBC client arrives as.
/// </summary>
[TypeOption(typeof(SqlProcedures), "ExecuteSql", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class ExecuteSqlProcedure : SqlProcedureBase
{
    /// <summary>Initializes a new instance of the <see cref="ExecuteSqlProcedure"/> class.</summary>
    public ExecuteSqlProcedure()
        : base(name: "ExecuteSql", sqlName: "sp_executesql")
    {
    }

    /// <inheritdoc/>
    public override async Task<IGenericResult<SqlProcedureResult>> Execute(SqlProcedureCall call, CancellationToken cancellationToken = default)
    {
        var statement = ProcedureArguments.Text(call, "@stmt", 0, required: true);
        if (statement.IsFailure)
            return statement.ToNewResult<SqlProcedureResult>();

        var declarations = ProcedureArguments.Text(call, "@params", 1, required: false);
        if (declarations.IsFailure)
            return declarations.ToNewResult<SqlProcedureResult>();

        return await ParameterizedBatch.Run(call, declarations.Value!, statement.Value!, firstValue: 2, cancellationToken).ConfigureAwait(false);
    }
}
