using Fdw.Collections;

namespace Fdw.Services.Authentication.Abstractions;

/// <summary>
/// Base class for authentication flow types.
/// Implements the IAuthenticationFlow interface with common functionality.
/// </summary>
public abstract class AuthenticationFlowBase : TypeOptionBase<AuthenticationFlowBase>, IAuthenticationFlow
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AuthenticationFlowBase"/> class.
    /// </summary>
    /// <param name="name">The name of the flow.</param>
    /// <param name="requiresUserInteraction">Whether this flow requires user interaction.</param>
    /// <param name="supportsRefreshTokens">Whether this flow supports refresh tokens.</param>
    /// <param name="isServerToServer">Whether this flow is for server-to-server communication.</param>
    protected AuthenticationFlowBase(
        string name,
        bool requiresUserInteraction,
        bool supportsRefreshTokens,
        bool isServerToServer)
        : base(name)
    {
        RequiresUserInteraction = requiresUserInteraction;
        SupportsRefreshTokens = supportsRefreshTokens;
        IsServerToServer = isServerToServer;
    }

    /// <inheritdoc/>
    public bool RequiresUserInteraction { get; }

    /// <inheritdoc/>
    public bool SupportsRefreshTokens { get; }

    /// <inheritdoc/>
    public bool IsServerToServer { get; }
}
