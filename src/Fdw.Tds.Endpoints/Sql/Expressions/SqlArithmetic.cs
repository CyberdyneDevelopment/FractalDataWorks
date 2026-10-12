using System;
using System.Collections.Generic;
using Fdw.Protocols.Tds;
using Fdw.Results;
using Fdw.Tds.Endpoints.Logging;
using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace Fdw.Tds.Endpoints.Sql.Expressions;

/// <summary>
/// The arithmetic and string operators, with SQL Server's rules for the type of their result.
/// </summary>
/// <remarks>
/// <para>
/// NULL in, NULL out. Two integers give an int, or a bigint when either is one, and overflowing it is
/// error 8115 rather than a wrapped number. A decimal on either side gives a decimal; a float gives a float.
/// Text added to text is joined; text against a number is converted to the number first, as SQL Server's
/// type precedence says, and text that is not a number is error 245.
/// </para>
/// </remarks>
public static class SqlArithmetic
{
    // Each operator answers its result, or null for a division by zero. Overflow throws OverflowException, which
    // Apply turns into error 8115: checked arithmetic is how .NET says a result does not fit its type.
    private static readonly Dictionary<BinaryExpressionType, Func<long, long, long?>> Integers = new()
    {
        [BinaryExpressionType.Add] = static (a, b) => checked(a + b),
        [BinaryExpressionType.Subtract] = static (a, b) => checked(a - b),
        [BinaryExpressionType.Multiply] = static (a, b) => checked(a * b),
        [BinaryExpressionType.Divide] = static (a, b) => b == 0 ? null : checked(a / b),
        [BinaryExpressionType.Modulo] = static (a, b) => b == 0 ? null : checked(a % b),
        [BinaryExpressionType.BitwiseAnd] = static (a, b) => a & b,
        [BinaryExpressionType.BitwiseOr] = static (a, b) => a | b,
        [BinaryExpressionType.BitwiseXor] = static (a, b) => a ^ b,
    };

    private static readonly Dictionary<BinaryExpressionType, Func<decimal, decimal, decimal?>> Decimals = new()
    {
        [BinaryExpressionType.Add] = static (a, b) => a + b,
        [BinaryExpressionType.Subtract] = static (a, b) => a - b,
        [BinaryExpressionType.Multiply] = static (a, b) => a * b,
        [BinaryExpressionType.Divide] = static (a, b) => b == 0m ? null : a / b,
        [BinaryExpressionType.Modulo] = static (a, b) => b == 0m ? null : a % b,
    };

    private static readonly Dictionary<BinaryExpressionType, Func<double, double, double?>> Doubles = new()
    {
        [BinaryExpressionType.Add] = static (a, b) => a + b,
        [BinaryExpressionType.Subtract] = static (a, b) => a - b,
        [BinaryExpressionType.Multiply] = static (a, b) => a * b,
        [BinaryExpressionType.Divide] = static (a, b) => b == 0d ? null : a / b,
        [BinaryExpressionType.Modulo] = static (a, b) => b == 0d ? null : a % b,
    };

    /// <summary>Evaluates a binary operator.</summary>
    /// <param name="expression">The expression as parsed.</param>
    /// <param name="scope">What it is evaluated against.</param>
    /// <returns>The value, or the refusal.</returns>
    public static IGenericResult<object?> Binary(BinaryExpression expression, SqlScope scope)
    {
        ArgumentNullException.ThrowIfNull(expression);
        var left = SqlExpressions.Evaluate(expression.FirstExpression, scope);
        if (left.IsFailure)
            return left;

        var right = SqlExpressions.Evaluate(expression.SecondExpression, scope);
        if (right.IsFailure)
            return right;

        if (left.Value is null || right.Value is null)
            return GenericResult<object?>.Success(null);

        var op = expression.BinaryExpressionType;
        if (left.Value is string a && right.Value is string b && op is BinaryExpressionType.Add or BinaryExpressionType.Concat)
            return GenericResult<object?>.Success(a + b);

        return Numbers(op, left.Value, right.Value, expression, scope);
    }

    /// <summary>Evaluates a unary operator.</summary>
    /// <param name="expression">The expression as parsed.</param>
    /// <param name="scope">What it is evaluated against.</param>
    /// <returns>The value, or the refusal.</returns>
    public static IGenericResult<object?> Unary(UnaryExpression expression, SqlScope scope)
    {
        ArgumentNullException.ThrowIfNull(expression);
        var operand = SqlExpressions.Evaluate(expression.Expression, scope);
        if (operand.IsFailure || operand.Value is null)
            return operand;

        var op = expression.UnaryExpressionType;
        if (op == UnaryExpressionType.Positive)
            return operand;

        if (op == UnaryExpressionType.BitwiseNot)
            return BitwiseNot(operand.Value, expression, scope);

        return Numbers(BinaryExpressionType.Subtract, 0, operand.Value, expression, scope);
    }

