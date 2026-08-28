using FluentValidation;
using Fdw.Validation;
using Fdw.Services.Calculations.Configuration;

namespace Fdw.Services.Calculations.Validators;

/// <summary>
/// Validator for <see cref="CalculationEntityConfiguration"/>.
/// </summary>
public sealed class CalculationEntityConfigurationValidator : FdwConfigurationValidator<CalculationEntityConfiguration>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CalculationEntityConfigurationValidator"/> class.
    /// </summary>
    public CalculationEntityConfigurationValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .WithMessage("Name is required");

        When(x => string.Equals(x.CalculationEntityType, "Formula", System.StringComparison.OrdinalIgnoreCase), () =>
        {
            RuleFor(x => x.CalculationEntityType)
                .NotEmpty()
                .WithMessage("Formula is required for FormulaCalculation entities");
        });
    }
}
