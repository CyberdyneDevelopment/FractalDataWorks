using FluentValidation;
using Fdw.Validation.FastEndpoints;

namespace Fdw.Services.Quality.Endpoints.Validators;

/// <summary>
/// Validator for <see cref="CreateQualityRuleRequest"/>.
/// </summary>
public abstract class CreateQualityRuleRequestValidator : FdwEndpointValidator<CreateQualityRuleRequest>
{
    private static readonly string[] ValidSeverities = ["Info", "Warning", "Error", "Critical"];

    /// <summary>
    /// Initializes a new instance of the <see cref="CreateQualityRuleRequestValidator"/> class.
    /// </summary>
    protected CreateQualityRuleRequestValidator()
    {
        RuleFor(x => x.DataSetName)
            .NotEmpty()
            .WithMessage("DataSetName is required");

        RuleFor(x => x.RuleType)
            .NotEmpty()
            .WithMessage("RuleType is required");

        RuleFor(x => x.Severity)
            .Must(s => System.Array.IndexOf(ValidSeverities, s) >= 0)
            .WithMessage("Severity must be one of: Info, Warning, Error, Critical");
    }
}
