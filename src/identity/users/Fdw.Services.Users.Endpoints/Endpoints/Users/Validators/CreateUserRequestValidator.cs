using System;
using System.Linq;
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
        UsernameRules();

        // An account with no password, no external identity and no key has no way in. The absence
        // is refused here rather than defaulted to a password nobody asked for.
        RuleFor(x => x.SignInMethods)
            .NotNull()
            .WithMessage("At least one sign-in method is required")
            .Must(m => m is not null && (m.Password is not null || m.External is not null || m.ApiKey is not null))
            .WithMessage("At least one sign-in method is required: a password, an external identity, or an API key");

        When(x => x.SignInMethods?.Password is not null, () =>
        {
            RuleFor(x => x.SignInMethods.Password!.InitialPassword)
                .NotEmpty()
                .WithMessage("Password is required")
                .MinimumLength(8)
                .WithMessage("Password must be at least 8 characters");
        });

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

        RuleForEach(x => x.Roles)
            .NotEmpty()
            .WithMessage("Role name cannot be empty")
            .MaximumLength(100)
            .WithMessage("Role name cannot exceed 100 characters");

        ValidateEmail(x => x.Email);
    }

    // The characters an email-form username may carry; see UsernameRules.
    private static bool EmailFormCarriesOnlySafeCharacters(string username)
        => username.All(c => char.IsAsciiLetterOrDigit(c) || c is '.' or '_' or '-' or '+' or '@');

    // What a username may be: an identifier, or an email address for an account that signs in through an identity provider (FDW-1056).
    // The two forms are told apart by the '@', and each says what is wrong with it. The email form is also held to characters every
    // consumer of the name can carry: the name is a URL path segment (/users/{IdOrName}), a VARCHAR key, and the left half of the
    // '{username}:{tenantId}' membership name, so no ':' '/' '%' or non-ASCII.
    private void UsernameRules()
    {
        RuleFor(x => x.Username)
            .NotEmpty()
            .WithMessage("Username is required")
            .MinimumLength(3)
            .WithMessage("Username must be at least 3 characters")
            .MaximumLength(100)
            .WithMessage("Username cannot exceed 100 characters");

        When(x => !string.IsNullOrEmpty(x.Username) && !x.Username.Contains('@', StringComparison.Ordinal), () =>
            RuleFor(x => x.Username)
                .Matches(@"^[a-zA-Z][a-zA-Z0-9_]*$")
                .WithMessage("Username must start with a letter and contain only letters, numbers, or underscores, or be an email address"));

        When(x => !string.IsNullOrEmpty(x.Username) && x.Username.Contains('@', StringComparison.Ordinal), () =>
            RuleFor(x => x.Username)
                .EmailAddress()
                .WithMessage("A username containing '@' must be a valid email address")
                .Must(EmailFormCarriesOnlySafeCharacters)
                .WithMessage("An email address used as a username may contain only ASCII letters, numbers and the characters . _ - + @"));
    }
}
