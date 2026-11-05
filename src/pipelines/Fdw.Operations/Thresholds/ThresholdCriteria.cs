using System;
using System.Collections.Generic;
using Fdw.Data;
using Fdw.Data.Abstractions;

namespace Fdw.Operations.Thresholds;

/// <summary>Reads what a rule criteria names (FDW-3936).</summary>
public static class ThresholdCriteria
{
    /// <summary>Lists the fields a criteria names, each once, in the order it first names them.</summary>
    /// <param name="criteria">The criteria: conditions in AND/OR groups.</param>
    /// <returns>The field names.</returns>
    public static IReadOnlyList<string> FieldsNamed(IFilterNode criteria)
    {
        ArgumentNullException.ThrowIfNull(criteria);
        var fields = new List<string>();
        Collect(criteria, fields);
        return fields;
    }

    /// <summary>Lists the conditions of a criteria, in the order they are stated.</summary>
    /// <param name="criteria">The criteria: conditions in AND/OR groups.</param>
    /// <returns>Every condition, at any depth.</returns>
    public static IReadOnlyList<FilterCondition> ConditionsOf(IFilterNode criteria)
    {
        ArgumentNullException.ThrowIfNull(criteria);
        var conditions = new List<FilterCondition>();
        CollectConditions(criteria, conditions);
        return conditions;
    }

    private static void CollectConditions(IFilterNode node, List<FilterCondition> conditions)
    {
        switch (node)
        {
            case FilterGroup group:
                foreach (var child in group.Nodes)
                {
                    CollectConditions(child, conditions);
                }

                break;
            case FilterCondition condition:
                conditions.Add(condition);
                break;
            default:
                throw new InvalidOperationException($"A criteria node of type '{node.GetType().Name}' is neither a group nor a condition.");
        }
    }

    private static void Collect(IFilterNode node, List<string> fields)
    {
        switch (node)
        {
            case FilterGroup group:
                foreach (var child in group.Nodes)
                {
                    Collect(child, fields);
                }

                break;
            case FilterCondition condition:
                if (!fields.Contains(condition.PropertyName, StringComparer.OrdinalIgnoreCase))
                {
                    fields.Add(condition.PropertyName);
                }

                break;
            default:
                throw new InvalidOperationException($"A criteria node of type '{node.GetType().Name}' is neither a group nor a condition.");
        }
    }
}
