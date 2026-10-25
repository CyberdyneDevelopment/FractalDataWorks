using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Fdw.Data;
using Fdw.Data.Abstractions;
using Fdw.Data.Abstractions.Rows;
using Fdw.Commands.Data.Logging;
using Fdw.Results;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Commands.Data.Expansions;

/// <summary>Builds the filter that selects the one row a source row stands for, from its key column values.</summary>
internal static class KeyFilters
{
    /// <summary>Makes an equality filter over the key columns of a source row.</summary>
    /// <param name="commandType">The command being expanded, for messages.</param>
    /// <param name="keyColumns">The key columns.</param>
    /// <param name="row">The source row, a dictionary of column values or an entity.</param>
    /// <returns>The filter; or a failure naming the key column the row has no value for.</returns>
    internal static IGenericResult<IFilterExpression> ForRow(string commandType, IReadOnlyList<string> keyColumns, object? row)
    {
        var conditions = new List<IFilterNode>(keyColumns.Count);
        foreach (var key in keyColumns)
        {
            if (!RowValues.TryRead(row, key, out var value, out _) || value is null)
                return GenericResult<IFilterExpression>.Failure(
                    CommandExpansionLog.KeyValueMissing(NullLogger.Instance, commandType, key));

            conditions.Add(new FilterCondition { PropertyName = key, Operator = FilterOperators.ByName("Equal"), Value = value });
        }

        return GenericResult<IFilterExpression>.Success(new FilterExpression
        {
            Root = conditions.Count == 1
                ? conditions[0]
                : new FilterGroup { Operator = LogicalOperator.And, Nodes = conditions },
        });
    }

    /// <summary>Makes the filters that select the rows a set of source rows stand for, each filter covering a group of rows.</summary>
    /// <param name="commandType">The command being expanded, for messages.</param>
    /// <param name="keyColumns">The key columns.</param>
    /// <param name="rows">The source rows.</param>
    /// <param name="keysPerFilter">The most key values one filter holds, so a delete carries no more parameters than an insert of that many values.</param>
    /// <returns>One filter for each group of rows, matching any of the group's keys; or a failure naming the key column a row has no value for, or the key two rows share.</returns>
    internal static IGenericResult<IReadOnlyList<IFilterExpression>> ForRows<T>(
        string commandType, IReadOnlyList<string> keyColumns, IReadOnlyList<T> rows, int keysPerFilter)
    {
        var rowsPerFilter = Math.Max(1, keysPerFilter / Math.Max(1, keyColumns.Count));
        var seen = new HashSet<string>(StringComparer.Ordinal);
        var filters = new List<IFilterExpression>();
        var group = new List<IFilterNode>(rowsPerFilter);
        foreach (var row in rows)
        {
            var one = ForRow(commandType, keyColumns, row);
            if (one.IsFailure)
                return one.ToNewResult<IReadOnlyList<IFilterExpression>>();

            var key = string.Join(',', keyColumns.Select(column => KeyText(row, column)));
            if (!seen.Add(key))
                return GenericResult<IReadOnlyList<IFilterExpression>>.Failure(
                    CommandExpansionLog.NotComposable(NullLogger.Instance, commandType, $"two rows share the key ({key}), which a bulk replace would write twice"));

            group.Add(one.Value!.Root!);
            if (group.Count < rowsPerFilter)
                continue;

            filters.Add(AnyOf(group));
            group = new List<IFilterNode>(rowsPerFilter);
        }

        if (group.Count > 0)
            filters.Add(AnyOf(group));

        return GenericResult<IReadOnlyList<IFilterExpression>>.Success(filters);
    }

    private static string? KeyText(object? row, string column)
        => RowValues.TryRead(row, column, out var value, out _) ? Convert.ToString(value, CultureInfo.InvariantCulture) : null;

    private static FilterExpression AnyOf(List<IFilterNode> nodes)
        => new() { Root = nodes.Count == 1 ? nodes[0] : new FilterGroup { Operator = LogicalOperator.Or, Nodes = nodes } };
}
