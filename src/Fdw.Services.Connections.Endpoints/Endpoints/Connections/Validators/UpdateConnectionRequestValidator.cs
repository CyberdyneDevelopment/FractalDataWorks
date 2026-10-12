using FluentValidation;
using Fdw.Validation.FastEndpoints;

namespace Fdw.Services.Connections.Endpoints.Validators;

/// <summary>
/// Validator for <see cref="UpdateConnectionRequest"/>.
/// </summary>
public abstract class UpdateConnectionRequestValidator : FdwEndpointValidator<UpdateConnectionRequest>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="UpdateConnectionRequestValidator"/> class.
    /// </summary>
    protected UpdateConnectionRequestValidator()
    {
        When(x => x.Server is not null, () =>
        {
            RuleFor(x => x.Server!)
                .NotEmpty()
                .WithMessage("Server must not be empty when provided")
                .MaximumLength(256)
                .WithMessage("Server must not exceed 256 characters");
        });

        When(x => x.Database is not null, () =>
        {
            RuleFor(x => x.Database!)
                .NotEmpty()
                .WithMessage("Database must not be empty when provided")
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
