using System;
using System.Collections.Generic;
using System.Linq;
using Fdw.Data.Abstractions;

namespace Fdw.Data;

/// <summary>
/// Applies a filter to rows in memory, for sources whose translator cannot express it natively.
/// </summary>
/// <remarks>
/// This is the other half of <c>IQueryCapability</c>. A translator says what the native command can
/// express; whatever it declines is applied here before the rows leave. The operators do the
/// comparing, so a delimited file and a SQL table agree on what a filter means.
/// </remarks>
public static class LocalFilterEvaluator
{
    /// <summary>Whether a row satisfies the filter.</summary>
    /// <param name="filter">The filter to apply. A null filter matches everything.</param>
    /// <param name="row">The row, keyed by field name.</param>
    /// <returns><see langword="true"/> when the row should be kept.</returns>
    public static bool Matches(IFilterExpression? filter, IReadOnlyDictionary<string, object?> row)
    {
        ArgumentNullException.ThrowIfNull(row);
        return filter?.Root is null || Matches(filter.Root, row);
    }

    /// <summary>Whether a row satisfies a node of the filter tree.</summary>
    /// <param name="node">The node — a condition or a group.</param>
    /// <param name="row">The row, keyed by field name.</param>
    /// <returns><see langword="true"/> when the row should be kept.</returns>
    /// <remarks>
    /// An unknown node kind returns false rather than true. Keeping a row a filter was never
    /// evaluated against is how unfiltered data reaches a caller who believes it was filtered,
    /// which is the defect this whole mechanism exists to close.
    /// </remarks>
    public static bool Matches(IFilterNode node, IReadOnlyDictionary<string, object?> row)
    {
        ArgumentNullException.ThrowIfNull(node);
        ArgumentNullException.ThrowIfNull(row);

        return node switch
        {
            FilterCondition condition => MatchesCondition(condition, row),
            FilterGroup group => MatchesGroup(group, row),
            _ => false,
        };
    }

    private static bool MatchesCondition(FilterCondition condition, IReadOnlyDictionary<string, object?> row)
    {
        // Why a missing field is not a match: the row does not carry the column the filter names, so
        // nothing can be said about it. Treating absence as a match would let a typo widen a filter.
        if (!row.TryGetValue(condition.PropertyName, out var value))
        {
            return false;
        }

        return condition.Operator is FilterOperatorBase op && op.Matches(value, condition.Value);
    }

    private static bool MatchesGroup(FilterGroup group, IReadOnlyDictionary<string, object?> row)
    {
        // Why an empty group matches: it constrains nothing, and the alternative — dropping every
        // row — turns a no-op group into a silent data loss.
        if (group.Nodes.Count == 0)
        {
            return true;
        }

        return group.Operator == LogicalOperator.Or
            ? group.Nodes.Any(n => Matches(n, row))
            : group.Nodes.All(n => Matches(n, row));
    }
}
