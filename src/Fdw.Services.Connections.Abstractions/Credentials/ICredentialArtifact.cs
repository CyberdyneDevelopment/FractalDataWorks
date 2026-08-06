namespace Fdw.Services.Connections.Abstractions;

/// <summary>
/// Marker interface for credential artifacts produced by credential translators.
/// </summary>
/// <remarks>
/// <para>
/// Credential artifacts are the typed output of credential translation.
/// They contain connection-specific credentials (connection strings, headers, tokens, etc.)
/// that factories consume to create connections.
/// </para>
/// <para>
/// Factories receive these artifacts and extract the credentials they need.
/// The artifact type is used to validate that the factory receives the correct credential format.
/// </para>
/// <para>
/// Artifacts are data objects created by credential translators - they are NOT TypeOptions.
/// Each translator creates an appropriate artifact instance with the resolved credentials.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Factory receives artifact from provider
/// public IGenericResult&lt;IGenericConnection&gt; Get(
///     ConnectionConfiguration config,
///     ICredentialArtifact credentials)
/// {
///     if (credentials is not OAuthClientArtifact oauthArtifact)
///         return GenericResult&lt;IGenericConnection&gt;.Failure(new GenericMessage("Wrong artifact type"));
///
///     var clientId = oauthArtifact.ClientId;
///     var clientSecret = oauthArtifact.ClientSecret;
///     // ... use credentials
/// }
/// </code>
/// </example>
public interface ICredentialArtifact
{
    /// <summary>
    /// Gets the artifact type name for identification.
    /// </summary>
    /// <remarks>
    /// Common values include "ConnectionString", "HttpHeaders", "OAuthClient", "None".
    /// Used by factories to validate they receive the expected credential format.
    /// </remarks>
    string ArtifactType { get; }
}
