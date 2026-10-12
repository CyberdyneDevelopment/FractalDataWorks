using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using Fdw.Results;
using Fdw.Tds.Endpoints.Sql.Expressions;
using Fdw.Web.RestEndpoints.Query;
using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace Fdw.Tds.Endpoints.Sql.Queries;

/// <summary>
/// Translates a WHERE clause into the filter tree a data set's store runs, so rows are filtered where they
/// live and row-level security narrows them as the caller.
/// </summary>
/// <remarks>
/// <para>
/// What translates: AND and OR; a column compared with a value; IS [NOT] NULL; IN with a list of values;
/// LIKE with % only at the start, the end or both; BETWEEN and NOT BETWEEN. A value is anything that reads no
/// column — a literal, a variable, a parameter, GETDATE() — evaluated before the query runs.
/// </para>
/// <para>
/// Anything else is refused with error 40517 naming the condition, never filtered after the rows are read:
/// a condition that cannot reach the store would otherwise read every row of the table to answer a query
/// that looks selective.
/// </para>
/// </remarks>
internal static class PushdownFilter
{
    private static readonly Dictionary<string, (string Operator, string Flipped)> Comparisons = new(StringComparer.Ordinal)
    {
        ["Equals"] = ("Equal", "Equal"),
        ["NotEqualToBrackets"] = ("NotEqual", "NotEqual"),
        ["NotEqualToExclamation"] = ("NotEqual", "NotEqual"),
        ["GreaterThan"] = ("GreaterThan", "LessThan"),
        ["LessThan"] = ("LessThan", "GreaterThan"),
        ["GreaterThanOrEqualTo"] = ("GreaterThanOrEqual", "LessThanOrEqual"),
        ["LessThanOrEqualTo"] = ("LessThanOrEqual", "GreaterThanOrEqual"),
        ["NotLessThan"] = ("GreaterThanOrEqual", "LessThanOrEqual"),
        ["NotGreaterThan"] = ("LessThanOrEqual", "GreaterThanOrEqual"),
    };

    /// <summary>Translates a search condition.</summary>
    /// <param name="condition">The condition as parsed.</param>
    /// <param name="table">The table it filters.</param>
    /// <param name="qualifier">The alias or name the query qualifies the table's columns by.</param>
    /// <param name="scope">The scope its values are evaluated in.</param>
    /// <returns>The filter tree, or the refusal.</returns>
    public static IGenericResult<QueryFilterNode> Translate(BooleanExpression condition, SqlTable table, string qualifier, SqlScope scope)
        => condition switch
        {
            BooleanParenthesisExpression parenthesis => Translate(parenthesis.Expression, table, qualifier, scope),
            BooleanBinaryExpression logical => Logical(logical, table, qualifier, scope),
            BooleanComparisonExpression comparison => Comparison(comparison, table, qualifier, scope),
            BooleanIsNullExpression isNull => IsNull(isNull, table, qualifier, scope),
            InPredicate list => In(list, table, qualifier, scope),
            LikePredicate like => Like(like, table, qualifier, scope),
            BooleanTernaryExpression between => Between(between, table, qualifier, scope),
            _ => Refused(condition, scope),
        };

    private static IGenericResult<QueryFilterNode> Logical(BooleanBinaryExpression logical, SqlTable table, string qualifier, SqlScope scope)
    {
        var left = Translate(logical.FirstExpression, table, qualifier, scope);
        if (left.IsFailure)
            return left;

        var right = Translate(logical.SecondExpression, table, qualifier, scope);
        if (right.IsFailure)
            return right;

        return Ok(new QueryFilterNode { Logic = logical.BinaryExpressionType.ToString(), Nodes = [left.Value, right.Value] });
    }

    private static IGenericResult<QueryFilterNode> Comparison(BooleanComparisonExpression comparison, SqlTable table, string qualifier, SqlScope scope)
    {
        if (!Comparisons.TryGetValue(comparison.ComparisonType.ToString(), out var ops))
            return Refused(comparison, scope);

        var columnFirst = comparison.FirstExpression is ColumnReferenceExpression;
        var (columnSide, valueSide) = columnFirst
            ? (comparison.FirstExpression, comparison.SecondExpression)
            : (comparison.SecondExpression, comparison.FirstExpression);

        var column = Column(columnSide, table, qualifier, comparison, scope);
        if (column.IsFailure)
            return column.ToNewResult<QueryFilterNode>();

        var value = Value(valueSide, comparison, scope);
        if (value.IsFailure)
            return value.ToNewResult<QueryFilterNode>();

        return Ok(new QueryFilterNode { Field = column.Value, Operator = columnFirst ? ops.Operator : ops.Flipped, Value = value.Value });
    }

    private static IGenericResult<QueryFilterNode> IsNull(BooleanIsNullExpression isNull, SqlTable table, string qualifier, SqlScope scope)
    {
        var column = Column(isNull.Expression, table, qualifier, isNull, scope);
        return column.IsFailure
            ? column.ToNewResult<QueryFilterNode>()
            : Ok(new QueryFilterNode { Field = column.Value, Operator = isNull.IsNot ? "IsNotNull" : "IsNull" });
    }

