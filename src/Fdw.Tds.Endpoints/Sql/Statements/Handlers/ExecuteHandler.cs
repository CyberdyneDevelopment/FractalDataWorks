using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Collections.Attributes;
using Fdw.Results;
using Fdw.Tds.Endpoints.Sql.Expressions;
using Fdw.Tds.Endpoints.Sql.Procedures;
using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace Fdw.Tds.Endpoints.Sql.Statements.Handlers;

/// <summary>
/// EXEC: a procedure this server runs (sp_executesql and the prepared-statement procedures), or EXEC('text'),
/// which runs the text as a batch of its own.
/// </summary>
/// <remarks>
/// A procedure's OUTPUT parameters are assigned back to the variables the call names. Any procedure this
/// server does not run is error 2812, as SQL Server reports one it cannot find.
/// </remarks>
[TypeOption(typeof(SqlStatementHandlers), "Execute", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class ExecuteHandler : SqlStatementHandlerBase<ExecuteStatement>
{
    /// <summary>Initializes a new instance of the <see cref="ExecuteHandler"/> class.</summary>
    public ExecuteHandler()
        : base("Execute")
    {
    }

    /// <inheritdoc/>
    protected override async Task<IGenericResult<SqlStatementOutcome>> Run(ExecuteStatement statement, SqlExecutionContext context, CancellationToken cancellationToken)
    {
        var entity = statement.ExecuteSpecification.ExecutableEntity;
        var ran = entity switch
        {
            ExecutableStringList text => await Text(text, context, cancellationToken).ConfigureAwait(false),
            ExecutableProcedureReference procedure => await Procedure(procedure, context, cancellationToken).ConfigureAwait(false),
            _ => SqlRefusal.Refuse(SqlServerErrors.NotSupported(SqlText.Of(statement), statement.StartLine), context.Logger),
        };

        return ran.IsFailure
            ? ran.ToNewResult<SqlStatementOutcome>()
            : GenericResult<SqlStatementOutcome>.Success(SqlStatementOutcome.Block());
    }

    // EXEC('...'): the text runs as a batch with variables of its own, as SQL Server runs it.
    private static async Task<IGenericResult> Text(ExecutableStringList list, SqlExecutionContext context, CancellationToken cancellationToken)
    {
        var text = new StringBuilder();
        foreach (var part in list.Strings)
        {
            var value = SqlExpressions.Evaluate(part, SqlScope.Of(context));
            if (value.IsFailure)
                return value;

            text.Append(value.Value is null ? string.Empty : SqlValues.ToText(value.Value));
        }

        var nested = context.Nested(new SqlVariables(), context.InProcedure, list.StartLine);
        if (nested.IsFailure)
            return nested;

        return await SqlBatchRunner.Run(text.ToString(), nested.Value!, cancellationToken).ConfigureAwait(false);
    }

    private static async Task<IGenericResult> Procedure(ExecutableProcedureReference reference, SqlExecutionContext context, CancellationToken cancellationToken)
    {
        if (reference.ProcedureReference?.ProcedureReference is not { } named)
            return SqlRefusal.Refuse(SqlServerErrors.NotSupported("EXEC of a procedure named by a variable", reference.StartLine), context.Logger);

        var name = named.Name;
        if (!SqlProcedures.TryFind(name.BaseIdentifier.Value, out var procedure))
            return SqlRefusal.Refuse(SqlServerErrors.ProcedureNotFound(SqlText.Of(name), reference.StartLine), context.Logger);

        var arguments = Arguments(reference.Parameters, context);
        if (arguments.IsFailure)
            return arguments;

        var result = await procedure.Execute(new SqlProcedureCall(context, procedure.SqlName, arguments.Value!, reference.StartLine), cancellationToken).ConfigureAwait(false);
        if (result.IsFailure)
            return result;

        return AssignOutputs(reference.Parameters, result.Value!, context);
    }

    private static IGenericResult<IReadOnlyList<SqlProcedureArgument>> Arguments(IList<ExecuteParameter> parameters, SqlExecutionContext context)
    {
        var arguments = new List<SqlProcedureArgument>(parameters.Count);
        for (var i = 0; i < parameters.Count; i++)
        {
            var value = SqlExpressions.Evaluate(parameters[i].ParameterValue, SqlScope.Of(context));
            if (value.IsFailure)
                return value.ToNewResult<IReadOnlyList<SqlProcedureArgument>>();

            var name = parameters[i].Variable?.Name is { } named ? named : string.Empty;
            arguments.Add(new SqlProcedureArgument(i, name, value.Value, parameters[i].IsOutput));
        }

        return GenericResult<IReadOnlyList<SqlProcedureArgument>>.Success(arguments);
    }

    // EXEC p @x = @y OUTPUT: the procedure's output for that argument is assigned to @y.
    private static IGenericResult AssignOutputs(IList<ExecuteParameter> parameters, SqlProcedureResult result, SqlExecutionContext context)
    {
        foreach (var output in result.Outputs)
        {
            if (parameters[output.Ordinal].ParameterValue is VariableReference target && !context.Variables.TrySet(target.Name, output.Value))
                return SqlRefusal.Refuse(SqlServerErrors.MustDeclareVariable(target.Name, target.StartLine), context.Logger);
        }

        return GenericResult.Success();
    }
}
