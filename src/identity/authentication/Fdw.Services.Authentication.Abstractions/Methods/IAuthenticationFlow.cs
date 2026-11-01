using Fdw.Collections;

namespace Fdw.Services.Authentication.Abstractions;

/// <summary>
/// Interface for authentication flow types.
/// Defines the contract for authentication flows supported by the framework.
/// </summary>
public interface IAuthenticationFlow : ITypeOption<int>
{
    /// <summary>
    /// Gets whether this flow requires user interaction.
    /// </summary>
    bool RequiresUserInteraction { get; }

    /// <summary>
    /// Gets whether this flow supports refresh tokens.
    /// </summary>
    bool SupportsRefreshTokens { get; }

    /// <summary>
    /// Gets whether this flow is suitable for server-to-server communication.
    /// </summary>
    bool IsServerToServer { get; }
}
