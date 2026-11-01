using System.Linq;
using FluentValidation;
using Fdw.Validation;
using Fdw.Services.Notifications.Configuration;

namespace Fdw.Services.Notifications.Validators;

/// <summary>
/// Validator for <see cref="NotificationRuleImplementationConfiguration"/>.
/// </summary>
public sealed class NotificationRuleConfigurationValidator : FdwConfigurationValidator<NotificationRuleImplementationConfiguration>
{
    // How a rule combines its conditions. A rule saves as "Or" and nothing in a request sets it, but the column is NOT NULL and the
    // member is settable, so a value outside the set is refused here, not stored.
    private static readonly string[] ValidConditionOperators = ["And", "Or"];

    /// <summary>
    /// Initializes a new instance of the <see cref="NotificationRuleConfigurationValidator"/> class.
    /// </summary>
    public NotificationRuleConfigurationValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .WithMessage("Name is required");

        RuleFor(x => x.NotificationServiceType)
            .NotEmpty()
            .WithMessage("NotificationServiceType is required");

        RuleFor(x => x.NotificationServiceName)
            .NotEmpty()
            .WithMessage("NotificationServiceName is required");

        RuleFor(x => x.ConditionOperator)
            .Must(op => System.Array.IndexOf(ValidConditionOperators, op) >= 0)
            .WithMessage("ConditionOperator must be 'And' or 'Or'");

        RuleFor(x => x.Severity)
            .Must(NotificationRuleSeverities.Offers)
            .WithMessage($"Severity must be one of: {string.Join(", ", NotificationRuleSeverities.All().Select(pair => pair.Key))}");

        When(x => x.CooldownMinutes.HasValue, () =>
        {
            RuleFor(x => x.CooldownMinutes!.Value)
                .GreaterThanOrEqualTo(1)
                .WithMessage("CooldownMinutes must be at least 1");
        });

        When(x => x.Description is not null, () =>
        {
            RuleFor(x => x.Description!)
                .IsSafeString(1000);
        });
    }
}
