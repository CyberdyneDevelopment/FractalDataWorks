using Fdw.Collections;

namespace Fdw.Services.Authentication.Abstractions;

/// <summary>
/// Base class for authentication protocol types.
/// Implements the IAuthenticationProtocol interface with common functionality.
/// </summary>
public abstract class AuthenticationProtocolBase : TypeOptionBase<int, AuthenticationProtocolBase>, IAuthenticationProtocol
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AuthenticationProtocolBase"/> class.
    /// </summary>
    /// <param name="id">The unique identifier for the protocol.</param>
    /// <param name="name">The name of the protocol.</param>
    /// <param name="version">The protocol version or specification identifier.</param>
    /// <param name="requiresSecureTransport">Whether this protocol requires HTTPS.</param>
    /// <param name="supportsTokens">Whether this protocol supports token-based authentication.</param>
    protected AuthenticationProtocolBase(
        int id,
        string name,
        string version,
        bool requiresSecureTransport,
        bool supportsTokens)
        : base(id, name)
    {
        Version = version;
        RequiresSecureTransport = requiresSecureTransport;
        SupportsTokens = supportsTokens;
    }

    /// <inheritdoc/>
    public string Version { get; }

    /// <inheritdoc/>
    public bool RequiresSecureTransport { get; }

    /// <inheritdoc/>
    public bool SupportsTokens { get; }
}
