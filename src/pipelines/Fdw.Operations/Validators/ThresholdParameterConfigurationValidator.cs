using System;
using System.Linq;
using FluentValidation;
using Fdw.Operations.Thresholds;
using Fdw.Operations.Configuration;
using Fdw.Validation;

namespace Fdw.Operations.Validators;

/// <summary>
/// Validator for <see cref="ThresholdParameterConfiguration"/> (FDW-4046): a parameter names itself, a data type that can be read, and a value that reads as that type.
/// </summary>
public sealed class ThresholdParameterConfigurationValidator : FdwConfigurationValidator<ThresholdParameterConfiguration>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ThresholdParameterConfigurationValidator"/> class.
    /// </summary>
    public ThresholdParameterConfigurationValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .WithMessage("Name is required")
            .MaximumLength(100)
            .WithMessage("Name must not exceed 100 characters");

        RuleFor(x => x.DataType)
            .NotEmpty()
            .WithMessage("DataType is required")
            .Must(dataType => string.IsNullOrEmpty(dataType) || ThresholdValueText.DataTypes.Contains(dataType, StringComparer.Ordinal))
            .WithMessage(x => $"DataType '{x.DataType}' is not one of {string.Join(", ", ThresholdValueText.DataTypes)}");

        RuleFor(x => x)
            .Must(parameter => parameter.Value is { } text && ThresholdValueText.Read(parameter.DataType, text) is not null)
            .When(x => ThresholdValueText.DataTypes.Contains(x.DataType, StringComparer.Ordinal))
            .WithName("Value")
            .WithMessage(x => $"The value of '{x.Name}' is missing or is not a {x.DataType}");
    }
}
