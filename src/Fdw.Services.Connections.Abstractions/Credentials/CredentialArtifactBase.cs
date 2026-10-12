namespace Fdw.Services.Connections.Abstractions;

/// <summary>
/// Base class for credential artifact implementations.
/// </summary>
/// <remarks>
/// <para>
/// Credential artifacts are produced by credential translators and consumed by connection factories.
/// They provide type-safe credential passing between the provider and factory layers.
/// </para>
/// <para>
/// Derived classes must implement <see cref="ArtifactType"/> to provide the type name
/// used for factory validation.
/// </para>
/// <para>
/// Common implementations include:
/// <list type="bullet">
/// <item><description>HttpHeadersArtifact - for HTTP authentication headers</description></item>
/// <item><description>OAuthClientArtifact - for OAuth client credentials</description></item>
/// <item><description>NoCredentialArtifact - for connections without credentials</description></item>
/// </list>
/// </para>
/// </remarks>
/// <example>
/// <code>
/// public sealed class OAuthClientArtifact : CredentialArtifactBase
/// {
///     public OAuthClientArtifact(string clientId, string clientSecret)
///     {
///         ClientId = clientId;
///         ClientSecret = clientSecret;
///     }
///
///     public override string ArtifactType => "OAuthClient";
///     public string ClientId { get; }
///     public string ClientSecret { get; }
/// }
/// </code>
/// </example>
public abstract class CredentialArtifactBase : ICredentialArtifact
{
    /// <summary>
    /// Gets the artifact type name for identification.
    /// </summary>
    /// <remarks>
    /// This value is used for factory validation to ensure the factory
    /// receives the expected credential format.
    /// </remarks>
    public abstract string ArtifactType { get; }
}
