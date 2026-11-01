using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Collections.Attributes;
using Fdw.Results;
using Fdw.Tds.Endpoints.Sql.Expressions;
using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace Fdw.Tds.Endpoints.Sql.Statements.Handlers;

/// <summary>
/// SET @name = value: assigns a declared variable, converted to its declared type.
/// </summary>
/// <remarks>The compound forms (+=, -= and their kin) and cursor assignments are refused with error 40517.</remarks>
[TypeOption(typeof(SqlStatementHandlers), "SetVariable", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class SetVariableHandler : SqlStatementHandlerBase<SetVariableStatement>
{
    /// <summary>Initializes a new instance of the <see cref="SetVariableHandler"/> class.</summary>
    public SetVariableHandler()
        : base("SetVariable")
    {
    }

    /// <inheritdoc/>
    protected override Task<IGenericResult<SqlStatementOutcome>> Run(SetVariableStatement statement, SqlExecutionContext context, CancellationToken cancellationToken)
    {
        if (statement.AssignmentKind != AssignmentKind.Equals || statement.CursorDefinition is not null || statement.Expression is null)
            return Refuse(SqlServerErrors.NotSupported(SqlText.Of(statement), statement.StartLine), context);

        var assigned = SqlAssignment.Assign(statement.Variable, statement.Expression, SqlScope.Of(context));
        return assigned.IsFailure ? Task.FromResult(assigned.ToNewResult<SqlStatementOutcome>()) : Ran();
    }
}
