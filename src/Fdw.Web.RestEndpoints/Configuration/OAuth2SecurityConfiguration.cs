using System.Diagnostics.CodeAnalysis;

namespace Fdw.Web.RestEndpoints.Configuration;

/// <summary>
/// OAuth2-specific security configuration.
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class OAuth2SecurityConfiguration
{
    /// <summary>
    /// Gets or sets a value indicating whether OAuth2 authentication is enabled.
    /// </summary>
    public bool Enabled { get; init; }

    /// <summary>
    /// Gets or sets the OAuth2 authority URL.
    /// </summary>
    public string Authority { get; init; } = string.Empty;

    /// <summary>
    /// Gets or sets the client ID for OAuth2.
    /// </summary>
    public string ClientId { get; init; } = string.Empty;

    /// <summary>
    /// Gets or sets the client secret for OAuth2.
    /// </summary>
    public string ClientSecret { get; init; } = string.Empty;

    /// <summary>
    /// Gets or sets the required scopes for OAuth2 authentication.
    /// </summary>
    public string[] RequiredScopes { get; init; } = [];
}