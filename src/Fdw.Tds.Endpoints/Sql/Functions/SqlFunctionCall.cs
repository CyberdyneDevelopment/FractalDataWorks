using System;
using System.Collections.Generic;
using Fdw.Protocols.Tds;
using Fdw.Results;
using Fdw.Tds.Endpoints.Sql.Expressions;
using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace Fdw.Tds.Endpoints.Sql.Functions;

/// <summary>
/// One call of a scalar function: its arguments as written and the scope they are evaluated in.
/// </summary>
/// <remarks>
/// A function evaluates the arguments it needs, when it needs them, so ISNULL's second argument is not
/// evaluated when its first is not NULL, and a function that reads an argument as a name rather than a
/// value can read it as written.
/// </remarks>
public sealed class SqlFunctionCall
{
    /// <summary>Initializes a new instance of the <see cref="SqlFunctionCall"/> class.</summary>
    /// <param name="name">The function's name as written.</param>
    /// <param name="arguments">The arguments as parsed.</param>
    /// <param name="scope">What they are evaluated against.</param>
    /// <param name="line">The line the call was written on.</param>
    public SqlFunctionCall(string name, IList<ScalarExpression> arguments, SqlScope scope, int line)
    {
        Name = name ?? throw new ArgumentNullException(nameof(name));
        Arguments = arguments ?? throw new ArgumentNullException(nameof(arguments));
        Scope = scope ?? throw new ArgumentNullException(nameof(scope));
        Line = line;
    }

    /// <summary>Gets the function's name as written.</summary>
    public string Name { get; }

    /// <summary>Gets the arguments as parsed.</summary>
    public IList<ScalarExpression> Arguments { get; }

    /// <summary>Gets what they are evaluated against.</summary>
    public SqlScope Scope { get; }

    /// <summary>Gets the line the call was written on.</summary>
    public int Line { get; }

    /// <summary>Gets the request.</summary>
    public SqlExecutionContext Context => Scope.Context;

    /// <summary>Evaluates one argument.</summary>
    /// <param name="index">Its position, from 0.</param>
    /// <returns>The value (null for NULL), or the refusal.</returns>
    public IGenericResult<object?> Value(int index) => SqlExpressions.Evaluate(Arguments[index], Scope);

    /// <summary>Evaluates every argument.</summary>
    /// <returns>The values, or the first refusal.</returns>
    public IGenericResult<IReadOnlyList<object?>> Values()
    {
        var values = new List<object?>(Arguments.Count);
        foreach (var argument in Arguments)
        {
            var value = SqlExpressions.Evaluate(argument, Scope);
            if (value.IsFailure)
                return value.ToNewResult<IReadOnlyList<object?>>();

            values.Add(value.Value);
        }

        return GenericResult<IReadOnlyList<object?>>.Success(values);
    }

    /// <summary>Evaluates one argument as text, as SQL Server converts it.</summary>
    /// <param name="index">Its position, from 0.</param>
    /// <returns>The text, null for NULL, or the refusal.</returns>
    public IGenericResult<string?> Text(int index)
    {
        var value = Value(index);
        if (value.IsFailure)
            return value.ToNewResult<string?>();

        return GenericResult<string?>.Success(value.Value is null ? null : SqlValues.ToText(value.Value));
    }

    /// <summary>Evaluates one argument as a number.</summary>
    /// <param name="index">Its position, from 0.</param>
    /// <returns>The number, null for NULL, or error 245 when it is not a number.</returns>
    public IGenericResult<decimal?> Number(int index)
    {
        var value = Value(index);
        if (value.IsFailure)
            return value.ToNewResult<decimal?>();

        if (value.Value is null)
            return GenericResult<decimal?>.Success(null);

        return SqlValues.TryNumber(value.Value, out var number)
            ? GenericResult<decimal?>.Success(number)
            : SqlRefusal.Refuse<decimal?>(SqlServerErrors.ConversionFailed("numeric", Line), Context.Logger);
    }

    /// <summary>Evaluates one argument as a whole number.</summary>
    /// <param name="index">Its position, from 0.</param>
    /// <returns>The number, null for NULL, or error 245 when it is not a whole number.</returns>
    public IGenericResult<long?> Whole(int index)
    {
        var number = Number(index);
        if (number.IsFailure || number.Value is not { } exact)
            return number.IsFailure ? number.ToNewResult<long?>() : GenericResult<long?>.Success(null);

        return exact == decimal.Truncate(exact) && exact is >= long.MinValue and <= long.MaxValue
            ? GenericResult<long?>.Success((long)exact)
            : SqlRefusal.Refuse<long?>(SqlServerErrors.ConversionFailed("int", Line), Context.Logger);
    }

    /// <summary>Answers a value.</summary>
    /// <param name="value">The value; null for NULL.</param>
    /// <returns>The successful result.</returns>
    public static IGenericResult<object?> Ok(object? value) => GenericResult<object?>.Success(value);

    /// <summary>Refuses the call with a SQL Server error.</summary>
    /// <param name="message">The error.</param>
    /// <returns>The refusal.</returns>
    public IGenericResult<object?> Refuse(SqlServerMessage message) => SqlRefusal.Refuse<object?>(message, Context.Logger);

    /// <summary>Refuses an argument that does not convert to what the function needs.</summary>
    /// <param name="target">What it needed.</param>
    /// <returns>The refusal.</returns>
    public IGenericResult<object?> NotConvertible(string target) => Refuse(SqlServerErrors.ConversionFailed(target, Line));
}
