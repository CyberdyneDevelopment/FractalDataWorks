using System;
using System.Collections.Generic;
using System.Linq;
using Fdw.Data;
using Fdw.Data.Abstractions;

namespace Fdw.Web.RestEndpoints.Query;

/// <summary>
/// Applies the part of a <see cref="QueryPlan"/> the store cannot run: a filter and an ordering over rows already read, then
/// the page (FDW-1045).
/// </summary>
/// <remarks>
/// Each operator decides its own match (<see cref="FilterOperatorBase.Matches"/>), so a value is judged here as it is in
/// every other in-memory reader: numbers by value, text without regard to case. A missing value matches no operator that
/// needs one, which is what a store does with a null, so the same filter keeps the same rows on either side of the read.
/// </remarks>
public static class InMemoryQuery
{
    private static readonly IComparer<object?> ValueOrder = new NullsFirst();

    /// <summary>Filters, orders and pages the rows a plan read.</summary>
    /// <param name="rows">The rows the store handed over.</param>
    /// <param name="plan">The plan whose in-memory parts to apply.</param>
    /// <returns>The page: <see cref="QueryPlan.Skip"/> rows skipped and at most one past <see cref="QueryPlan.Take"/> kept, so a following page can be reported.</returns>
    public static List<Dictionary<string, object?>> Apply(IEnumerable<Dictionary<string, object?>> rows, QueryPlan plan)
    {
        ArgumentNullException.ThrowIfNull(rows);
        ArgumentNullException.ThrowIfNull(plan);

        var named = rows.Select(row => new Dictionary<string, object?>(row, StringComparer.OrdinalIgnoreCase));
        var kept = plan.InMemoryFilter is { } filter ? named.Where(row => Matches(row, filter)) : named;
        var ordered = plan.InMemoryOrdering is { } ordering ? Sort(kept, ordering) : kept;
        return ordered.Skip(plan.Skip).Take(plan.Take + 1).ToList();
    }

    /// <summary>Whether a row satisfies a filter tree.</summary>
    /// <param name="row">The row, keyed by field name.</param>
    /// <param name="node">The filter node.</param>
    /// <returns>True when the row satisfies it.</returns>
    public static bool Matches(IReadOnlyDictionary<string, object?> row, IFilterNode node)
        => node switch
        {
            FilterGroup group => ReferenceEquals(group.Operator, LogicalOperator.Or)
                ? group.Nodes.Any(child => Matches(row, child))
                : group.Nodes.All(child => Matches(row, child)),
            FilterCondition condition => Satisfies(row, condition),
            _ => throw new InvalidOperationException($"A filter node of type '{node.GetType().Name}' reached the in-memory reader; the query translator builds only groups and conditions."),
        };

    private static bool Satisfies(IReadOnlyDictionary<string, object?> row, FilterCondition condition)
    {
        if (condition.Operator is not FilterOperatorBase op)
            throw new InvalidOperationException($"Operator '{condition.Operator.Name}' cannot be applied in memory; only registered filter operators can.");

        row.TryGetValue(condition.PropertyName, out var actual);
        return (actual is not null || !op.RequiresValue) && op.Matches(actual, condition.Value);
    }

    private static IEnumerable<Dictionary<string, object?>> Sort(IEnumerable<Dictionary<string, object?>> rows, IOrderingExpression ordering)
    {
        IOrderedEnumerable<Dictionary<string, object?>>? ordered = null;
        foreach (var field in ordering.OrderedFields)
        {
            var name = field.PropertyName;
            var descending = ReferenceEquals(field.Direction, SortDirections.ByName("Descending"));
            object? Key(Dictionary<string, object?> row) => row.TryGetValue(name, out var value) ? value : null;

            ordered = ordered is null
                ? (descending ? rows.OrderByDescending(Key, ValueOrder) : rows.OrderBy(Key, ValueOrder))
                : (descending ? ordered.ThenByDescending(Key, ValueOrder) : ordered.ThenBy(Key, ValueOrder));
        }

        return ordered ?? rows;
    }

    // A missing value sorts first when ascending and last when descending, as SQL Server sorts a null. The comparison of two
    // values is the operators' own, so a number is ordered by value and text without regard to case.
    private sealed class NullsFirst : IComparer<object?>
    {
        public int Compare(object? x, object? y)
            => x is null ? (y is null ? 0 : -1) : y is null ? 1 : FilterValueComparer.Compare(x, y);
    }
}
