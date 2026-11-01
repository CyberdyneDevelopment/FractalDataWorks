using System;
using System.Buffers;
using System.Collections.Generic;
using System.Globalization;
using Fdw.Results;
using Fdw.Tds.Endpoints.Sql.Functions;
using Fdw.Tds.Endpoints.Sql.Globals;
using Fdw.Tds.Endpoints.Sql.Queries;
using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace Fdw.Tds.Endpoints.Sql.Expressions;

/// <summary>
/// Evaluates a T-SQL scalar expression against a scope.
/// </summary>
/// <remarks>
/// Each kind of expression is one table entry. An expression of a kind the table does not hold is refused
/// with error 40517 naming the text written, so a client learns exactly which part of its statement this
/// server does not run.
/// </remarks>
public static class SqlExpressions
{
    // CURRENT_TIMESTAMP is GETDATE(); the user calls answer the signed-in login, which is its own user here.
    private static readonly Dictionary<ParameterlessCallType, Func<SqlScope, object?>> ParameterlessCalls = new()
    {
        [ParameterlessCallType.CurrentTimestamp] = static s => s.Context.RequestTime.LocalDateTime,
        [ParameterlessCallType.CurrentDate] = static s => DateOnly.FromDateTime(s.Context.RequestTime.LocalDateTime),
        [ParameterlessCallType.User] = static s => s.Context.Session.UserName,
        [ParameterlessCallType.CurrentUser] = static s => s.Context.Session.UserName,
        [ParameterlessCallType.SessionUser] = static s => s.Context.Session.UserName,
        [ParameterlessCallType.SystemUser] = static s => s.Context.Session.UserName,
    };

    private static readonly Dictionary<Type, Func<ScalarExpression, SqlScope, IGenericResult<object?>>> Evaluators = new()
    {
        [typeof(IntegerLiteral)] = static (e, s) => Integer((Literal)e, s),
        [typeof(NumericLiteral)] = static (e, s) => Numeric((Literal)e, s),
        [typeof(MoneyLiteral)] = static (e, s) => Numeric((Literal)e, s),
        [typeof(RealLiteral)] = static (e, s) => Real((Literal)e, s),
        [typeof(StringLiteral)] = static (e, _) => Ok(((Literal)e).Value),
        [typeof(NullLiteral)] = static (_, _) => Ok(null),
        [typeof(BinaryLiteral)] = static (e, s) => Binary((Literal)e, s),
        [typeof(VariableReference)] = static (e, s) => Variable((VariableReference)e, s),
        [typeof(GlobalVariableExpression)] = static (e, s) => SqlGlobalVariables.Evaluate((GlobalVariableExpression)e, s),
        [typeof(ColumnReferenceExpression)] = static (e, s) => s.Column((ColumnReferenceExpression)e),
        [typeof(ParenthesisExpression)] = static (e, s) => Evaluate(((ParenthesisExpression)e).Expression, s),
        [typeof(UnaryExpression)] = static (e, s) => SqlArithmetic.Unary((UnaryExpression)e, s),
        [typeof(BinaryExpression)] = static (e, s) => SqlArithmetic.Binary((BinaryExpression)e, s),
        [typeof(FunctionCall)] = static (e, s) => SqlScalarFunctions.Call((FunctionCall)e, s),
        [typeof(CastCall)] = static (e, s) => Cast(((CastCall)e).Parameter, ((CastCall)e).DataType, s, e.StartLine, tryOnly: false),
        [typeof(ConvertCall)] = static (e, s) => Cast(((ConvertCall)e).Parameter, ((ConvertCall)e).DataType, s, e.StartLine, tryOnly: false),
        [typeof(TryCastCall)] = static (e, s) => Cast(((TryCastCall)e).Parameter, ((TryCastCall)e).DataType, s, e.StartLine, tryOnly: true),
        [typeof(TryConvertCall)] = static (e, s) => Cast(((TryConvertCall)e).Parameter, ((TryConvertCall)e).DataType, s, e.StartLine, tryOnly: true),
        [typeof(ParameterlessCall)] = static (e, s) => Parameterless((ParameterlessCall)e, s),
        [typeof(SearchedCaseExpression)] = static (e, s) => SqlConditionals.Searched((SearchedCaseExpression)e, s),
        [typeof(SimpleCaseExpression)] = static (e, s) => SqlConditionals.Simple((SimpleCaseExpression)e, s),
        [typeof(CoalesceExpression)] = static (e, s) => SqlConditionals.Coalesce((CoalesceExpression)e, s),
        [typeof(NullIfExpression)] = static (e, s) => SqlConditionals.NullIf((NullIfExpression)e, s),
        [typeof(IIfCall)] = static (e, s) => SqlConditionals.IIf((IIfCall)e, s),
        [typeof(ScalarSubquery)] = static (e, s) => InMemorySelect.Scalar((ScalarSubquery)e, s),
    };

