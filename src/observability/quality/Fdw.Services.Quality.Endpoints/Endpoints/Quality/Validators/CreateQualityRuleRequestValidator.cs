using FluentValidation;
using Fdw.Services.Quality.Configuration;
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
        // Why the data set name is not written here: it is the quality rule configuration validator's rule, taken through RulesOf so the request
        // answers 400 for what the save check would refuse (FDW-3884). The request carries no Name, so the validator's Name rule is not
        // reported against it; RuleType and Severity are stated here because that validator does not state them.
        RulesOf(request => new QualityRuleImplementationConfiguration
        {
            DataSetName = request.DataSetName,
            FieldName = request.FieldName,
            RuleType = request.RuleType,
            Severity = request.Severity ?? string.Empty,
            Description = request.Description,
        });

        RuleFor(x => x.RuleType)
            .NotEmpty()
            .WithMessage("RuleType is required");

        RuleFor(x => x.Severity)
            .Must(s => System.Array.IndexOf(ValidSeverities, s) >= 0)
            .WithMessage("Severity must be one of: Info, Warning, Error, Critical");
    }
}
