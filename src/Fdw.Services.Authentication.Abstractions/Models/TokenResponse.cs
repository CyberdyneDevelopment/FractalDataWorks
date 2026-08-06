using System;

namespace Fdw.Services.Authentication.Clients.Models;

/// <summary>
/// Client-side response model for token generation containing access and refresh tokens.
/// </summary>
public sealed class TokenResponse
{
    /// <summary>
    /// Gets or sets the JWT access token.
    /// </summary>
    public string AccessToken { get; set; } = "";

    /// <summary>
    /// Gets or sets the refresh token for obtaining new access tokens.
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

    /// <summary>
    /// Gets or sets the tenant ID included in the token, if any.
    /// </summary>
    public Guid? TenantId { get; set; }
}
