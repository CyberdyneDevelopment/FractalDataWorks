using FluentValidation;
using Fdw.Validation;
using Fdw.Operations.Configuration;

namespace Fdw.Operations.Validators;

/// <summary>
/// Validator for <see cref="EscalationLevelRecipientConfiguration"/>.
/// </summary>
public sealed class EscalationLevelRecipientConfigurationValidator : FdwConfigurationValidator<EscalationLevelRecipientConfiguration>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="EscalationLevelRecipientConfigurationValidator"/> class.
    /// </summary>
    public EscalationLevelRecipientConfigurationValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .WithMessage("Name is required");

        RuleFor(x => x.EscalationLevelId)
            .NotEqual(System.Guid.Empty)
            .WithMessage("EscalationLevelId is required");

        RuleFor(x => x.Recipient)
            .NotEmpty()
            .WithMessage("Recipient is required");

        RuleFor(x => x.RecipientType)
            .NotEmpty()
            .WithMessage("RecipientType is required");
    }
}
