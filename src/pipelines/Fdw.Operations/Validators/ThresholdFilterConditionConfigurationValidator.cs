using System;
using System.Linq;
using FluentValidation;
using Fdw.Data;
using Fdw.Operations.Thresholds;
using Fdw.Operations.Configuration;
using Fdw.Validation;

namespace Fdw.Operations.Validators;

/// <summary>
/// Validator for <see cref="ThresholdFilterConditionConfiguration"/> (FDW-4046): a condition names a field, a filter operator that exists and takes one value, a data type that can be read, and a value that reads as that type when the operator takes one.
/// </summary>
public sealed class ThresholdFilterConditionConfigurationValidator : FdwConfigurationValidator<ThresholdFilterConditionConfiguration>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ThresholdFilterConditionConfigurationValidator"/> class.
    /// </summary>
    public ThresholdFilterConditionConfigurationValidator()
    {
        RuleFor(x => x.FieldName)
            .NotEmpty()
            .WithMessage("FieldName is required")
            .MaximumLength(100)
            .WithMessage("FieldName must not exceed 100 characters");

        RuleFor(x => x.Operator)
            .NotEmpty()
            .WithMessage("Operator is required")
            .MaximumLength(50)
            .WithMessage("Operator must not exceed 50 characters")
            .Must(name => string.IsNullOrEmpty(name) || !ReferenceEquals(FilterOperators.ByName(name), FilterOperators.NotFound))
            .WithMessage(x => $"'{x.Operator}' is not a filter operator")
            .Must(name => string.IsNullOrEmpty(name) || !string.Equals(FilterOperators.ByName(name).Name, "In", StringComparison.Ordinal))
            .WithMessage("The operator 'In' takes a list and a condition holds one value; state the values as conditions in an Any group");

        RuleFor(x => x.DataType)
            .NotEmpty()
            .WithMessage("DataType is required")
            .Must(dataType => string.IsNullOrEmpty(dataType) || ThresholdValueText.DataTypes.Contains(dataType, StringComparer.Ordinal))
            .WithMessage(x => $"DataType '{x.DataType}' is not one of {string.Join(", ", ThresholdValueText.DataTypes)}");

        RuleFor(x => x.Value)
            .MaximumLength(400)
            .WithMessage("Value must not exceed 400 characters");

        RuleFor(x => x)
            .Must(condition => condition.Value is { } text && ThresholdValueText.Read(condition.DataType, text) is not null)
            .When(x => !ReferenceEquals(FilterOperators.ByName(x.Operator), FilterOperators.NotFound) && FilterOperators.ByName(x.Operator).RequiresValue && ThresholdValueText.DataTypes.Contains(x.DataType, StringComparer.Ordinal))
            .WithName("Value")
            .WithMessage(x => $"The condition on '{x.FieldName}' has a value that is missing or is not a {x.DataType}");
    }
}
