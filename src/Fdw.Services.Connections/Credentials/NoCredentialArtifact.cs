using System.Diagnostics.CodeAnalysis;
using Fdw.Services.Connections.Abstractions;

namespace Fdw.Services.Connections;

/// <summary>
/// Credential artifact indicating no credentials are required.
/// </summary>
/// <remarks>
/// <para>
/// This artifact is used for connections that do not require credentials:
/// <list type="bullet">
/// <item><description>Windows Integrated Authentication</description></item>
/// <item><description>Azure Managed Identity</description></item>
/// <item><description>Public API endpoints</description></item>
/// <item><description>Local file system access</description></item>
/// </list>
/// </para>
/// <para>
/// This is a singleton class - use <see cref="Instance"/> to get the shared instance.
/// Creating multiple instances is unnecessary since the artifact carries no data.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Created by credential translator for Windows Auth
/// return GenericResult&lt;ICredentialArtifact&gt;.Success(NoCredentialArtifact.Instance);
///
/// // Factory checks artifact type
/// if (credentials is NoCredentialArtifact)
/// {
///     // Use Windows Auth / Managed Identity
///     connectionString = $"Server={config.Server};Database={config.Database};Integrated Security=true;";
/// }
/// </code>
/// </example>
[ExcludeFromCodeCoverage]
public sealed class NoCredentialArtifact : CredentialArtifactBase
{
    /// <summary>
    /// The artifact type name.
    /// </summary>
    public const string TypeName = "None";

    /// <summary>
    /// Gets the singleton instance of <see cref="NoCredentialArtifact"/>.
    /// </summary>
    /// <remarks>
    /// Use this property instead of creating new instances.
    /// The artifact carries no data, so a single instance is sufficient.
    /// </remarks>
    public static NoCredentialArtifact Instance { get; } = new NoCredentialArtifact();

    /// <summary>
    /// Initializes a new instance of the <see cref="NoCredentialArtifact"/> class.
    /// </summary>
    /// <remarks>
    /// Prefer using <see cref="Instance"/> instead of creating new instances.
    /// </remarks>
    private NoCredentialArtifact()
    {
    }

    /// <inheritdoc/>
    public override string ArtifactType => TypeName;
}
