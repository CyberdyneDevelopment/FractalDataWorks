using FluentValidation;

namespace Fdw.Services.Calculations;

/// <summary>
/// Validator for <see cref="FormulaCalculationConfiguration"/>.
/// Enforces non-empty identity/section fields and a valid formula body.
/// </summary>
public sealed class FormulaCalculationConfigurationValidator : AbstractValidator<FormulaCalculationConfiguration>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="FormulaCalculationConfigurationValidator"/> class.
    /// </summary>
    public FormulaCalculationConfigurationValidator()
    {
        RuleFor(x => x.Name).NotEmpty();
        RuleFor(x => x.SectionName).NotEmpty();
        RuleFor(x => x.ServiceType).NotEmpty();
        RuleFor(x => x.ServiceOptionType).NotEmpty();
        RuleFor(x => x.FormulaLanguage).NotEmpty();
        RuleFor(x => x.FormulaBody).NotEmpty();
        RuleFor(x => x.TimeoutSeconds).GreaterThan(0);
    }
}
