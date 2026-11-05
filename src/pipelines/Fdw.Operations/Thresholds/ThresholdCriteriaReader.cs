using System;
using System.Collections.Generic;
using System.Linq;
using Fdw.Data;
using Fdw.Data.Abstractions;
using Fdw.Operations.Configuration;
using Fdw.Results;

namespace Fdw.Operations.Thresholds;

/// <summary>
/// Builds the criteria of a stored rule (FDW-3936): the filter model, read from the rule's filter group and condition rows. The criteria are judged by the filter model's own
/// predicate logic, so no comparison is written here. A rule whose rows do not make a criteria is refused naming what is wrong; nothing is assumed or repaired.
/// </summary>
public static class ThresholdCriteriaReader
{
    internal const string AllOperator = "All";

    internal const string AnyOperator = "Any";

    /// <summary>Reads the criteria of a rule.</summary>
    /// <param name="threshold">The threshold, for a failure message.</param>
    /// <param name="rule">The stored rule.</param>
    /// <returns>The filter node; a failure naming what is wrong with the rows.</returns>
    public static IGenericResult<IFilterNode> From(string threshold, ThresholdRuleConfiguration rule)
    {
        ArgumentNullException.ThrowIfNull(rule);
        var roots = rule.FilterGroups.Where(group => group.ParentGroupId is null).ToList();
        if (roots.Count != 1)
        {
            return Invalid(threshold, rule, roots.Count == 0 ? "it has no root filter group" : "it has more than one root filter group");
        }

        var visited = new HashSet<Guid>();
        var criteria = Group(roots[0], rule, visited, threshold);
        if (criteria.IsFailure)
        {
            return criteria;
        }

        // Every group of the rule must be one the root reaches: a group left over hangs from no group of the rule or from a cycle of groups, and
        // rows that are stored but never read are not a criteria to judge by.
        return rule.FilterGroups.FirstOrDefault(group => !visited.Contains(group.Id)) is { } stray
            ? Invalid(threshold, rule, MakesCycle(stray, rule) ? "its filter groups make a cycle" : "a filter group hangs from no group of the rule")
            : criteria;
    }

    // Whether following a group's parent links comes back to a group it has already passed.
    private static bool MakesCycle(ThresholdFilterGroupConfiguration group, ThresholdRuleConfiguration rule)
    {
        var seen = new HashSet<Guid>();
        ThresholdFilterGroupConfiguration? at = group;
        while (at is not null && seen.Add(at.Id))
        {
            at = Parent(at, rule);
        }

        return at is not null;
    }

    private static ThresholdFilterGroupConfiguration? Parent(ThresholdFilterGroupConfiguration group, ThresholdRuleConfiguration rule)
    {
        return group.ParentGroupId is { } parentId ? rule.FilterGroups.FirstOrDefault(candidate => candidate.Id == parentId) : null;
    }

    private static IGenericResult<IFilterNode> Group(ThresholdFilterGroupConfiguration group, ThresholdRuleConfiguration rule, HashSet<Guid> visited, string threshold)
    {
        if (!visited.Add(group.Id))
        {
            return Invalid(threshold, rule, "its filter groups make a cycle");
        }

        var logical = group.Operator switch
        {
            AllOperator => LogicalOperator.And,
            AnyOperator => LogicalOperator.Or,
            _ => null,
        };
        if (logical is null)
        {
            return Invalid(threshold, rule, $"a filter group has the operator '{group.Operator}', which is neither {AllOperator} nor {AnyOperator}");
        }

        var children = new List<IFilterNode>();
        foreach (var condition in group.Conditions.OrderBy(condition => condition.Ordinal))
        {
            var node = Condition(condition, rule, threshold);
            if (node.IsFailure || node.Value is null)
            {
                return node;
            }

            children.Add(node.Value);
        }

        foreach (var child in rule.FilterGroups.Where(candidate => candidate.ParentGroupId == group.Id).OrderBy(candidate => candidate.Ordinal))
        {
            var node = Group(child, rule, visited, threshold);
            if (node.IsFailure || node.Value is null)
            {
                return node;
            }

            children.Add(node.Value);
        }

        return children.Count == 0
            ? Invalid(threshold, rule, "a filter group has no condition and no group in it")
            : GenericResult<IFilterNode>.Success(new FilterGroup { Operator = logical, Nodes = children });
    }

    private static IGenericResult<IFilterNode> Condition(ThresholdFilterConditionConfiguration condition, ThresholdRuleConfiguration rule, string threshold)
    {
        if (string.IsNullOrWhiteSpace(condition.FieldName))
        {
            return Invalid(threshold, rule, "a condition names no field");
        }

        var filterOperator = FilterOperators.ByName(condition.Operator);
        if (ReferenceEquals(filterOperator, FilterOperators.NotFound))
        {
            return Invalid(threshold, rule, $"'{condition.Operator}' is not a filter operator");
        }

        if (string.Equals(filterOperator.Name, "In", StringComparison.Ordinal))
        {
            return Invalid(threshold, rule, "the operator 'In' takes a list and a stored condition holds one value; state the values as conditions in an Any group");
        }

        if (!filterOperator.RequiresValue)
        {
            return GenericResult<IFilterNode>.Success(new FilterCondition { PropertyName = condition.FieldName, Operator = filterOperator });
        }

        var value = ValueOf(condition);
        return value is null
            ? Invalid(threshold, rule, $"the condition on '{condition.FieldName}' has a value that is missing or is not a {condition.DataType}")
            : GenericResult<IFilterNode>.Success(new FilterCondition { PropertyName = condition.FieldName, Operator = filterOperator, Value = value });
    }

    private static object? ValueOf(ThresholdFilterConditionConfiguration condition)
    {
        return condition.Value is { } text ? ThresholdValueText.Read(condition.DataType, text) : null;
    }

    private static IGenericResult<IFilterNode> Invalid(string threshold, ThresholdRuleConfiguration rule, string reason) =>
        ThresholdRuleFailures.RuleInvalid(threshold, rule.Ordinal, reason).ToNewResult<IFilterNode>();
}
