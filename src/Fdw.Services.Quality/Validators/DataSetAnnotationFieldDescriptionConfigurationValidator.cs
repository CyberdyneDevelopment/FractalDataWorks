using FluentValidation;
using Fdw.Validation;
using Fdw.Services.Quality.Configuration;

namespace Fdw.Services.Quality.Validators;

/// <summary>
/// Validator for <see cref="DataSetAnnotationFieldDescriptionConfiguration"/>.
/// </summary>
public sealed class DataSetAnnotationFieldDescriptionConfigurationValidator : FdwConfigurationValidator<DataSetAnnotationFieldDescriptionConfiguration>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DataSetAnnotationFieldDescriptionConfigurationValidator"/> class.
    /// </summary>
    public DataSetAnnotationFieldDescriptionConfigurationValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .WithMessage("Name is required");

        RuleFor(x => x.FieldName)
            .NotEmpty()
            .WithMessage("FieldName is required");

        RuleFor(x => x.Description)
            .NotEmpty()
            .WithMessage("Description is required");
    }
}
