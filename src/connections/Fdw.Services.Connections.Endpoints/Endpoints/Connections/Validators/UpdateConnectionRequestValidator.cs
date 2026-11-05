using FluentValidation;
using Fdw.Validation.FastEndpoints;

namespace Fdw.Services.Connections.Endpoints.Validators;

/// <summary>
/// The rules an update-connection request must meet in any host. A host derives from this class and adds only what the host alone knows (FDW-3572).
/// </summary>
public abstract class UpdateConnectionRequestValidator : FdwEndpointValidator<UpdateConnectionRequest>
{
    /// <summary>Initializes a new instance of the <see cref="UpdateConnectionRequestValidator"/> class.</summary>
    protected UpdateConnectionRequestValidator()
    {
        // Why NotEmpty only: the name is the route's, naming a connection that exists; a name rule here would refuse an update to a stored name
        // that a seed or an older version wrote in another form.
        RuleFor(x => x.Name)
            .NotEmpty()
            .WithMessage("Connection name is required in the route");

        // Why: a request with an empty body or only the route-bound Name is a no-op. At least one updateable property must be supplied so the
        // caller gets a structured 400 instead of a silent 200. The request is shared across connection types (an HTTP connection is updated
        // with a base url, a protocol, a timeout), so the check cannot name the relational fields only; UpdateConnectionChanges holds the
        // members once, with what counts as stated for each, and words the refusal (FDW-1177).
        RuleFor(x => x)
            .Must(request => UpdateConnectionChanges.Stated(request))
            .WithErrorCode(UpdateConnectionChanges.NothingStatedCode)
            .WithMessage(UpdateConnectionChanges.RefusalMessage);

        // Why no NotEmpty on Server and Database: an empty string is not stated (UpdateConnectionChanges counts text only when it has some),
        // so it changes nothing and needs no refusal; only a length that cannot be stored is refused.
        When(x => x.Server is not null, () =>
        {
            RuleFor(x => x.Server!)
                .MaximumLength(256)
                .WithMessage("Server must not exceed 256 characters");
        });

        When(x => x.Database is not null, () =>
        {
            RuleFor(x => x.Database!)
                .MaximumLength(128)
                .WithMessage("Database must not exceed 128 characters");
        });

        When(x => x.Port.HasValue, () =>
        {
            RuleFor(x => x.Port!.Value)
                .InclusiveBetween(1, 65535)
                .WithMessage("Port must be between 1 and 65535");
        });
    }
}
