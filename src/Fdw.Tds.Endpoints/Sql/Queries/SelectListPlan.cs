using System;
using System.Collections.Generic;
using System.Linq;
using Fdw.Protocols.Tds;
using Fdw.Protocols.Tds.Types;
using Fdw.Results;
using Fdw.Tds.Endpoints.Sql.Expressions;
using Fdw.Tds.Endpoints.Sql.Rows;
using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace Fdw.Tds.Endpoints.Sql.Queries;

/// <summary>
/// Turns a select list into the result columns it produces and how each is read from a row.
/// </summary>
internal static class SelectListPlan
{
    /// <summary>Plans a select list against a row source.</summary>
    /// <param name="elements">The select list as parsed; none of them a variable assignment.</param>
    /// <param name="source">The rows' columns.</param>
    /// <param name="scope">The enclosed scope the rows are evaluated in.</param>
    /// <returns>The items, in result order, or the refusal.</returns>
    public static IGenericResult<IReadOnlyList<OutputItem>> Build(IList<SelectElement> elements, IReadOnlyList<SqlBoundColumn> source, SqlScope scope)
    {
        var items = new List<OutputItem>();
        foreach (var element in elements)
        {
            var planned = element is SelectStarExpression star
                ? Star(star, source, scope)
                : Scalar((SelectScalarExpression)element, scope);
            if (planned.IsFailure)
                return planned;

            items.AddRange(planned.Value!);
        }

        return GenericResult<IReadOnlyList<OutputItem>>.Success(items);
    }

    // * or alias.*: every column of the source, or of the one the qualifier names, in source order.
    private static IGenericResult<IReadOnlyList<OutputItem>> Star(SelectStarExpression star, IReadOnlyList<SqlBoundColumn> source, SqlScope scope)
    {
        var qualifier = star.Qualifier?.Identifiers.Last().Value;
        var columns = Enumerable.Range(0, source.Count)
            .Where(i => qualifier is null || string.Equals(source[i].Qualifier, qualifier, StringComparison.OrdinalIgnoreCase))
            .Select(i => OutputItem.FromRow(new SqlOutputColumn(source[i].Name, source[i].Type, source[i].IsNullable), i))
            .ToList();

        if (columns.Count > 0)
            return GenericResult<IReadOnlyList<OutputItem>>.Success(columns);

        var message = qualifier is null
            ? new SqlServerMessage(263, 1, 16, "Must specify table to select from.", star.StartLine)
            : new SqlServerMessage(107, 1, 16, $"The column prefix '{qualifier}' does not match with a table name or alias name used in the query.", star.StartLine);
        return SqlRefusal.Refuse<IReadOnlyList<OutputItem>>(message, scope.Context.Logger);
    }

    private static IGenericResult<IReadOnlyList<OutputItem>> Scalar(SelectScalarExpression scalar, SqlScope scope)
    {
        var expression = scalar.Expression;
        var typing = Typing(expression, scope);
        if (typing.IsFailure)
            return typing.ToNewResult<IReadOnlyList<OutputItem>>();

        var (declared, nullable) = typing.Value;
        var column = new SqlOutputColumn(Name(scalar), declared, nullable);
        return GenericResult<IReadOnlyList<OutputItem>>.Success([OutputItem.FromExpression(column, expression)]);
    }

    // The alias when one is written; a column's own name when the expression is a column; else no name.
    private static string Name(SelectScalarExpression scalar)
    {
        if (scalar.ColumnName is { } alias)
            return alias.Value;

        return scalar.Expression is ColumnReferenceExpression { MultiPartIdentifier: { } parts } ? parts.Identifiers.Last().Value : string.Empty;
    }

    // A column keeps its source's declared type; a CAST or CONVERT declares its target; anything else is
    // typed from its values.
    private static IGenericResult<(TdsColumnType? Declared, bool Nullable)> Typing(ScalarExpression expression, SqlScope scope)
    {
        if (expression is ColumnReferenceExpression { MultiPartIdentifier: { } parts } reference)
        {
            var bound = scope.Bind(parts, reference.StartLine);
            if (bound.IsFailure)
                return bound.ToNewResult<(TdsColumnType?, bool)>();

            var column = scope.Columns[bound.Value];
            return GenericResult<(TdsColumnType?, bool)>.Success((column.Type, column.IsNullable));
        }

        var target = expression switch
        {
            CastCall cast => cast.DataType,
            ConvertCall convert => convert.DataType,
            _ => null,
        };
        if (target is null)
            return GenericResult<(TdsColumnType?, bool)>.Success((null, true));

        var resolved = SqlConversions.Resolve(target, inCast: true, expression.StartLine, scope.Context.Logger);
        return resolved.IsFailure
            ? resolved.ToNewResult<(TdsColumnType?, bool)>()
            : GenericResult<(TdsColumnType?, bool)>.Success((resolved.Value!.ColumnType(), true));
    }
}
