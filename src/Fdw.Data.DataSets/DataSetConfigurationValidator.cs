using Fdw.Configuration;
using FluentValidation;

namespace Fdw.Data.DataSets.Abstractions;

/// <summary>
/// Validator for DataSetConfiguration instances.
/// Ensures all required fields are provided and valid.
/// </summary>
public sealed class DataSetConfigurationValidator : AbstractValidator<DataSetConfiguration>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DataSetConfigurationValidator"/> class.
    /// </summary>
    public DataSetConfigurationValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .WithMessage("Dataset name is required")
            .MaximumLength(100)
            .WithMessage("Dataset name must not exceed 100 characters");

        RuleFor(x => x.Description)
            .NotEmpty()
            .WithMessage("Description is required")
            .MaximumLength(500)
            .WithMessage("Description must not exceed 500 characters");

        RuleFor(x => x.RecordTypeName)
            .NotEmpty()
            .WithMessage("Record type name is required")
            .Must(BeValidTypeName)
            .WithMessage("Record type name must be a valid .NET type name");

        RuleFor(x => x.Fields)
            .NotEmpty()
            .WithMessage("At least one field must be defined");

        RuleForEach(x => x.Fields)
            .SetValidator(new DataFieldConfigurationValidator());

        RuleFor(x => x.KeyFields)
            .NotEmpty()
            .WithMessage("At least one key field must be specified");

    }

    private static bool BeValidTypeName(string typeName)
    {
        if (string.IsNullOrWhiteSpace(typeName)) return false;

        // Basic validation - could be enhanced with more sophisticated type name parsing
        return !typeName.Contains(' ') && typeName.Contains('.');
    }

}