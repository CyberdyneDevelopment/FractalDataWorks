using FluentValidation;
using Fdw.Validation;
using Fdw.Services.Quality.Configuration;

namespace Fdw.Services.Quality.Validators;

/// <summary>
/// Validator for <see cref="EnvironmentConfiguration"/>.
/// </summary>
public sealed class EnvironmentConfigurationValidator : FdwConfigurationValidator<EnvironmentConfiguration>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="EnvironmentConfigurationValidator"/> class.
    /// </summary>
    public EnvironmentConfigurationValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .WithMessage("Name is required");
    }
}
