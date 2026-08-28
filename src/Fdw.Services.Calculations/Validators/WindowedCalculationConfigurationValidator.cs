using FluentValidation;
using Fdw.Validation;

namespace Fdw.Services.Calculations.Validators;

/// <summary>
/// Validator for <see cref="WindowedCalculationConfiguration"/>.
/// </summary>
public sealed class WindowedCalculationConfigurationValidator : FdwConfigurationValidator<WindowedCalculationConfiguration>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="WindowedCalculationConfigurationValidator"/> class.
    /// </summary>
    public WindowedCalculationConfigurationValidator()
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

        RuleFor(x => x.TargetField)
            .NotEmpty()
            .WithMessage("TargetField is required");

        RuleFor(x => x.WindowFunction)
            .NotEmpty()
            .WithMessage("WindowFunction is required");

        RuleFor(x => x.OutputFieldName)
            .NotEmpty()
            .WithMessage("OutputFieldName is required");
    }
}
