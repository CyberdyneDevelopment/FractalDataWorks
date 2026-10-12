using FluentValidation;
using Fdw.Validation.FastEndpoints;

namespace Fdw.Services.Connections.Endpoints.Validators;

/// <summary>
/// Validator for <see cref="CreateConnectionRequest"/>.
/// </summary>
public abstract class CreateConnectionRequestValidator : FdwEndpointValidator<CreateConnectionRequest>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CreateConnectionRequestValidator"/> class.
    /// </summary>
    protected CreateConnectionRequestValidator()
    {
        ValidateName(x => x.Name);

        RuleFor(x => x.ServiceType)
            .NotEmpty()
            .WithMessage("ServiceType is required");

        // Why only when supplied: these are the deprecated flat fields, and most connection types have no
        // server or database at all. What a type requires is the type's own rule, checked when its
        // configuration is bound (IConnectionType.EndpointBehavior), not here.
        When(x => !string.IsNullOrEmpty(x.Server), () =>
        {
            RuleFor(x => x.Server)
                .MaximumLength(256)
                .WithMessage("Server must not exceed 256 characters");
        });

        When(x => !string.IsNullOrEmpty(x.Database), () =>
        {
            RuleFor(x => x.Database)
                .MaximumLength(128)
                .WithMessage("Database must not exceed 128 characters");
        });

        RuleFor(x => x.Port)
            .InclusiveBetween(1, 65535)
            .WithMessage("Port must be between 1 and 65535");
    }
}
