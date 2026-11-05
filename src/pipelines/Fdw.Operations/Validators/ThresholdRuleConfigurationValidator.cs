using System.Linq;
using FluentValidation;
using Fdw.Operations.Abstractions.Thresholds;
using Fdw.Operations.Configuration;
using Fdw.Validation;

namespace Fdw.Operations.Validators;

/// <summary>
/// Validator for <see cref="ThresholdRuleConfiguration"/> (FDW-4046): a rule names an escalation type that exists, the policy or notification rules that escalation needs, and filter groups and notifications that are each valid.
/// </summary>
public sealed class ThresholdRuleConfigurationValidator : FdwConfigurationValidator<ThresholdRuleConfiguration>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ThresholdRuleConfigurationValidator"/> class.
    /// </summary>
    public ThresholdRuleConfigurationValidator()
    {
        RuleFor(x => x.Escalation)
            .NotEmpty()
            .WithMessage("Escalation is required")
            .MaximumLength(100)
            .WithMessage("Escalation must not exceed 100 characters")
            .Must(escalation => string.IsNullOrEmpty(escalation) || !ReferenceEquals(EscalationTypes.ByName(escalation), EscalationTypes.NotFound))
            .WithMessage(x => $"Escalation '{x.Escalation}' is not an escalation type");

        RuleFor(x => x.EscalationPolicy)
            .MaximumLength(256)
            .WithMessage("EscalationPolicy must not exceed 256 characters");

        RuleFor(x => x.EscalationPolicy)
            .NotEmpty()
            .When(x => EscalationTypes.ByName(x.Escalation) is { } escalation && !ReferenceEquals(escalation, EscalationTypes.NotFound) && escalation.NeedsPolicy)
            .WithMessage(x => $"The escalation '{x.Escalation}' needs an escalation policy and none is named");

        RuleFor(x => x.Notifications)
            .NotEmpty()
            .When(x => EscalationTypes.ByName(x.Escalation) is { } escalation && !ReferenceEquals(escalation, EscalationTypes.NotFound) && escalation.NeedsNotificationRules)
            .WithMessage(x => $"The escalation '{x.Escalation}' needs notification rules and none is named");

        RuleFor(x => x.Notifications)
            .Must(notifications => notifications.Select(notification => notification.Ordinal).Distinct().Count() == notifications.Count)
            .WithMessage("The Ordinal of each notification must differ from the others");

        RuleForEach(x => x.FilterGroups).SetValidator(new ThresholdFilterGroupConfigurationValidator());
        RuleForEach(x => x.Notifications).SetValidator(new ThresholdRuleNotificationConfigurationValidator());
    }
}
