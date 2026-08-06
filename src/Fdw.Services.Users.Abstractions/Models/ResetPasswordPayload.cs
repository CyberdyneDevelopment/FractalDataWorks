namespace Fdw.Services.Users.Clients.Models;

/// <summary>
/// Request body for an administrative password reset.
/// </summary>
public sealed class ResetPasswordPayload
{
    /// <summary>
    /// Gets or sets the new password to set for the target user.
    /// </summary>
    public string NewPassword { get; set; } = string.Empty;
}
