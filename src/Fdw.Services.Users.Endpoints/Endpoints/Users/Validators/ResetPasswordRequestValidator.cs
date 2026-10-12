using Fdw.Validation.FastEndpoints;

namespace Fdw.Services.Users.Endpoints.Validators;

/// <summary>
/// Validator for <see cref="ResetPasswordRequest"/>.
/// </summary>
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
