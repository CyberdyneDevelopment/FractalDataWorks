namespace Fdw.Services.Users.Endpoints;

/// <summary>
/// Request to give an account its first password.
/// </summary>
public class SetPasswordLoginRequest : UserScopedRequest
{
    /// <summary>
    /// Gets or sets the initial password. It is always marked must-change, so the account's owner
    /// replaces it at their first sign-in.
    /// </summary>
    public string InitialPassword { get; set; } = string.Empty;
}
