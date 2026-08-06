namespace Fdw.Services.Users.Endpoints;

/// <summary>
/// Request model for resetting a user's password (admin operation).
/// </summary>
public class ResetPasswordRequest : UserScopedRequest
{
    /// <summary>
    /// Gets or sets the new password to set.
    /// </summary>
    public string NewPassword { get; set; } = string.Empty;
}
