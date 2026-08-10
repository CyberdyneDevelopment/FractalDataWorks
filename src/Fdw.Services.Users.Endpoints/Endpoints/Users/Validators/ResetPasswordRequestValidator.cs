using Fdw.Validation.FastEndpoints;

namespace Fdw.Services.Users.Endpoints.Validators;

/// <summary>
/// Validator for <see cref="ResetPasswordRequest"/>.
/// </summary>
// Why: only CreateUserRequest enforced a password rule, so an administrative reset accepted a
// one-character password — a weaker bar than creating the same account. The reset path gets the
// same minimum as create.
public abstract class ResetPasswordRequestValidator<TRequest> : FdwEndpointValidator<TRequest>
    where TRequest : ResetPasswordRequest
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ResetPasswordRequestValidator{TRequest}"/> class.
    /// </summary>
    protected ResetPasswordRequestValidator()
    {
        ValidatePassword(x => x.NewPassword, minLength: 8);
    }
}
