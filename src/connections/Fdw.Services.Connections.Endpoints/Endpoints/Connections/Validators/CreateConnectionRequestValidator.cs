using FluentValidation;
using Fdw.Validation.FastEndpoints;

namespace Fdw.Services.Connections.Endpoints.Validators;

/// <summary>
/// The rules a create-connection request must meet in any host. A host derives from this class (so the endpoint scan finds a
/// concrete validator) and adds only what the host alone knows, such as which service types it registered (FDW-3572).
/// </summary>
/// <remarks>
/// A connection's name column is 200 characters, so that is the limit here; a tighter host limit would refuse names the store holds.
/// </remarks>
public abstract class CreateConnectionRequestValidator : FdwEndpointValidator<CreateConnectionRequest>
{
    /// <summary>Initializes a new instance of the <see cref="CreateConnectionRequestValidator"/> class.</summary>
    protected CreateConnectionRequestValidator()
    {
        ValidateName(x => x.Name);

        RuleFor(x => x.ServiceType)
            .NotEmpty()
            .WithMessage("Service type is required");

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

        // Why no Port rule here: Port is the same deprecated flat field, and a kind with no port (an HTTP connection) cannot meet a range
        // rule written for every kind. Whether a port is required and in range is the type's rule, run when its configuration is bound
        // (the SQL Server and PostgreSQL validators state 1 to 65535), so a bad port is still refused where a port exists.
    }
}
