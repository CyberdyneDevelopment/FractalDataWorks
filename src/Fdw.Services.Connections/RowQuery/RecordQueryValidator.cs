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
/// evaluate in memory: every filter condition must be an equality predicate combined with AND, and at
/// most one INNER join with a single condition pair is permitted (the typed-body parent-join read
/// shape <c>ConfigurationCommandBase.GetByParentJoin</c> emits). Anything outside that grammar (a
/// non-equality operator, an OR group, more than one join) fails loud — NO FALLBACKS.
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

        if (joins.Count > 1)
            return GenericResult.Failure(RecordQueryLog.UnsupportedJoinCount(logger, joins.Count));

        var join = joins[0];
        if (!string.Equals(join.JoinType, "INNER", StringComparison.OrdinalIgnoreCase) || join.JoinConditions.Count != 1)
            return GenericResult.Failure(RecordQueryLog.UnsupportedJoinShape(logger, join.JoinType, join.JoinConditions.Count));

        return GenericResult.Success();
    }

    private static IGenericResult ValidateFilter(IFilterNode? node, ILogger logger)
    {
        switch (node)
        {
            case null:
                return GenericResult.Success();

            case FilterCondition condition:
                if (!string.Equals(condition.Operator.SqlOperator, "=", StringComparison.Ordinal))
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
