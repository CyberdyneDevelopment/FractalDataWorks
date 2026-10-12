using System;
using Fdw.Results;
using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace Fdw.Tds.Endpoints.Sql.Expressions;

/// <summary>
/// CASE, COALESCE, NULLIF and IIF: the expressions that choose a value.
/// </summary>
/// <remarks>Only the branch chosen is evaluated, so a branch that would fail is never reached when it is not taken.</remarks>
public static class SqlConditionals
{
    /// <summary>Evaluates CASE WHEN condition THEN value ... ELSE value END.</summary>
    /// <param name="expression">The expression as parsed.</param>
    /// <param name="scope">What it is evaluated against.</param>
    /// <returns>The chosen value, or the refusal.</returns>
    public static IGenericResult<object?> Searched(SearchedCaseExpression expression, SqlScope scope)
    {
        ArgumentNullException.ThrowIfNull(expression);
        foreach (var when in expression.WhenClauses)
        {
            var holds = SqlPredicates.Evaluate(when.WhenExpression, scope);
            if (holds.IsFailure)
                return holds.ToNewResult<object?>();

            if (holds.Value == true)
                return SqlExpressions.Evaluate(when.ThenExpression, scope);
        }

        return Else(expression.ElseExpression, scope);
    }

    /// <summary>Evaluates CASE input WHEN value THEN value ... ELSE value END.</summary>
    /// <param name="expression">The expression as parsed.</param>
    /// <param name="scope">What it is evaluated against.</param>
    /// <returns>The chosen value, or the refusal.</returns>
    public static IGenericResult<object?> Simple(SimpleCaseExpression expression, SqlScope scope)
    {
        ArgumentNullException.ThrowIfNull(expression);
        var input = SqlExpressions.Evaluate(expression.InputExpression, scope);
        if (input.IsFailure)
            return input;

        foreach (var when in expression.WhenClauses)
        {
            var candidate = SqlExpressions.Evaluate(when.WhenExpression, scope);
            if (candidate.IsFailure)
                return candidate;

            if (Same(input.Value, candidate.Value))
                return SqlExpressions.Evaluate(when.ThenExpression, scope);
        }

        return Else(expression.ElseExpression, scope);
    }

    /// <summary>Evaluates COALESCE: the first argument that is not NULL.</summary>
    /// <param name="expression">The expression as parsed.</param>
    /// <param name="scope">What it is evaluated against.</param>
    /// <returns>The value, NULL when every argument is, or the refusal.</returns>
    public static IGenericResult<object?> Coalesce(CoalesceExpression expression, SqlScope scope)
    {
        ArgumentNullException.ThrowIfNull(expression);
        foreach (var candidate in expression.Expressions)
        {
            var value = SqlExpressions.Evaluate(candidate, scope);
            if (value.IsFailure || value.Value is not null)
                return value;
        }

        return GenericResult<object?>.Success(null);
    }

    /// <summary>Evaluates NULLIF: NULL when its arguments are equal, else the first.</summary>
    /// <param name="expression">The expression as parsed.</param>
    /// <param name="scope">What it is evaluated against.</param>
    /// <returns>The value, or the refusal.</returns>
    public static IGenericResult<object?> NullIf(NullIfExpression expression, SqlScope scope)
    {
        ArgumentNullException.ThrowIfNull(expression);
        var first = SqlExpressions.Evaluate(expression.FirstExpression, scope);
        if (first.IsFailure)
            return first;

        var second = SqlExpressions.Evaluate(expression.SecondExpression, scope);
        if (second.IsFailure)
            return second;

        return Same(first.Value, second.Value) ? GenericResult<object?>.Success(null) : first;
    }

    /// <summary>Evaluates IIF(condition, then, else).</summary>
    /// <param name="expression">The expression as parsed.</param>
    /// <param name="scope">What it is evaluated against.</param>
    /// <returns>The chosen value, or the refusal.</returns>
    public static IGenericResult<object?> IIf(IIfCall expression, SqlScope scope)
    {
        ArgumentNullException.ThrowIfNull(expression);
        var holds = SqlPredicates.Evaluate(expression.Predicate, scope);
        if (holds.IsFailure)
            return holds.ToNewResult<object?>();

        return SqlExpressions.Evaluate(holds.Value == true ? expression.ThenExpression : expression.ElseExpression, scope);
    }

    private static IGenericResult<object?> Else(ScalarExpression? otherwise, SqlScope scope)
        => otherwise is null ? GenericResult<object?>.Success(null) : SqlExpressions.Evaluate(otherwise, scope);

    // Equal, and neither NULL: a NULL equals nothing, not even another NULL.
    private static bool Same(object? left, object? right)
        => left is not null && right is not null && SqlValues.TryCompare(left, right, out var order) && order == 0;
}
