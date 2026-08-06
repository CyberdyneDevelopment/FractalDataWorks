using FluentValidation;
using Fdw.Validation;
using Fdw.Services.Quality.Configuration;

namespace Fdw.Services.Quality.Validators;

/// <summary>
/// Validator for <see cref="DataSetAnnotationConfiguration"/>.
/// </summary>
public sealed class DataSetAnnotationConfigurationValidator : FdwConfigurationValidator<DataSetAnnotationConfiguration>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DataSetAnnotationConfigurationValidator"/> class.
    /// </summary>
    public DataSetAnnotationConfigurationValidator()
    {
        RuleFor(x => x.DataSetName)
            .NotEmpty()
            .WithMessage("DataSetName is required");
    }
}
