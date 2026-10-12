using System;
using FluentValidation;
using Fdw.Validation.FastEndpoints;
using Fdw.Services.Users.Clients.Models;

namespace Fdw.Services.Users.Endpoints.Validators;

/// <summary>
/// Validator for <see cref="CreateUserRequest"/>.
/// </summary>
/// <typeparam name="TRequest">The request type, host-extensible beyond <see cref="CreateUserRequest"/>.</typeparam>
public abstract class CreateUserRequestValidator<TRequest> : FdwEndpointValidator<TRequest>
    where TRequest : CreateUserRequest
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CreateUserRequestValidator{TRequest}"/> class.
    /// </summary>
    protected CreateUserRequestValidator()
    {
        RuleFor(x => x.Username)
            .NotEmpty()
            .WithMessage("Username is required")
            .MinimumLength(3)
            .WithMessage("Username must be at least 3 characters")
            .MaximumLength(100)
            .WithMessage("Username must not exceed 100 characters");

        // An account with no password, no external identity and no key has no way in. The absence
        // is refused here rather than defaulted to a password nobody asked for.
        RuleFor(x => x.SignInMethods)
            .NotNull()
            .WithMessage("At least one sign-in method is required")
            .Must(m => m is not null && (m.Password is not null || m.External is not null || m.ApiKey is not null))
            .WithMessage("At least one sign-in method is required: a password, an external identity, or an API key");

        When(x => x.SignInMethods?.Password is not null, () =>
            ValidatePassword(x => x.SignInMethods.Password!.InitialPassword, minLength: 8));

        When(x => x.SignInMethods?.External is not null, () =>
        {
            RuleFor(x => x.SignInMethods.External!.Issuer)
                .NotEmpty()
                .WithMessage("An external identity needs the issuer that asserts it");

            // An address is not a binding key, but it is an invitation key (FDW-910): exactly one.
            RuleFor(x => x.SignInMethods.External!)
                .Must(e => string.IsNullOrWhiteSpace(e.Subject) != string.IsNullOrWhiteSpace(e.InvitedEmail))
                .WithName("SignInMethods.External.Subject")
                .WithMessage("An external identity needs exactly one of the issuer's subject identifier (bind now) or an invitedEmail (invite)");

            When(x => !string.IsNullOrWhiteSpace(x.SignInMethods.External!.InvitedEmail), () =>
                RuleFor(x => x.SignInMethods.External!.InvitedEmail)
                    .EmailAddress()
                    .WithMessage("invitedEmail must be an email address"));
        });

        When(x => x.SignInMethods?.ApiKey is not null, () =>
        {
            RuleFor(x => x.SignInMethods.ApiKey!.Label)
                .NotEmpty()
                .WithMessage("An API key needs a label");

            RuleFor(x => x.SignInMethods.ApiKey!.Kind)
                .Must(k => string.Equals(k, ApiKeyKinds.User, StringComparison.OrdinalIgnoreCase)
                        || string.Equals(k, ApiKeyKinds.Agent, StringComparison.OrdinalIgnoreCase))
                .WithMessage("API key kind must be 'user' or 'agent'");
        });

        // Stated, not defaulted: there is no role every deployment has, so the caller names them.
        RuleFor(x => x.Roles)
            .NotNull()
            .WithMessage("Roles is required: name the roles the account holds in its tenant, or send [] for none");

        ValidateEmail(x => x.Email);
    }
}
