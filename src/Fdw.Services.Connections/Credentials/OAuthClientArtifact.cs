using System;
using System.Diagnostics.CodeAnalysis;
using Fdw.Services.Connections.Abstractions;

namespace Fdw.Services.Connections;

/// <summary>
/// Credential artifact containing OAuth client credentials.
/// </summary>
/// <remarks>
/// <para>
/// This artifact is used for OAuth 2.0 client credentials flow authentication.
/// The factory uses these credentials to obtain access tokens from the token endpoint.
/// </para>
/// <para>
/// Typical usage flow:
/// <list type="number">
/// <item><description>Credential translator resolves client secret from secret manager</description></item>
/// <item><description>Translator creates OAuthClientArtifact with client ID, secret, and endpoint</description></item>
/// <item><description>Factory uses credentials to request access token</description></item>
/// <item><description>Factory adds token to Authorization header for API requests</description></item>
/// </list>
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Created by credential translator
/// var artifact = new OAuthClientArtifact(
///     clientId: "my-client-id",
///     clientSecret: "resolved-secret",
///     tokenEndpoint: "https://auth.example.com/oauth/token",
///     scope: "api.read api.write");
///
/// // Used by factory
/// if (credentials is OAuthClientArtifact oauthArtifact)
/// {
///     var token = await tokenClient.GetToken(
///         oauthArtifact.TokenEndpoint,
///         oauthArtifact.ClientId,
///         oauthArtifact.ClientSecret,
///         oauthArtifact.Scope);
/// }
/// </code>
/// </example>
[ExcludeFromCodeCoverage]
public sealed class OAuthClientArtifact : CredentialArtifactBase
{
    /// <summary>
    /// The artifact type name.
    /// </summary>
    public const string TypeName = "OAuthClient";

    /// <summary>
    /// Initializes a new instance of the <see cref="OAuthClientArtifact"/> class.
    /// </summary>
    /// <param name="clientId">The OAuth client identifier.</param>
    /// <param name="clientSecret">The OAuth client secret.</param>
    /// <param name="tokenEndpoint">The OAuth token endpoint URL. Optional if token endpoint is configured elsewhere.</param>
    /// <param name="scope">The OAuth scope to request. Optional.</param>
    /// <exception cref="ArgumentNullException">Thrown when clientId or clientSecret is null.</exception>
    /// <exception cref="ArgumentException">Thrown when clientId or clientSecret is empty or whitespace.</exception>
    public OAuthClientArtifact(
        string clientId,
        string clientSecret,
        string? tokenEndpoint = null,
        string? scope = null)
    {
        if (clientId == null)
            throw new ArgumentNullException(nameof(clientId));
        if (string.IsNullOrWhiteSpace(clientId))
            throw new ArgumentException("Client ID cannot be empty or whitespace.", nameof(clientId));

        if (clientSecret == null)
            throw new ArgumentNullException(nameof(clientSecret));
        if (string.IsNullOrWhiteSpace(clientSecret))
            throw new ArgumentException("Client secret cannot be empty or whitespace.", nameof(clientSecret));

        ClientId = clientId;
        ClientSecret = clientSecret;
        TokenEndpoint = tokenEndpoint;
        Scope = scope;
    }

    /// <inheritdoc/>
    public override string ArtifactType => TypeName;

    /// <summary>
    /// Gets the OAuth client identifier.
    /// </summary>
    public string ClientId { get; }

    /// <summary>
    /// Gets the OAuth client secret.
    /// </summary>
    /// <remarks>
    /// This is sensitive information and should not be logged or exposed.
    /// </remarks>
    public string ClientSecret { get; }

    /// <summary>
    /// Gets the OAuth token endpoint URL.
    /// </summary>
    /// <remarks>
    /// This may be null if the token endpoint is configured elsewhere
    /// (e.g., in the connection configuration or discovered via metadata).
    /// </remarks>
    public string? TokenEndpoint { get; }

    /// <summary>
    /// Gets the OAuth scope to request.
    /// </summary>
    /// <remarks>
    /// Scopes are space-separated per OAuth 2.0 specification.
    /// This may be null if no specific scope is required.
    /// </remarks>
    public string? Scope { get; }
}
