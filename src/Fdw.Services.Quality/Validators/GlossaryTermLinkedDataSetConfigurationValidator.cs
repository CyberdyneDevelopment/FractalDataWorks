using FluentValidation;
using Fdw.Validation;
using Fdw.Services.Quality.Configuration;

namespace Fdw.Services.Quality.Validators;

/// <summary>
/// Validator for <see cref="GlossaryTermLinkedDataSetConfiguration"/>.
/// </summary>
public sealed class GlossaryTermLinkedDataSetConfigurationValidator : FdwConfigurationValidator<GlossaryTermLinkedDataSetConfiguration>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="GlossaryTermLinkedDataSetConfigurationValidator"/> class.
    /// </summary>
    public GlossaryTermLinkedDataSetConfigurationValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .WithMessage("Name is required");

        RuleFor(x => x.DataSetName)
            .NotEmpty()
            .WithMessage("DataSetName is required");
    }
}
