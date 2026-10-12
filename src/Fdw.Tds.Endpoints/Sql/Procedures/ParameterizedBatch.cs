using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Protocols.Tds;
using Fdw.Results;
using Fdw.Tds.Endpoints.Sql.Expressions;
using Fdw.Tds.Endpoints.Sql.Statements;
using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace Fdw.Tds.Endpoints.Sql.Procedures;

/// <summary>
/// Runs a statement with declared parameters, as sp_executesql and the prepared-statement procedures do.
/// </summary>
/// <remarks>
/// <para>
/// The parameter list (@p0 int, @p1 nvarchar(50) OUTPUT) is read with SQL Server's own grammar, by parsing it
/// as a procedure's parameters. Each parameter takes the argument passed by its name, or else its default;
/// one with neither is error 8178, as SQL Server reports it. Every value is converted to its declared type.
/// </para>
/// <para>
/// The statement runs on its own variables — the parameters, and nothing of the caller's — and its statements
/// end in DONEINPROC. An OUTPUT parameter the caller passed as OUTPUT hands its final value back.
/// </para>
/// </remarks>
public static class ParameterizedBatch
{
    /// <summary>Runs a parameterized statement.</summary>
    /// <param name="call">The call that supplies the arguments.</param>
    /// <param name="declarations">The parameter list, empty for none.</param>
    /// <param name="statement">The statement text.</param>
    /// <param name="firstValue">The position of the first argument that is a parameter's value.</param>
    /// <param name="cancellationToken">A token the client's attention signal cancels.</param>
    /// <returns>The OUTPUT values, or the refusal.</returns>
    public static async Task<IGenericResult<SqlProcedureResult>> Run(
        SqlProcedureCall call, string declarations, string statement, int firstValue, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(call);
        ArgumentNullException.ThrowIfNull(declarations);
        var parameters = Parameters(declarations, call);
        if (parameters.IsFailure)
            return parameters.ToNewResult<SqlProcedureResult>();

        var variables = new SqlVariables();
        var nested = call.Context.Nested(variables, inProcedure: true, call.Line);
        if (nested.IsFailure)
            return nested.ToNewResult<SqlProcedureResult>();

        var bound = Bind(parameters.Value!, call, firstValue, nested.Value!, declarations);
        if (bound.IsFailure)
            return bound.ToNewResult<SqlProcedureResult>();

        var ran = await SqlBatchRunner.Run(statement, nested.Value!, cancellationToken).ConfigureAwait(false);
        if (ran.IsFailure)
            return ran.ToNewResult<SqlProcedureResult>();

        return GenericResult<SqlProcedureResult>.Success(new SqlProcedureResult(Outputs(bound.Value!, variables)));
    }

    // The parameter list, read as a procedure's parameter list.
    private static IGenericResult<IList<ProcedureParameter>> Parameters(string declarations, SqlProcedureCall call)
    {
        if (string.IsNullOrWhiteSpace(declarations))
            return GenericResult<IList<ProcedureParameter>>.Success([]);

        using var reader = new StringReader($"CREATE PROCEDURE p {declarations} AS RETURN");
        var parsed = new TSql160Parser(true).Parse(reader, out var errors);
        if (errors.Count > 0 || parsed is not TSqlScript { Batches: [{ Statements: [CreateProcedureStatement procedure] }] })
            return SqlRefusal.Refuse<IList<ProcedureParameter>>(SqlServerErrors.Syntax($"Incorrect syntax in the parameter list '{declarations}'.", call.Line), call.Context.Logger);

        return GenericResult<IList<ProcedureParameter>>.Success(procedure.Parameters);
    }

    private static IGenericResult<IReadOnlyList<(ProcedureParameter Parameter, SqlProcedureArgument? Argument, SqlConversionTarget Type)>> Bind(
        IList<ProcedureParameter> parameters, SqlProcedureCall call, int firstValue, SqlExecutionContext nested, string declarations)
    {
        var bound = new List<(ProcedureParameter, SqlProcedureArgument?, SqlConversionTarget)>();
        for (var i = 0; i < parameters.Count; i++)
        {
            var parameter = parameters[i];
            var type = SqlConversions.Resolve(parameter.DataType, inCast: false, call.Line, call.Context.Logger);
            if (type.IsFailure)
                return type.ToNewResult<IReadOnlyList<(ProcedureParameter, SqlProcedureArgument?, SqlConversionTarget)>>();

            var name = parameter.VariableName.Value;
            call.TryArgument(name, firstValue + i, out var argument);
            var value = Value(parameter, argument, type.Value!, call, nested, declarations);
            if (value.IsFailure)
                return value.ToNewResult<IReadOnlyList<(ProcedureParameter, SqlProcedureArgument?, SqlConversionTarget)>>();

            nested.Variables.Declare(name, type.Value!, value.Value);
            bound.Add((parameter, argument, type.Value!));
        }

        return GenericResult<IReadOnlyList<(ProcedureParameter, SqlProcedureArgument?, SqlConversionTarget)>>.Success(bound);
    }

    private static IGenericResult<object?> Value(
        ProcedureParameter parameter, SqlProcedureArgument? argument, SqlConversionTarget type, SqlProcedureCall call, SqlExecutionContext nested, string declarations)
    {
        IGenericResult<object?> raw;
        if (argument is not null)
            raw = GenericResult<object?>.Success(argument.Value);
        else if (parameter.Value is not null)
            raw = SqlExpressions.Evaluate(parameter.Value, SqlScope.Of(nested));
        else
            return SqlRefusal.Refuse<object?>(new SqlServerMessage(8178, 1, 16,
                $"The parameterized query '({declarations})' expects the parameter '{parameter.VariableName.Value}', which was not supplied.", call.Line),
                call.Context.Logger);

        if (raw.IsFailure)
            return raw;

        return type.TryConvert(raw.Value, out var converted)
            ? GenericResult<object?>.Success(converted)
            : SqlRefusal.Refuse<object?>(SqlServerErrors.ConversionFailed(type.ToString(), call.Line), call.Context.Logger);
    }

    private static List<SqlProcedureOutput> Outputs(
        IReadOnlyList<(ProcedureParameter Parameter, SqlProcedureArgument? Argument, SqlConversionTarget Type)> bound, SqlVariables variables)
        => bound
            .Where(b => b.Argument is { IsOutput: true } && b.Parameter.Modifier == ParameterModifier.Output)
            .Select(b =>
            {
                variables.TryGet(b.Parameter.VariableName.Value, out var value);
                return new SqlProcedureOutput(b.Argument!.Ordinal, b.Argument.Name, value, b.Type.ColumnType());
            })
            .ToList();
}
