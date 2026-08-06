namespace Fdw.Services.Authentication.Clients.Models;

/// <summary>
/// Client-side response model for a token refresh containing the new access and refresh tokens.
/// </summary>
public sealed class RefreshTokenResponse
{
    /// <summary>
    /// Gets or sets the new JWT access token.
    /// </summary>
    public string AccessToken { get; set; } = "";

    /// <summary>
    /// Gets or sets the new refresh token.
    /// </summary>
    public string RefreshToken { get; set; } = "";

    /// <summary>
    /// Gets or sets the token type (always "Bearer").
    /// </summary>
    public string TokenType { get; set; } = "Bearer";

    /// <summary>
    /// Gets or sets the token expiration time in seconds.
    /// </summary>
    public int ExpiresIn { get; set; }
}
