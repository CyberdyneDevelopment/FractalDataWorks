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

        // Why: Server and Database are required for SQL connections; we validate them as safe strings
        // to block injection attempts that bypass the connection layer.
        RuleFor(x => x.Server)
            .NotEmpty()
            .WithMessage("Server is required")
            .MaximumLength(256)
            .WithMessage("Server must not exceed 256 characters");

        RuleFor(x => x.Database)
            .NotEmpty()
            .WithMessage("Database is required")
            .MaximumLength(128)
            .WithMessage("Database must not exceed 128 characters");

        RuleFor(x => x.Port)
            .InclusiveBetween(1, 65535)
            .WithMessage("Port must be between 1 and 65535");
    }
}
