using FluentValidation;
using Fdw.Validation;
using Fdw.Services.Notifications.Configuration;

namespace Fdw.Services.Notifications.Validators;

/// <summary>
/// Validator for <see cref="NotificationRuleConfiguration"/>.
/// </summary>
public sealed class NotificationRuleConfigurationValidator : FdwConfigurationValidator<NotificationRuleConfiguration>
{
    private static readonly string[] ValidConditionOperators = ["And", "Or"];
    private static readonly string[] ValidSeverities = ["Info", "Warning", "Error", "Critical"];

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
            .Must(s => System.Array.IndexOf(ValidSeverities, s) >= 0)
            .WithMessage("Severity must be one of: Info, Warning, Error, Critical");

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
