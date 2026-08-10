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
            .MaximumLength(50)
            .WithMessage("Username must not exceed 50 characters");

        ValidatePassword(x => x.Password, minLength: 8);

        ValidateEmail(x => x.Email);
    }
}
