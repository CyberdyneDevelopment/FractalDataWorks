using Fdw.Collections;

namespace Fdw.Web.Http.Abstractions.Security;

/// <summary>
/// Enhanced enum defining security authentication methods supported by the framework.
/// Provides extensible authentication options with framework integration.
/// </summary>
public abstract class SecurityMethodBase : TypeOptionBase<ISecurityMethod>, ISecurityMethod
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SecurityMethodBase"/> class.
    /// </summary>
    /// <param name="name">The name of the security method.</param>
    /// <param name="requiresAuthentication">Whether this method requires authentication.</param>
    /// <param name="authenticationScheme">The authentication scheme to use (optional).</param>
    /// <param name="supportsTokenRefresh">Whether this method supports token refresh.</param>
    protected SecurityMethodBase(string name, bool requiresAuthentication, string? authenticationScheme, bool supportsTokenRefresh)
        : base(name)
    {
        RequiresAuthentication = requiresAuthentication;
        AuthenticationScheme = authenticationScheme;
        SupportsTokenRefresh = supportsTokenRefresh;
    }

    /// <inheritdoc/>
    public bool RequiresAuthentication { get; }

    /// <inheritdoc/>
    public string? AuthenticationScheme { get; }

    /// <inheritdoc/>
    public bool SupportsTokenRefresh { get; }
}
