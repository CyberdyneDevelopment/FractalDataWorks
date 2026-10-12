using System;
using System.Collections.Generic;
using System.Linq;
using Fdw.Results;
using Fdw.Tds.Endpoints.Sql.Queries;
using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace Fdw.Tds.Endpoints.Sql.Expressions;

/// <summary>
/// Evaluates a T-SQL search condition with SQL's three-valued logic: true, false, or unknown (null).
/// </summary>
/// <remarks>
/// A comparison with NULL is unknown, not false; NOT unknown is unknown; a WHERE keeps only true. Each kind
/// of condition is one table entry, as each kind of expression is.
/// </remarks>
public static class SqlPredicates
{
    private static readonly Dictionary<Type, Func<BooleanExpression, SqlScope, IGenericResult<bool?>>> Evaluators = new()
    {
        [typeof(BooleanComparisonExpression)] = static (e, s) => Comparison((BooleanComparisonExpression)e, s),
        [typeof(BooleanBinaryExpression)] = static (e, s) => Logical((BooleanBinaryExpression)e, s),
        [typeof(BooleanNotExpression)] = static (e, s) => Not(((BooleanNotExpression)e).Expression, s),
        [typeof(BooleanParenthesisExpression)] = static (e, s) => Evaluate(((BooleanParenthesisExpression)e).Expression, s),
        [typeof(BooleanIsNullExpression)] = static (e, s) => IsNull((BooleanIsNullExpression)e, s),
        [typeof(InPredicate)] = static (e, s) => In((InPredicate)e, s),
        [typeof(LikePredicate)] = static (e, s) => Like((LikePredicate)e, s),
        [typeof(BooleanTernaryExpression)] = static (e, s) => Between((BooleanTernaryExpression)e, s),
        [typeof(ExistsPredicate)] = static (e, s) => Exists((ExistsPredicate)e, s),
    };

    private static readonly Dictionary<BooleanComparisonType, Func<int, bool>> Comparisons = new()
    {
        [BooleanComparisonType.Equals] = static o => o == 0,
        [BooleanComparisonType.NotEqualToBrackets] = static o => o != 0,
        [BooleanComparisonType.NotEqualToExclamation] = static o => o != 0,
        [BooleanComparisonType.GreaterThan] = static o => o > 0,
        [BooleanComparisonType.LessThan] = static o => o < 0,
        [BooleanComparisonType.GreaterThanOrEqualTo] = static o => o >= 0,
        [BooleanComparisonType.LessThanOrEqualTo] = static o => o <= 0,
        [BooleanComparisonType.NotLessThan] = static o => o >= 0,
        [BooleanComparisonType.NotGreaterThan] = static o => o <= 0,
    };

    /// <summary>Evaluates a search condition.</summary>
    /// <param name="condition">The condition as parsed.</param>
    /// <param name="scope">What it is evaluated against.</param>
    /// <returns>True, false or null (unknown), or the refusal.</returns>
    public static IGenericResult<bool?> Evaluate(BooleanExpression condition, SqlScope scope)
    {
        ArgumentNullException.ThrowIfNull(condition);
        ArgumentNullException.ThrowIfNull(scope);
        return Evaluators.TryGetValue(condition.GetType(), out var evaluate)
            ? evaluate(condition, scope)
            : SqlRefusal.Refuse<bool?>(SqlServerErrors.NotSupported(SqlText.Of(condition), condition.StartLine), scope.Context.Logger);
    }

    /// <summary>Compares two values with a T-SQL comparison operator.</summary>
    /// <param name="comparisonType">The operator.</param>
    /// <param name="left">The left value.</param>
    /// <param name="right">The right value.</param>
    /// <param name="holds">The outcome: null when either value is NULL.</param>
    /// <returns>False when the values do not compare: one does not convert to the other's type.</returns>
    public static bool TryCompare(BooleanComparisonType comparisonType, object? left, object? right, out bool? holds)
    {
        holds = null;
        if (left is null || right is null)
            return true;

        if (!Comparisons.TryGetValue(comparisonType, out var test) || !SqlValues.TryCompare(left, right, out var order))
            return false;

        holds = test(order);
        return true;
    }

    private static IGenericResult<bool?> Ok(bool? value) => GenericResult<bool?>.Success(value);

    private static IGenericResult<bool?> Comparison(BooleanComparisonExpression comparison, SqlScope scope)
    {
        var left = SqlExpressions.Evaluate(comparison.FirstExpression, scope);
        if (left.IsFailure)
            return left.ToNewResult<bool?>();

        var right = SqlExpressions.Evaluate(comparison.SecondExpression, scope);
        if (right.IsFailure)
            return right.ToNewResult<bool?>();

        var type = comparison.ComparisonType;
        if (type is BooleanComparisonType.IsDistinctFrom or BooleanComparisonType.IsNotDistinctFrom)
            return Distinct(type, left.Value, right.Value, comparison, scope);

        return TryCompare(type, left.Value, right.Value, out var holds)
            ? Ok(holds)
            : SqlRefusal.Refuse<bool?>(SqlServerErrors.ConversionFailed("the type compared with", comparison.StartLine), scope.Context.Logger);
    }

