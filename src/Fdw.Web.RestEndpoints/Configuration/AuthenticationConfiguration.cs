using System.Diagnostics.CodeAnalysis;

namespace Fdw.Web.RestEndpoints.Configuration;

/// <summary>
/// Authentication configuration for the web framework.
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class AuthenticationConfiguration
{
    /// <summary>
    /// Gets or sets the JWT configuration.
    /// </summary>
    public JwtConfiguration Jwt { get; init; } = new();

    /// <summary>
    /// Gets or sets the API key configuration.
    /// </summary>
    public ApiKeyConfiguration ApiKey { get; init; } = new();
}