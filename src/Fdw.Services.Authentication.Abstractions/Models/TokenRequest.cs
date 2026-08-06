namespace Fdw.Services.Authentication.Clients.Models;

/// <summary>
/// Client-side request model for token generation via username/password authentication.
/// </summary>
public sealed class TokenRequest
{
    /// <summary>
    /// Gets or sets the username.
    /// </summary>
    public string Username { get; set; } = "";

    /// <summary>
    /// Gets or sets the password.
    /// </summary>
    public string Password { get; set; } = "";

    /// <summary>
    /// Gets or sets the optional tenant identifier or slug for multi-tenant login.
    /// </summary>
    public string? Tenant { get; set; }
}
