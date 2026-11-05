using FluentValidation;
using Fdw.Operations.Configuration;
using Fdw.Validation;

namespace Fdw.Operations.Validators;

/// <summary>
/// Validator for <see cref="ThresholdRuleNotificationConfiguration"/> (FDW-4046): a notification names the notification rule it sends.
/// </summary>
public sealed class ThresholdRuleNotificationConfigurationValidator : FdwConfigurationValidator<ThresholdRuleNotificationConfiguration>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ThresholdRuleNotificationConfigurationValidator"/> class.
    /// </summary>
    public ThresholdRuleNotificationConfigurationValidator()
    {
        RuleFor(x => x.NotificationRuleName)
            .NotEmpty()
            .WithMessage("NotificationRuleName is required")
            .MaximumLength(256)
            .WithMessage("NotificationRuleName must not exceed 256 characters");
    }
}
