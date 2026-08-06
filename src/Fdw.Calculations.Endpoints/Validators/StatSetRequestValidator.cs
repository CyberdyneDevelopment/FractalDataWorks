using FluentValidation;
using Fdw.Services.Data.Abstractions.Visualization;
using Fdw.Validation.FastEndpoints;

namespace Fdw.Calculations.Endpoints.Validators;

/// <summary>
/// Validator for <see cref="StatSetRequest"/> — enforces the container coordinates
/// (DataStoreName / PathName / ContainerName) that StatSetService relies on to
/// build a DataGateway query.
/// </summary>
public sealed class StatSetRequestValidator : FdwEndpointValidator<StatSetRequest>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="StatSetRequestValidator"/> class.
    /// </summary>
    public StatSetRequestValidator()
    {
        RuleFor(x => x.DataStoreName)
            .NotEmpty()
            .WithMessage("DataStoreName is required");

        RuleFor(x => x.PathName)
            .NotEmpty()
            .WithMessage("PathName is required");

        RuleFor(x => x.ContainerName)
            .NotEmpty()
            .WithMessage("ContainerName is required");
    }
}
