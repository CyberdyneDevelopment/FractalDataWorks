using Fdw.Collections;

namespace Fdw.Web.Http.Abstractions.Security;

/// <summary>
/// Interface for security method type options.
/// Provides abstraction for security authentication methods.
/// </summary>
public interface ISecurityMethod : ITypeOption<int, ISecurityMethod>
{
    /// <summary>
    /// Gets a value indicating whether this security method requires authentication.
    /// </summary>
    bool RequiresAuthentication { get; }

    /// <summary>
    /// Gets the authentication scheme name used by this security method.
    /// </summary>
    string? AuthenticationScheme { get; }

    /// <summary>
    /// Gets a value indicating whether this security method supports token refresh.
    /// </summary>
    bool SupportsTokenRefresh { get; }
}
