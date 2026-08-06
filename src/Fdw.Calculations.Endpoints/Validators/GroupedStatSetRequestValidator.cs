using FluentValidation;
using Fdw.Services.Data.Abstractions.Visualization;
using Fdw.Validation.FastEndpoints;

namespace Fdw.Calculations.Endpoints.Validators;

/// <summary>
/// Validator for <see cref="GroupedStatSetRequest"/> — enforces the container
/// coordinates that StatSetService relies on to build a DataGateway query.
/// </summary>
public sealed class GroupedStatSetRequestValidator : FdwEndpointValidator<GroupedStatSetRequest>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="GroupedStatSetRequestValidator"/> class.
    /// </summary>
    public GroupedStatSetRequestValidator()
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