    private static IGenericResult<object?> Numbers(BinaryExpressionType op, object left, object right, ScalarExpression expression, SqlScope scope)
    {
        if (left is double or float || right is double or float)
            return Floating(op, left, right, expression, scope);

        if (IsWhole(left) && IsWhole(right))
            return Whole(op, left, right, expression, scope);

        return Exact(op, left, right, expression, scope);
    }

    private static IGenericResult<object?> Floating(BinaryExpressionType op, object left, object right, ScalarExpression expression, SqlScope scope)
        => TryDouble(left, out var x) && TryDouble(right, out var y)
            ? Apply(Doubles, op, x, y, expression, scope)
            : NotANumber(expression, scope);

    // Two integers give an int unless either is a bigint, as SQL Server's precedence says.
    private static IGenericResult<object?> Whole(BinaryExpressionType op, object left, object right, ScalarExpression expression, SqlScope scope)
    {
        var whole = Apply(Integers, op,
            Convert.ToInt64(left, System.Globalization.CultureInfo.InvariantCulture),
            Convert.ToInt64(right, System.Globalization.CultureInfo.InvariantCulture), expression, scope);
        if (whole.IsFailure)
            return whole;

        if (left is long || right is long)
            return whole;

        return NarrowToInt((long)whole.Value!, expression, scope);
    }

    private static IGenericResult<object?> Exact(BinaryExpressionType op, object left, object right, ScalarExpression expression, SqlScope scope)
        => SqlValues.TryNumber(left, out var x) && SqlValues.TryNumber(right, out var y)
            ? Apply(Decimals, op, x, y, expression, scope)
            : NotANumber(expression, scope);

    private static IGenericResult<object?> NotANumber(ScalarExpression expression, SqlScope scope)
        => SqlRefusal.Refuse<object?>(SqlServerErrors.ConversionFailed("numeric", expression.StartLine), scope.Context.Logger);

    private static IGenericResult<object?> Apply<T>(Dictionary<BinaryExpressionType, Func<T, T, T?>> table, BinaryExpressionType op, T left, T right, ScalarExpression expression, SqlScope scope)
        where T : struct
    {
        if (!table.TryGetValue(op, out var apply))
            return Incompatible(expression, scope);

        T? result;
        try
        {
            result = apply(left, right);
        }
        catch (OverflowException ex)
        {
            TdsEndpointLog.ArithmeticOverflowed(scope.Context.Logger, ex, expression.StartLine);
            return SqlRefusal.Refuse<object?>(new SqlServerMessage(8115, 2, 16,
                "Arithmetic overflow error converting expression to data type numeric.", expression.StartLine), scope.Context.Logger);
        }

        return result is { } value
            ? GenericResult<object?>.Success(value)
            : SqlRefusal.Refuse<object?>(SqlServerErrors.DivideByZero(expression.StartLine), scope.Context.Logger);
    }

    private static IGenericResult<object?> NarrowToInt(long value, ScalarExpression expression, SqlScope scope)
    {
        return value is >= int.MinValue and <= int.MaxValue
            ? GenericResult<object?>.Success((int)value)
            : SqlRefusal.Refuse<object?>(new SqlServerMessage(8115, 2, 16,
                "Arithmetic overflow error converting expression to data type int.", expression.StartLine), scope.Context.Logger);
    }

    private static IGenericResult<object?> Incompatible(ScalarExpression expression, SqlScope scope)
        => SqlRefusal.Refuse<object?>(SqlServerErrors.NotSupported(SqlText.Of(expression), expression.StartLine), scope.Context.Logger);

    private static bool IsWhole(object value) => value is int or long or short or byte or bool
        || (value is string text && long.TryParse(text.Trim(), System.Globalization.NumberStyles.Integer, System.Globalization.CultureInfo.InvariantCulture, out _));

    private static bool TryDouble(object value, out double number)
    {
        if (value is double or float)
        {
            number = Convert.ToDouble(value, System.Globalization.CultureInfo.InvariantCulture);
            return true;
        }

        var exact = SqlValues.TryNumber(value, out var parsed);
        number = exact ? (double)parsed : 0d;
        return exact;
    }

    // ~ on a bigint gives a bigint; on any smaller integer it gives an int.
    private static IGenericResult<object?> BitwiseNot(object operand, ScalarExpression expression, SqlScope scope) => operand switch
    {
        long whole => GenericResult<object?>.Success(~whole),
        int whole => GenericResult<object?>.Success(~whole),
        short whole => GenericResult<object?>.Success(~whole),
        byte whole => GenericResult<object?>.Success(~whole),
        _ => Incompatible(expression, scope),
    };

}
