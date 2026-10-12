using FluentValidation;
using Fdw.Validation;
using Fdw.Services.Quality.Configuration;

namespace Fdw.Services.Quality.Validators;

/// <summary>
/// Validator for <see cref="DataSetAnnotationTagConfiguration"/>.
/// </summary>
public sealed class DataSetAnnotationTagConfigurationValidator : FdwConfigurationValidator<DataSetAnnotationTagConfiguration>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DataSetAnnotationTagConfigurationValidator"/> class.
    /// </summary>
    public DataSetAnnotationTagConfigurationValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .WithMessage("Name is required");

        RuleFor(x => x.Tag)
            .NotEmpty()
            .WithMessage("Tag is required");
    }
}
