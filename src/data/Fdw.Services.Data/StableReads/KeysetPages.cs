using System.Collections.Generic;
using System.Linq;
using Fdw.Data;
using Fdw.Data.Abstractions;

namespace Fdw.Services.Data;

/// <summary>
/// The parts of a keyset page (FDW-4911): the order every key field is read in, and the filter that asks for the rows after the last key.
/// </summary>
/// <remarks>
/// For keys <c>a, b, c</c> and a last key <c>x, y, z</c> the filter is <c>a &gt; x OR (a = x AND b &gt; y) OR (a = x AND b = y AND c &gt; z)</c>:
/// the rows that sort after the last one in the order the page states. Every key field is read ascending, so the one filter serves every
/// field. The filter compares in the store, so it follows the store's own collation, the same one its ordering follows.
/// </remarks>
internal static class KeysetPages
{
    /// <summary>The order a page is read in: every key field, ascending, in the key's order.</summary>
    /// <param name="keyFields">The key's fields, in order.</param>
    /// <returns>The ordering.</returns>
    public static IOrderingExpression Order(IReadOnlyList<string> keyFields) =>
        new OrderingExpression
        {
            OrderedFields = [.. keyFields.Select(field => (IOrderedField)new OrderedField { PropertyName = field, Direction = SortDirections.Ascending })],
        };

    /// <summary>The filter a page is read with: the caller's own filter, and, after the first page, the rows after the last key.</summary>
    /// <param name="callerFilter">The caller's filter, or null when it states none.</param>
    /// <param name="keyFields">The key's fields, in order.</param>
    /// <param name="lastKey">The key values of the last row read, in the key's order; null before the first page.</param>
    /// <returns>The filter, or null when there is neither a caller's filter nor a last key.</returns>
    public static IFilterExpression? Filter(IFilterExpression? callerFilter, IReadOnlyList<string> keyFields, IReadOnlyList<object?>? lastKey)
    {
        if (lastKey is null)
            return callerFilter;

        var after = After(keyFields, lastKey);
        return callerFilter?.Root is { } callerRoot
            ? new FilterExpression { Root = new FilterGroup { Operator = LogicalOperator.And, Nodes = [callerRoot, after] } }
            : new FilterExpression { Root = after };
    }

    // One branch per key field: the fields before it equal to the last key, and it greater than its value.
    private static IFilterNode After(IReadOnlyList<string> keyFields, IReadOnlyList<object?> lastKey)
    {
        var branches = new List<IFilterNode>(keyFields.Count);
        for (var depth = 0; depth < keyFields.Count; depth++)
        {
            var conditions = new List<IFilterNode>(depth + 1);
            for (var i = 0; i < depth; i++)
                conditions.Add(Condition(keyFields[i], "Equal", lastKey[i]));

            conditions.Add(Condition(keyFields[depth], "GreaterThan", lastKey[depth]));
            branches.Add(conditions.Count == 1 ? conditions[0] : new FilterGroup { Operator = LogicalOperator.And, Nodes = conditions });
        }

        return branches.Count == 1 ? branches[0] : new FilterGroup { Operator = LogicalOperator.Or, Nodes = branches };
    }

    private static FilterCondition Condition(string field, string operatorName, object? value) =>
        new() { PropertyName = field, Operator = FilterOperators.ByName(operatorName), Value = value };
}
