using Fdw.Calculations.Endpoints.CalculationEntities;
using FluentValidation;
using Fdw.Validation.FastEndpoints;

namespace Fdw.Calculations.Endpoints.Validators;

/// <summary>
/// Validator for <see cref="CreateCalculationEntityRequest"/>.
/// </summary>
public sealed class CreateCalculationEntityRequestValidator : FdwEndpointValidator<CreateCalculationEntityRequest>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CreateCalculationEntityRequestValidator"/> class.
    /// </summary>
    public CreateCalculationEntityRequestValidator()
    {
        ValidateName(x => x.Name);

        RuleFor(x => x.CalculationEntityType)
            .NotEmpty()
            .WithMessage("CalculationEntityType is required");

        RuleFor(x => x.ResultDataTypeName)
            .NotEmpty()
            .WithMessage("ResultDataTypeName is required");
    }
}
