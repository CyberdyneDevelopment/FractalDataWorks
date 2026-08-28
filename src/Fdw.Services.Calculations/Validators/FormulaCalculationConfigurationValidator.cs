using FluentValidation;
using Fdw.Validation;

namespace Fdw.Services.Calculations.Validators;

/// <summary>
/// Validator for <see cref="FormulaCalculationConfiguration"/>.
/// </summary>
public sealed class FormulaCalculationConfigurationValidator : FdwConfigurationValidator<FormulaCalculationConfiguration>
{
    private static readonly string[] ValidLanguages = ["CSharp", "Sql"];

    /// <summary>
    /// Initializes a new instance of the <see cref="FormulaCalculationConfigurationValidator"/> class.
    /// </summary>
    public FormulaCalculationConfigurationValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .WithMessage("Name is required");

        RuleFor(x => x.SectionName)
            .NotEmpty()
            .WithMessage("SectionName is required");

        RuleFor(x => x.ServiceType)
            .NotEmpty()
            .WithMessage("ServiceType is required");

        RuleFor(x => x.ServiceOptionType)
            .NotEmpty()
            .WithMessage("ServiceOptionType is required");

        RuleFor(x => x.FormulaBody)
            .NotEmpty()
            .WithMessage("FormulaBody is required");

        RuleFor(x => x.FormulaLanguage)
            .NotEmpty()
            .WithMessage("FormulaLanguage is required")
            .Must(l => System.Array.IndexOf(ValidLanguages, l) >= 0)
            .WithMessage("FormulaLanguage must be one of: CSharp, Sql");

        RuleFor(x => x.TimeoutSeconds)
            .GreaterThanOrEqualTo(1)
            .WithMessage("TimeoutSeconds must be at least 1");
    }
}
