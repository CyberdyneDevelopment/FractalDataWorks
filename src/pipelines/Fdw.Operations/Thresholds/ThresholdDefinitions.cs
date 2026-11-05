using System;
using System.Collections.Generic;
using System.Linq;
using Fdw.Data.Abstractions;
using Fdw.Operations.Abstractions.Thresholds;
using Fdw.Operations.Configuration;
using Fdw.Results;

namespace Fdw.Operations.Thresholds;

/// <summary>
/// Turns a stored threshold into the definition the evaluator judges (FDW-3936), refusing one that cannot be judged: a type that is not a threshold type, a rule that names
/// a value the type does not report, a consequence that does not exist, an Alert without a policy, a Notify without notification rules, or criteria text that does not parse.
/// Nothing is assumed or repaired.
/// </summary>
public static class ThresholdDefinitions
{
    /// <summary>Reads a stored threshold.</summary>
    /// <param name="stored">The threshold as the configuration provider returned it.</param>
    /// <returns>The definition; a failure naming the first thing wrong with it.</returns>
    public static IGenericResult<IThresholdDefinition> From(IThresholdImplementationConfiguration stored)
    {
        ArgumentNullException.ThrowIfNull(stored);
        var type = ThresholdTypes.ByName(stored.Implementation);
        if (ReferenceEquals(type, ThresholdTypes.NotFound))
        {
            return ThresholdRuleFailures.TypeUnknown(stored.Name, stored.Implementation).ToNewResult<IThresholdDefinition>();
        }

        if (!string.Equals(type.SubjectKind, stored.SubjectKind, StringComparison.Ordinal))
        {
            return ThresholdRuleFailures.KindMismatch(stored.Name, stored.SubjectKind, type.SubjectKind).ToNewResult<IThresholdDefinition>();
        }

        var parameters = ThresholdParameterReader.From(type, stored.Name, stored.Parameters);
        if (parameters.IsFailure || parameters.Value is null)
        {
            return parameters.ToNewResult<IThresholdDefinition>();
        }

        var rules = new List<ThresholdRule>();
        foreach (var rule in stored.Rules.OrderBy(rule => rule.Ordinal))
        {
            var read = Rule(type, stored.Name, rule);
            if (read.IsFailure || read.Value is null)
            {
                return read.ToNewResult<IThresholdDefinition>();
            }

            rules.Add(read.Value);
        }

        return GenericResult<IThresholdDefinition>.Success(new Threshold<IThreshold>(type, stored.Name, stored.IsEnabled, stored.FiresOnEntry, parameters.Value, rules));
    }

    private static IGenericResult<ThresholdRule> Rule(IThreshold type, string threshold, ThresholdRuleConfiguration stored)
    {
        var criteria = ThresholdCriteriaReader.From(threshold, stored);
        if (criteria.IsFailure || criteria.Value is null)
        {
            return criteria.ToNewResult<ThresholdRule>();
        }

        var known = type.Fields.Select(field => field.Name).ToHashSet(StringComparer.OrdinalIgnoreCase);
        if (ThresholdCriteria.FieldsNamed(criteria.Value).FirstOrDefault(name => !known.Contains(name)) is { } unknown)
        {
            return Invalid(threshold, stored.Ordinal, $"it names '{unknown}', which the type '{type.Name}' does not report");
        }

        if (Unaccepted(type, criteria.Value) is { } refused)
        {
            return Invalid(threshold, stored.Ordinal, refused);
        }

        var escalation = EscalationTypes.ByName(stored.Escalation);
        if (ReferenceEquals(escalation, EscalationTypes.NotFound))
        {
            return Invalid(threshold, stored.Ordinal, $"'{stored.Escalation}' is not an escalation type");
        }

        var notifications = stored.Notifications.OrderBy(notification => notification.Ordinal).Select(notification => notification.NotificationRuleName).ToList();

        if (escalation.NeedsPolicy && string.IsNullOrWhiteSpace(stored.EscalationPolicy))
        {
            return Invalid(threshold, stored.Ordinal, $"the escalation '{escalation.Name}' needs an escalation policy and none is named");
        }

        return escalation.NeedsNotificationRules && notifications.Count == 0
            ? Invalid(threshold, stored.Ordinal, $"the escalation '{escalation.Name}' needs notification rules and none is named")
            : GenericResult<ThresholdRule>.Success(new ThresholdRule(criteria.Value, escalation.Name, stored.EscalationPolicy, notifications));
    }

    // A condition whose operator its field does not accept is refused, naming both. The fields themselves were checked just before.
    private static string? Unaccepted(IThreshold type, IFilterNode criteria)
    {
        foreach (var condition in ThresholdCriteria.ConditionsOf(criteria))
        {
            var field = type.Fields.First(candidate => string.Equals(candidate.Name, condition.PropertyName, StringComparison.OrdinalIgnoreCase));
            if (!field.Operators.Contains(condition.Operator.Name, StringComparer.Ordinal))
            {
                return $"the field '{field.Name}' does not accept the comparison '{condition.Operator.Name}'; it accepts {string.Join(", ", field.Operators)}";
            }
        }

        return null;
    }

    private static IGenericResult<ThresholdRule> Invalid(string threshold, int rule, string reason) =>
        ThresholdRuleFailures.RuleInvalid(threshold, rule, reason).ToNewResult<ThresholdRule>();
}
