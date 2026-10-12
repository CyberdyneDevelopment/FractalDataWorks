using System;
using System.Collections.Generic;
using Fdw.Commands.Data.Abstractions;
using Fdw.Data;
using Fdw.Data.Abstractions;
using Fdw.Results;
using Fdw.Services.Connections.Logging;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Connections.RowQuery;

/// <summary>
/// Validates that a <c>QueryCommand</c>'s filter/join shape is one <see cref="RecordRowMatcher"/> can
/// evaluate in memory: every filter condition must be an equality or IN predicate combined with AND,
/// and INNER joins with a single condition pair each, chained from the queried container, are permitted (the typed-body parent-join
/// read shape <c>ConfigurationCommandBase.GetByParentJoin</c>/<c>GetByParentJoinSet</c> emit — the
/// latter's <c>WhereIn</c> is where IN comes from). Anything outside that grammar (a non-equality,
/// non-IN operator, an OR group, more than one join) fails loud — NO FALLBACKS.
/// </summary>
public static class RecordQueryValidator
{
    /// <summary>
    /// Validates both the join count/shape and the filter tree.
    /// </summary>
    public static IGenericResult ValidateShape(IFilterNode? filterRoot, IReadOnlyList<IJoinExpression> joins, ILogger logger)
    {
        var joinResult = ValidateJoins(joins, logger);
        if (!joinResult.IsSuccess)
            return joinResult;

        return ValidateFilter(filterRoot, logger);
    }

    private static IGenericResult ValidateJoins(IReadOnlyList<IJoinExpression> joins, ILogger logger)
    {
        if (joins.Count == 0)
            return GenericResult.Success();

        // Each join is INNER on one column pair, and starts from the queried container or from a
        // container an earlier join brought in -- the chain a child row walks to its implementation
        // row and on to the domain row.
        var joined = new HashSet<string>(StringComparer.Ordinal);
        foreach (var join in joins)
        {
            if (!string.Equals(join.JoinType, "INNER", StringComparison.OrdinalIgnoreCase) || join.JoinConditions.Count != 1)
                return GenericResult.Failure(RecordQueryLog.UnsupportedJoinShape(logger, join.JoinType, join.JoinConditions.Count));

            if (join.SourceContainerName is { } source && !joined.Contains(source))
                return GenericResult.Failure(RecordQueryLog.UnknownJoinSource(logger, source, join.TargetContainerName));

            joined.Add(join.TargetContainerName);
        }

        return GenericResult.Success();
    }

    private static IGenericResult ValidateFilter(IFilterNode? node, ILogger logger)
    {
        switch (node)
        {
            case null:
                return GenericResult.Success();

            case FilterCondition condition:
                if (!string.Equals(condition.Operator.SqlOperator, "=", StringComparison.Ordinal)
                    && !string.Equals(condition.Operator.SqlOperator, "IN", StringComparison.Ordinal))
                    return GenericResult.Failure(RecordQueryLog.UnsupportedFilterOperator(logger, condition.PropertyName, condition.Operator.SqlOperator));
                return GenericResult.Success();

            case FilterGroup group:
                if (group.Operator != LogicalOperator.And)
                    return GenericResult.Failure(RecordQueryLog.UnsupportedFilterOperator(logger, "(group)", group.Operator.SqlOperator));

                foreach (var child in group.Nodes)
                {
                    var childResult = ValidateFilter(child, logger);
                    if (!childResult.IsSuccess)
                        return childResult;
                }
                return GenericResult.Success();

            default:
                return GenericResult.Failure(RecordQueryLog.UnsupportedFilterNodeType(logger, node.GetType().Name));
        }
    }
}
