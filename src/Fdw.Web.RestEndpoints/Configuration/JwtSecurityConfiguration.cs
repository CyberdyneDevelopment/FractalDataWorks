using System.Diagnostics.CodeAnalysis;

namespace Fdw.Web.RestEndpoints.Configuration;

/// <summary>
/// JWT-specific security configuration.
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class JwtSecurityConfiguration
{
    /// <summary>
    /// Gets or sets a value indicating whether JWT authentication is enabled.
    /// </summary>
    public bool Enabled { get; init; } = true;

    /// <summary>
    /// Gets or sets the JWT issuer for token validation.
    /// </summary>
    public string Issuer { get; init; } = string.Empty;

    /// <summary>
    /// Gets or sets the JWT audience for token validation.
    /// </summary>
    public string Audience { get; init; } = string.Empty;

    /// <summary>
    /// Gets or sets the secret key for JWT token validation.
    /// In production, this should be stored securely.
    /// </summary>
    public string SecretKey { get; init; } = string.Empty;

    /// <summary>
    /// Gets or sets the allowed clock skew for token validation in seconds.
    /// </summary>
    public int ClockSkewSeconds { get; init; } = 300; // 5 minutes

    /// <summary>
    /// Gets or sets a value indicating whether token lifetimeBase should be validated.
    /// </summary>
    public bool ValidateLifetime { get; init; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether the issuer should be validated.
    /// </summary>
    public bool ValidateIssuer { get; init; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether the audience should be validated.
    /// </summary>
    public bool ValidateAudience { get; init; } = true;

    /// <summary>
    /// Gets or sets the required claims that must be present in the JWT token.
    /// </summary>
    public string[] RequiredClaims { get; init; } = [];
}