    /// <summary>Evaluates an expression.</summary>
    /// <param name="expression">The expression as parsed.</param>
    /// <param name="scope">What it is evaluated against.</param>
    /// <returns>The value (null for NULL), or the refusal.</returns>
    public static IGenericResult<object?> Evaluate(ScalarExpression expression, SqlScope scope)
    {
        ArgumentNullException.ThrowIfNull(expression);
        ArgumentNullException.ThrowIfNull(scope);
        return Evaluators.TryGetValue(expression.GetType(), out var evaluate)
            ? evaluate(expression, scope)
            : SqlRefusal.Refuse<object?>(SqlServerErrors.NotSupported(SqlText.Of(expression), expression.StartLine), scope.Context.Logger);
    }

    /// <summary>Evaluates an expression to a whole number, as TOP, OFFSET and SET ROWCOUNT need.</summary>
    /// <param name="expression">The expression as parsed.</param>
    /// <param name="scope">What it is evaluated against.</param>
    /// <returns>The number, or the refusal.</returns>
    public static IGenericResult<long> Whole(ScalarExpression expression, SqlScope scope)
    {
        var value = Evaluate(expression, scope);
        if (value.IsFailure)
            return value.ToNewResult<long>();

        return value.Value is { } number && SqlValues.TryNumber(number, out var exact) && exact == decimal.Truncate(exact)
            ? GenericResult<long>.Success((long)exact)
            : SqlRefusal.Refuse<long>(SqlServerErrors.ConversionFailed("bigint", expression.StartLine), scope.Context.Logger);
    }

    private static IGenericResult<object?> Ok(object? value) => GenericResult<object?>.Success(value);

    // An integer literal past int's range is, in SQL Server, a numeric.
    private static IGenericResult<object?> Integer(Literal literal, SqlScope scope)
    {
        if (int.TryParse(literal.Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var small))
            return Ok(small);

        return Numeric(literal, scope);
    }

    private static IGenericResult<object?> Numeric(Literal literal, SqlScope scope)
        => decimal.TryParse(literal.Value.TrimStart('$', '£', '€', '¥'), NumberStyles.Float, CultureInfo.InvariantCulture, out var number)
            ? Ok(number)
            : SqlRefusal.Refuse<object?>(SqlServerErrors.ConversionFailed("numeric", literal.StartLine), scope.Context.Logger);

    private static IGenericResult<object?> Real(Literal literal, SqlScope scope)
        => double.TryParse(literal.Value, NumberStyles.Float, CultureInfo.InvariantCulture, out var number)
            ? Ok(number)
            : SqlRefusal.Refuse<object?>(SqlServerErrors.ConversionFailed("float", literal.StartLine), scope.Context.Logger);

    private static IGenericResult<object?> Binary(Literal literal, SqlScope scope)
    {
        var hex = literal.Value.StartsWith("0x", StringComparison.OrdinalIgnoreCase) ? literal.Value[2..] : literal.Value;
        if (hex.Length % 2 != 0)
            hex = "0" + hex;

        var bytes = new byte[hex.Length / 2];
        return Convert.FromHexString(hex, bytes, out _, out _) == OperationStatus.Done
            ? Ok(bytes)
            : SqlRefusal.Refuse<object?>(SqlServerErrors.ConversionFailed("varbinary", literal.StartLine), scope.Context.Logger);
    }

    private static IGenericResult<object?> Variable(VariableReference reference, SqlScope scope)
        => scope.Context.Variables.TryGet(reference.Name, out var value)
            ? Ok(value)
            : SqlRefusal.Refuse<object?>(SqlServerErrors.MustDeclareVariable(reference.Name, reference.StartLine), scope.Context.Logger);

    private static IGenericResult<object?> Cast(ScalarExpression parameter, DataTypeReference type, SqlScope scope, int line, bool tryOnly)
    {
        var value = Evaluate(parameter, scope);
        if (value.IsFailure)
            return value;

        var target = SqlConversions.Resolve(type, inCast: true, line, scope.Context.Logger);
        if (target.IsFailure)
            return target.ToNewResult<object?>();

        if (target.Value!.TryConvert(value.Value, out var converted))
            return Ok(converted);

        return tryOnly
            ? Ok(null)
            : SqlRefusal.Refuse<object?>(SqlServerErrors.ConversionFailed(target.Value.ToString(), line), scope.Context.Logger);
    }

    private static IGenericResult<object?> Parameterless(ParameterlessCall call, SqlScope scope)
        => ParameterlessCalls.TryGetValue(call.ParameterlessCallType, out var read)
            ? Ok(read(scope))
            : SqlRefusal.Refuse<object?>(SqlServerErrors.NotSupported(SqlText.Of(call), call.StartLine), scope.Context.Logger);
}
