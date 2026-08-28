using FluentValidation;
using Fdw.Validation;
using Fdw.Services.Calculations.Configuration;

namespace Fdw.Services.Calculations.Validators;

/// <summary>
/// Validator for <see cref="CalculationStepOperandConfiguration"/>.
/// </summary>
public sealed class CalculationStepOperandConfigurationValidator : FdwConfigurationValidator<CalculationStepOperandConfiguration>
{
    private static readonly string[] ValidOperandTypes = ["Input", "StepReference", "Literal"];

    /// <summary>
    /// Initializes a new instance of the <see cref="CalculationStepOperandConfigurationValidator"/> class.
    /// </summary>
    public CalculationStepOperandConfigurationValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .WithMessage("Name is required");

        RuleFor(x => x.OperandType)
            .NotEmpty()
            .WithMessage("OperandType is required")
            .Must(t => System.Array.IndexOf(ValidOperandTypes, t) >= 0)
            .WithMessage("OperandType must be one of: Input, StepReference, Literal");

        When(x => string.Equals(x.OperandType, "Input", System.StringComparison.OrdinalIgnoreCase), () =>
        {
            RuleFor(x => x.InputAlias)
                .NotEmpty()
                .WithMessage("InputAlias is required when OperandType is Input");
        });

        When(x => string.Equals(x.OperandType, "StepReference", System.StringComparison.OrdinalIgnoreCase), () =>
        {
            RuleFor(x => x.StepAlias)
                .NotEmpty()
                .WithMessage("StepAlias is required when OperandType is StepReference");
        });

        When(x => string.Equals(x.OperandType, "Literal", System.StringComparison.OrdinalIgnoreCase), () =>
        {
            RuleFor(x => x.LiteralValue)
                .NotEmpty()
                .WithMessage("LiteralValue is required when OperandType is Literal");
        });
    }
}
