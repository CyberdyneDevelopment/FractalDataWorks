namespace Fdw.Services.Authentication.Clients.Models;

/// <summary>
/// Client-side request model for changing the current user's password.
/// </summary>
public sealed class ChangePasswordRequest
{
    /// <summary>
    /// Gets or sets the current password.
    /// </summary>
    public string CurrentPassword { get; set; } = "";

    /// <summary>
    /// Gets or sets the new password.
    /// </summary>
    public string NewPassword { get; set; } = "";
}
