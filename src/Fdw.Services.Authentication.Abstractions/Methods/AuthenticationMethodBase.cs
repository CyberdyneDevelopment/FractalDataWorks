using Fdw.Collections;

namespace Fdw.Services.Authentication.Abstractions;

/// <summary>
/// Base class for authentication method types.
/// Implements the IAuthenticationMethod interface with common functionality.
/// </summary>
public abstract class AuthenticationMethodBase : TypeOptionBase<int, AuthenticationMethodBase>, IAuthenticationMethod
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AuthenticationMethodBase"/> class.
    /// </summary>
    /// <param name="id">The unique identifier for the authentication method.</param>
    /// <param name="name">The name of the authentication method.</param>
    /// <param name="requiresUserInteraction">Whether this method requires user interaction.</param>
    /// <param name="supportsTokenRefresh">Whether this method supports token refresh.</param>
    /// <param name="supportsMultiTenant">Whether this method supports multi-tenant scenarios.</param>
    /// <param name="authenticationScheme">The authentication scheme to use (optional).</param>
    /// <param name="priority">The priority of this authentication method.</param>
    protected AuthenticationMethodBase(
        int id,
        string name,
        bool requiresUserInteraction,
        bool supportsTokenRefresh,
        bool supportsMultiTenant,
        string? authenticationScheme,
        int priority)
        : base(id, name)
    {
        RequiresUserInteraction = requiresUserInteraction;
        SupportsTokenRefresh = supportsTokenRefresh;
        SupportsMultiTenant = supportsMultiTenant;
        AuthenticationScheme = authenticationScheme;
        Priority = priority;
    }


    /// <inheritdoc/>
    public bool RequiresUserInteraction { get; }

    /// <inheritdoc/>
    public bool SupportsTokenRefresh { get; }

    /// <inheritdoc/>
    public bool SupportsMultiTenant { get; }

    /// <inheritdoc/>
    public string? AuthenticationScheme { get; }

    /// <inheritdoc/>
    public int Priority { get; }
}