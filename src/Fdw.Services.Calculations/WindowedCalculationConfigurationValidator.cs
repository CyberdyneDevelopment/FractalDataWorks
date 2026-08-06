using FluentValidation;

namespace Fdw.Services.Calculations;

/// <summary>
/// Validator for <see cref="WindowedCalculationConfiguration"/>.
/// Enforces non-empty identity/section fields and required window descriptors.
/// </summary>
public sealed class WindowedCalculationConfigurationValidator : AbstractValidator<WindowedCalculationConfiguration>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="WindowedCalculationConfigurationValidator"/> class.
    /// </summary>
    public WindowedCalculationConfigurationValidator()
    {
        RuleFor(x => x.Name).NotEmpty();
        RuleFor(x => x.SectionName).NotEmpty();
        RuleFor(x => x.ServiceType).NotEmpty();
        RuleFor(x => x.ServiceOptionType).NotEmpty();
        RuleFor(x => x.TargetField).NotEmpty();
        RuleFor(x => x.WindowFunction).NotEmpty();
        RuleFor(x => x.OutputFieldName).NotEmpty();
    }
}