    private static IGenericResult<QueryFilterNode> In(InPredicate list, SqlTable table, string qualifier, SqlScope scope)
    {
        if (list.Subquery is not null || list.NotDefined)
            return Refused(list, scope);

        var column = Column(list.Expression, table, qualifier, list, scope);
        if (column.IsFailure)
            return column.ToNewResult<QueryFilterNode>();

        var values = new List<object?>(list.Values.Count);
        foreach (var expression in list.Values)
        {
            var value = Constant(expression, list, scope);
            if (value.IsFailure)
                return value.ToNewResult<QueryFilterNode>();

            values.Add(value.Value);
        }

        return Ok(new QueryFilterNode { Field = column.Value, Operator = "In", Value = JsonSerializer.SerializeToElement(values) });
    }

    // LIKE 'x%' starts with, '%x' ends with, '%x%' contains, 'x' equals; any other pattern stays refused.
    private static IGenericResult<QueryFilterNode> Like(LikePredicate like, SqlTable table, string qualifier, SqlScope scope)
    {
        if (like.NotDefined || like.EscapeExpression is not null)
            return Refused(like, scope);

        var column = Column(like.FirstExpression, table, qualifier, like, scope);
        if (column.IsFailure)
            return column.ToNewResult<QueryFilterNode>();

        var pattern = Constant(like.SecondExpression, like, scope);
        if (pattern.IsFailure)
            return pattern.ToNewResult<QueryFilterNode>();

        return pattern.Value is string text && LikePatterns.TryRead(text, out var op, out var literal)
            ? Ok(new QueryFilterNode { Field = column.Value, Operator = op, Value = JsonSerializer.SerializeToElement(literal) })
            : Refused(like, scope);
    }

    private static IGenericResult<QueryFilterNode> Between(BooleanTernaryExpression between, SqlTable table, string qualifier, SqlScope scope)
    {
        var column = Column(between.FirstExpression, table, qualifier, between, scope);
        var low = Value(between.SecondExpression, between, scope);
        var high = Value(between.ThirdExpression, between, scope);
        if (column.IsFailure || low.IsFailure || high.IsFailure)
            return new IGenericResult[] { column, low, high }.First(r => r.IsFailure).ToNewResult<QueryFilterNode>();

        var inside = between.TernaryExpressionType != BooleanTernaryExpressionType.NotBetween;
        return Ok(new QueryFilterNode
        {
            Logic = inside ? "And" : "Or",
            Nodes =
            [
                new QueryFilterNode { Field = column.Value, Operator = inside ? "GreaterThanOrEqual" : "LessThan", Value = low.Value },
                new QueryFilterNode { Field = column.Value, Operator = inside ? "LessThanOrEqual" : "GreaterThan", Value = high.Value },
            ],
        });
    }

    // The table column a reference names, by its own name, under the table's qualifier if one is written.
    private static IGenericResult<string> Column(ScalarExpression expression, SqlTable table, string qualifier, TSqlFragment condition, SqlScope scope)
    {
        if (expression is not ColumnReferenceExpression { MultiPartIdentifier: { } parts })
            return Refused(condition, scope).ToNewResult<string>();

        var written = parts.Identifiers.Select(i => i.Value).ToList();
        var qualified = written.Count < 2 || string.Equals(written[^2], qualifier, StringComparison.OrdinalIgnoreCase);
        var column = table.Columns.FirstOrDefault(c => string.Equals(c.Name, written[^1], StringComparison.OrdinalIgnoreCase));
        if (!qualified || column is null)
            return SqlRefusal.Refuse<string>(SqlServerErrors.InvalidColumnName(string.Join('.', written), expression.StartLine), scope.Context.Logger);

        return column.IsQueryable ? GenericResult<string>.Success(column.Name) : Refused(condition, scope).ToNewResult<string>();
    }

    // A comparand: evaluated now, before the query runs. A NULL never compares true, so it is refused with the reason.
    private static IGenericResult<JsonElement?> Value(ScalarExpression expression, TSqlFragment condition, SqlScope scope)
    {
        var value = Constant(expression, condition, scope);
        if (value.IsFailure)
            return value.ToNewResult<JsonElement?>();

        if (value.Value is null)
        {
            return SqlRefusal.Refuse<JsonElement?>(SqlServerErrors.NotSupported(
                $"{SqlText.Of(condition)}: a comparison with NULL is never true; use IS NULL", condition.StartLine), scope.Context.Logger);
        }

        return value.Value is byte[]
            ? Refused(condition, scope).ToNewResult<JsonElement?>()
            : GenericResult<JsonElement?>.Success(JsonSerializer.SerializeToElement(value.Value));
    }

    private static IGenericResult<object?> Constant(ScalarExpression expression, TSqlFragment condition, SqlScope scope)
        => FragmentSearch.ReadsColumns(expression)
            ? Refused(condition, scope).ToNewResult<object?>()
            : SqlExpressions.Evaluate(expression, scope);

    private static IGenericResult<QueryFilterNode> Ok(QueryFilterNode node) => GenericResult<QueryFilterNode>.Success(node);

    private static IGenericResult<QueryFilterNode> Refused(TSqlFragment condition, SqlScope scope)
        => SqlRefusal.Refuse<QueryFilterNode>(
            SqlServerErrors.NotSupported($"{SqlText.Of(condition)} (a condition the data set's store cannot run)", condition.StartLine),
            scope.Context.Logger);
}
