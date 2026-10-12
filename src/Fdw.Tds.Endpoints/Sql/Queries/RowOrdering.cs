using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Fdw.Protocols.Tds;
using Fdw.Results;
using Fdw.Tds.Endpoints.Sql.Expressions;
using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace Fdw.Tds.Endpoints.Sql.Queries;

/// <summary>
/// ORDER BY over rows held in memory, as SQL Server orders them.
/// </summary>
/// <remarks>
/// An item that is a whole number orders by that position in the select list; a single name that is a
/// select-list alias orders by that column; anything else is evaluated against the row. NULL sorts first
/// ascending and last descending. The sort is stable, so rows equal on every key keep their order.
/// </remarks>
internal static class RowOrdering
{
    /// <summary>Orders rows.</summary>
    /// <param name="order">The ORDER BY clause as parsed.</param>
    /// <param name="rows">The rows: each source scope with the select-list values it produced.</param>
    /// <param name="items">The select list's items, for names and positions.</param>
    /// <param name="scope">The query's scope, for the refusal.</param>
    /// <returns>The rows in order, or the refusal.</returns>
    public static IGenericResult<List<(SqlScope Source, object?[] Values)>> Sort(
        OrderByClause order, List<(SqlScope Source, object?[] Values)> rows, IReadOnlyList<OutputItem> items, SqlScope scope)
    {
        var keyed = new List<(object?[] Keys, (SqlScope Source, object?[] Values) Row)>(rows.Count);
        foreach (var row in rows)
        {
            var keys = Keys(order.OrderByElements, row, items, scope);
            if (keys.IsFailure)
                return keys.ToNewResult<List<(SqlScope, object?[])>>();

            keyed.Add((keys.Value!, row));
        }

        var descending = order.OrderByElements.Select(e => e.SortOrder == SortOrder.Descending).ToArray();
        var sorted = keyed.OrderBy(k => k.Keys, new KeyComparer(descending)).Select(k => k.Row).ToList();
        return GenericResult<List<(SqlScope, object?[])>>.Success(sorted);
    }

    private static IGenericResult<object?[]> Keys(
        IList<ExpressionWithSortOrder> elements, (SqlScope Source, object?[] Values) row, IReadOnlyList<OutputItem> items, SqlScope scope)
    {
        var keys = new object?[elements.Count];
        for (var i = 0; i < elements.Count; i++)
        {
            var key = Key(elements[i].Expression, row, items, scope);
            if (key.IsFailure)
                return key.ToNewResult<object?[]>();

            keys[i] = key.Value;
        }

        return GenericResult<object?[]>.Success(keys);
    }

    private static IGenericResult<object?> Key(ScalarExpression expression, (SqlScope Source, object?[] Values) row, IReadOnlyList<OutputItem> items, SqlScope scope)
    {
        if (expression is IntegerLiteral position)
            return Positional(position, row.Values, scope);

        if (expression is ColumnReferenceExpression { MultiPartIdentifier.Count: 1 } named && Alias(named, items) is { } index)
            return GenericResult<object?>.Success(row.Values[index]);

        return SqlExpressions.Evaluate(expression, row.Source);
    }

    private static IGenericResult<object?> Positional(IntegerLiteral position, object?[] values, SqlScope scope)
    {
        var ordinal = int.Parse(position.Value, CultureInfo.InvariantCulture);
        return ordinal >= 1 && ordinal <= values.Length
            ? GenericResult<object?>.Success(values[ordinal - 1])
            : SqlRefusal.Refuse<object?>(new SqlServerMessage(108, 1, 16,
                $"The ORDER BY position number {ordinal} is out of range of the number of items in the select list.", position.StartLine), scope.Context.Logger);
    }

    private static int? Alias(ColumnReferenceExpression named, IReadOnlyList<OutputItem> items)
    {
        var name = named.MultiPartIdentifier.Identifiers[0].Value;
        for (var i = 0; i < items.Count; i++)
        {
            if (string.Equals(items[i].Column.Name, name, StringComparison.OrdinalIgnoreCase))
                return i;
        }

        return null;
    }

    private sealed class KeyComparer : IComparer<object?[]>
    {
        private readonly bool[] _descending;

        public KeyComparer(bool[] descending) => _descending = descending;

        public int Compare(object?[]? x, object?[]? y)
        {
            for (var i = 0; i < _descending.Length; i++)
            {
                var order = Order(x![i], y![i]);
                if (order != 0)
                    return _descending[i] ? -order : order;
            }

            return 0;
        }

        // NULL sorts below every value; values that do not compare keep their order.
        private static int Order(object? a, object? b)
        {
            if (a is null || b is null)
                return (a is null ? 0 : 1) - (b is null ? 0 : 1);

            return SqlValues.TryCompare(a, b, out var order) ? order : 0;
        }
    }
}
