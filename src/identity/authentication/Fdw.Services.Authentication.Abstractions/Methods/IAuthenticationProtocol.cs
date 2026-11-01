using Fdw.Collections;

namespace Fdw.Services.Authentication.Abstractions;

/// <summary>
/// Interface for authentication protocol types.
/// Defines the contract for authentication protocols supported by the framework.
/// </summary>
public interface IAuthenticationProtocol : ITypeOption<int>
{
    /// <summary>
    /// Gets the protocol version or specification identifier.
    /// </summary>
    string Version { get; }

    /// <summary>
    /// Gets whether this protocol requires secure transport (HTTPS).
    /// </summary>
    bool RequiresSecureTransport { get; }

    /// <summary>
    /// Gets whether this protocol supports token-based authentication.
    /// </summary>
    bool SupportsTokens { get; }
}
