using FluentValidation;
using Fdw.Validation;
using Fdw.UI.Themes.Configuration;

namespace Fdw.UI.Themes.Validators;

/// <summary>
/// Validator for <see cref="ThemeManagedConfiguration"/>.
/// </summary>
public sealed class ThemeConfigurationValidator : FdwConfigurationValidator<ThemeManagedConfiguration>
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
