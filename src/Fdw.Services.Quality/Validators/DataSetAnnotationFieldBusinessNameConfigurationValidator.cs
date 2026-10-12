using FluentValidation;
using Fdw.Validation;
using Fdw.Services.Quality.Configuration;

namespace Fdw.Services.Quality.Validators;

/// <summary>
/// Validator for <see cref="DataSetAnnotationFieldBusinessNameConfiguration"/>.
/// </summary>
public sealed class DataSetAnnotationFieldBusinessNameConfigurationValidator : FdwConfigurationValidator<DataSetAnnotationFieldBusinessNameConfiguration>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DataSetAnnotationFieldBusinessNameConfigurationValidator"/> class.
    /// </summary>
    public DataSetAnnotationFieldBusinessNameConfigurationValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .WithMessage("Name is required");

        RuleFor(x => x.FieldName)
            .NotEmpty()
            .WithMessage("FieldName is required");

        RuleFor(x => x.BusinessName)
            .NotEmpty()
            .WithMessage("BusinessName is required");
    }
}
