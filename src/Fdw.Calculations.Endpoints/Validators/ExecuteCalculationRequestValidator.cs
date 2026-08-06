using FluentValidation;
using Fdw.Calculations.Abstractions.CalculationTypeOptions;
using Fdw.Validation.FastEndpoints;
using Fdw.Web.Calculations.Clients.Models;

namespace Fdw.Calculations.Endpoints.Validators;

/// <summary>
/// Validator for <see cref="ExecuteCalculationRequest"/>. Enforces request *shape*: CalculationType
/// must be a registered option, and the caller must supply either inline Values or DataSetName +
/// FieldName. DataSet *existence* is a resource-state concern handled by the endpoint with 404
/// semantics, not 400.
/// </summary>
public sealed class ExecuteCalculationRequestValidator : FdwEndpointValidator<ExecuteCalculationRequest>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ExecuteCalculationRequestValidator"/> class.
    /// </summary>
    public ExecuteCalculationRequestValidator()
    {
        RuleFor(x => x.CalculationType)
            .NotEmpty()
            .WithMessage("CalculationType is required")
            .Must(name => CalculationTypes.ByName(name).Id != 0)
            .WithMessage(req => $"Unknown calculation type: {req.CalculationType}");

        // Why: either path is valid — inline Values, or a DataSet projection.
        // Both empty is a contract violation; surface a single composite error.
        RuleFor(x => x)
            .Must(req => req.Values.Count > 0 || (req.DataSetName.Length > 0 && req.FieldName.Length > 0))
            .WithMessage("Provide either inline Values, or DataSetName + FieldName to project from.");
    }
}