    // IS [NOT] DISTINCT FROM treats two NULLs as the same and is never unknown.
    private static IGenericResult<bool?> Distinct(BooleanComparisonType type, object? left, object? right, BooleanComparisonExpression comparison, SqlScope scope)
    {
        var same = left is null && right is null;
        if (left is not null && right is not null)
        {
            if (!SqlValues.TryCompare(left, right, out var order))
                return SqlRefusal.Refuse<bool?>(SqlServerErrors.ConversionFailed("the type compared with", comparison.StartLine), scope.Context.Logger);

            same = order == 0;
        }

        return Ok(type == BooleanComparisonType.IsNotDistinctFrom ? same : !same);
    }

    private static IGenericResult<bool?> Logical(BooleanBinaryExpression expression, SqlScope scope)
    {
        var left = Evaluate(expression.FirstExpression, scope);
        if (left.IsFailure)
            return left;

        var isAnd = expression.BinaryExpressionType == BooleanBinaryExpressionType.And;
        if (left.Value == !isAnd)
            return left;

        var right = Evaluate(expression.SecondExpression, scope);
        if (right.IsFailure)
            return right;

        return Ok(isAnd ? And(left.Value, right.Value) : Or(left.Value, right.Value));
    }

    private static bool? And(bool? left, bool? right)
        => left == false || right == false ? false : left == true && right == true ? true : null;

    private static bool? Or(bool? left, bool? right)
        => left == true || right == true ? true : left == false && right == false ? false : null;

    private static IGenericResult<bool?> Not(BooleanExpression operand, SqlScope scope)
    {
        var value = Evaluate(operand, scope);
        return value.IsFailure || value.Value is null ? value : Ok(!value.Value);
    }

    private static IGenericResult<bool?> IsNull(BooleanIsNullExpression expression, SqlScope scope)
    {
        var value = SqlExpressions.Evaluate(expression.Expression, scope);
        return value.IsFailure ? value.ToNewResult<bool?>() : Ok((value.Value is null) != expression.IsNot);
    }

    private static IGenericResult<bool?> In(InPredicate predicate, SqlScope scope)
    {
        var value = SqlExpressions.Evaluate(predicate.Expression, scope);
        if (value.IsFailure)
            return value.ToNewResult<bool?>();

        var candidates = predicate.Subquery is { } subquery
            ? InMemorySelect.Column(subquery.QueryExpression, scope)
            : Values(predicate.Values, scope);
        if (candidates.IsFailure)
            return candidates.ToNewResult<bool?>();

        var found = Member(value.Value, candidates.Value!);
        return Ok(predicate.NotDefined ? Negate(found) : found);
    }

    // x IN (...) is true on a match, unknown when nothing matched but something was NULL, else false.
    private static bool? Member(object? value, IReadOnlyList<object?> candidates)
    {
        if (value is null)
            return null;

        var sawNull = false;
        foreach (var candidate in candidates)
        {
            if (candidate is null)
                sawNull = true;
            else if (SqlValues.TryCompare(value, candidate, out var order) && order == 0)
                return true;
        }

        return sawNull ? null : false;
    }

    private static bool? Negate(bool? value) => value is null ? null : !value;

    private static IGenericResult<IReadOnlyList<object?>> Values(IList<ScalarExpression> expressions, SqlScope scope)
    {
        var values = new List<object?>(expressions.Count);
        foreach (var expression in expressions)
        {
            var value = SqlExpressions.Evaluate(expression, scope);
            if (value.IsFailure)
                return value.ToNewResult<IReadOnlyList<object?>>();

            values.Add(value.Value);
        }

        return GenericResult<IReadOnlyList<object?>>.Success(values);
    }

    private static IGenericResult<bool?> Like(LikePredicate predicate, SqlScope scope)
    {
        var parts = Values([predicate.FirstExpression, predicate.SecondExpression, .. Optional(predicate.EscapeExpression)], scope);
        if (parts.IsFailure)
            return parts.ToNewResult<bool?>();

        var values = parts.Value!;
        if (values[0] is null || values[1] is null)
            return Ok(null);

        char? escape = values.Count > 2 && values[2] is string { Length: 1 } e ? e[0] : null;
        var matched = SqlValues.Like(SqlValues.ToText(values[0]!), SqlValues.ToText(values[1]!), escape);
        return Ok(predicate.NotDefined ? !matched : matched);
    }

    private static ScalarExpression[] Optional(ScalarExpression? expression) => expression is null ? [] : [expression];

    private static IGenericResult<bool?> Between(BooleanTernaryExpression expression, SqlScope scope)
    {
        var parts = Values([expression.FirstExpression, expression.SecondExpression, expression.ThirdExpression], scope);
        if (parts.IsFailure)
            return parts.ToNewResult<bool?>();

        var v = parts.Value!;
        if (!TryCompare(BooleanComparisonType.GreaterThanOrEqualTo, v[0], v[1], out var low) || !TryCompare(BooleanComparisonType.LessThanOrEqualTo, v[0], v[2], out var high))
            return SqlRefusal.Refuse<bool?>(SqlServerErrors.ConversionFailed("the type compared with", expression.StartLine), scope.Context.Logger);

        var inside = And(low, high);
        return Ok(expression.TernaryExpressionType == BooleanTernaryExpressionType.NotBetween ? Negate(inside) : inside);
    }

    private static IGenericResult<bool?> Exists(ExistsPredicate predicate, SqlScope scope)
    {
        var rows = InMemorySelect.Rows(predicate.Subquery.QueryExpression, scope);
        return rows.IsFailure ? rows.ToNewResult<bool?>() : Ok(rows.Value!.Rows.Any());
    }
}
