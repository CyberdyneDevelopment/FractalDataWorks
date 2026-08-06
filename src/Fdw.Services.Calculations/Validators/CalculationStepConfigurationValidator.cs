using FluentValidation;
using Fdw.Validation;
using Fdw.Services.Calculations.Configuration;

namespace Fdw.Services.Calculations.Validators;

/// <summary>
/// Validator for <see cref="CalculationStepConfiguration"/>.
/// </summary>
public sealed class CalculationStepConfigurationValidator : FdwConfigurationValidator<CalculationStepConfiguration>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CalculationStepConfigurationValidator"/> class.
    /// </summary>
    public CalculationStepConfigurationValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .WithMessage("Name is required");

        RuleFor(x => x.OperationType)
            .NotEmpty()
            .WithMessage("OperationType is required");

        RuleFor(x => x.OutputAlias)
            .NotEmpty()
            .WithMessage("OutputAlias is required");

        RuleFor(x => x.Ordinal)
            .GreaterThanOrEqualTo(0)
            .WithMessage("Ordinal must be 0 or greater");
    }
}
