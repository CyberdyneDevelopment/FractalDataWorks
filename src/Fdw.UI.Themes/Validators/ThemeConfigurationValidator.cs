using FluentValidation;
using Fdw.Validation;
using Fdw.UI.Themes.Configuration;

namespace Fdw.UI.Themes.Validators;

/// <summary>
/// Validator for <see cref="ThemeImplementationConfiguration"/>.
/// </summary>
public sealed class ThemeConfigurationValidator : FdwConfigurationValidator<ThemeImplementationConfiguration>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ThemeConfigurationValidator"/> class.
    /// </summary>
    public ThemeConfigurationValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .WithMessage("Name is required");
    }
}
