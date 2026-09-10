using FluentValidation;
using Fdw.Validation;
using Fdw.Services.Quality.Configuration;

namespace Fdw.Services.Quality.Validators;

/// <summary>
/// Validator for <see cref="GlossaryTermImplementationConfiguration"/>.
/// </summary>
public sealed class GlossaryTermConfigurationValidator : FdwConfigurationValidator<GlossaryTermImplementationConfiguration>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="GlossaryTermConfigurationValidator"/> class.
    /// </summary>
    public GlossaryTermConfigurationValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .WithMessage("Name is required");
    }
}
