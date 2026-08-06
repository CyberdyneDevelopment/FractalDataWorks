namespace Fdw.Services.Authentication.Clients.Models;

/// <summary>
/// Client-side request model for refreshing an access token using a refresh token.
/// </summary>
public sealed class RefreshTokenRequest
{
    /// <summary>
    /// Gets or sets the refresh token.
    /// </summary>
    public string RefreshToken { get; set; } = "";
}
