using FluentValidation;
using Fdw.Validation.FastEndpoints;

namespace Fdw.Services.SecretManagers.Endpoints.Validators;

/// <summary>
/// Validator for <see cref="CreateSecretManagerRequest"/>.
/// </summary>
public sealed class CreateSecretManagerRequestValidator : FdwEndpointValidator<CreateSecretManagerRequest>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CreateSecretManagerRequestValidator"/> class.
    /// </summary>
    public CreateSecretManagerRequestValidator()
    {
        ValidateName(x => x.Name);

        RuleFor(x => x.SecretManagerType)
            .NotEmpty()
            .WithMessage("SecretManagerType is required");

        RuleFor(x => x.Configuration)
            .NotNull()
            .WithMessage("Configuration object is required");

        When(x => x.Description is not null, () =>
        {
            RuleFor(x => x.Description!)
                .MaximumLength(1000)
                .WithMessage("Description must not exceed 1000 characters");
        });
    }
}
