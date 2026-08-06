using System.Diagnostics.CodeAnalysis;

namespace Fdw.Web.RestEndpoints.Configuration;

/// <summary>
/// JWT authentication configuration.
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class JwtConfiguration
{
    /// <summary>
    /// Gets or sets the JWT issuer.
    /// </summary>
    public string Issuer { get; init; } = string.Empty;

    /// <summary>
    /// Gets or sets the JWT audience.
    /// </summary>
    public string Audience { get; init; } = string.Empty;

    /// <summary>
    /// Gets or sets the JWT signing key.
    /// </summary>
    public string SecretKey { get; init; } = string.Empty;

    /// <summary>
    /// Gets or sets the JWT token expiration time in minutes.
    /// </summary>
    public int ExpirationMinutes { get; init; } = 60;
}