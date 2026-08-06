using FluentValidation;
using Fdw.Validation;
using Fdw.Operations.Configuration;

namespace Fdw.Operations.Validators;

/// <summary>
/// Validator for <see cref="EscalationLevelConfiguration"/>.
/// </summary>
public sealed class EscalationLevelConfigurationValidator : FdwConfigurationValidator<EscalationLevelConfiguration>
{
    private static readonly string[] ValidSeverities = ["Info", "Warning", "Error", "Critical"];

    /// <summary>
    /// Initializes a new instance of the <see cref="EscalationLevelConfigurationValidator"/> class.
    /// </summary>
    public EscalationLevelConfigurationValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .WithMessage("Name is required");

        RuleFor(x => x.EscalationPolicyId)
            .NotEqual(System.Guid.Empty)
            .WithMessage("EscalationPolicyId is required");

        RuleFor(x => x.Level)
            .GreaterThanOrEqualTo(1)
            .WithMessage("Level must be at least 1");

        RuleFor(x => x.DelayMinutes)
            .GreaterThanOrEqualTo(0)
            .WithMessage("DelayMinutes must be 0 or greater");

        RuleFor(x => x.NotificationChannel)
            .NotEmpty()
            .WithMessage("NotificationChannel is required");

        RuleFor(x => x.Severity)
            .NotEmpty()
            .WithMessage("Severity is required")
            .Must(s => System.Array.IndexOf(ValidSeverities, s) >= 0)
            .WithMessage("Severity must be one of: Info, Warning, Error, Critical");
    }
}
