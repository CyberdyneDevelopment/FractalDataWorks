using FluentValidation;
using Fdw.Web.Calculations.Clients.Models;

namespace Fdw.Web.Calculations.Clients.Validation;

/// <summary>
/// Validator for CalculationEditModel.
/// </summary>
public class CalculationEditModelValidator : AbstractValidator<CalculationEditModel>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CalculationEditModelValidator"/> class.
    /// </summary>
    public CalculationEditModelValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Name is required")
            .Length(1, 128).WithMessage("Name must be 1-128 characters");

        RuleFor(x => x.TargetDataSet)
            .NotEmpty().WithMessage("Target DataSet is required");

        RuleFor(x => x.ResultFieldName)
            .NotEmpty().WithMessage("Result field name is required")
            .Matches("^[a-zA-Z][a-zA-Z0-9_]*$").WithMessage("Result field name must start with a letter and contain only letters, digits, and underscores");

        RuleFor(x => x.ResultDataType)
            .NotEmpty().WithMessage("Result data type is required");

        RuleFor(x => x.Formula)
            .NotEmpty().WithMessage("Formula is required")
            .MaximumLength(10000).WithMessage("Formula must be 10000 characters or less");

        RuleFor(x => x.Description)
            .MaximumLength(2000).WithMessage("Description must be 2000 characters or less");
    }
}
