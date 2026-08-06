using Fdw.Collections.Attributes;
using Fdw.Services.Authentication.Abstractions;
using Fdw.Services.Authentication.Abstractions.Methods;
using System.Diagnostics.CodeAnalysis;

namespace Fdw.Services.Authentication;

/// <summary>
/// OpenID Connect authentication method.
/// </summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(AuthenticationMethods), "OpenIDConnect", RestrictToCurrentCompilation = true)]
public sealed class OpenIDConnectAuthenticationMethod : AuthenticationMethodBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="OpenIDConnectAuthenticationMethod"/> class.
    /// </summary>
    public OpenIDConnectAuthenticationMethod() : base(
        id: 7,
        name: "OpenIDConnect",
        requiresUserInteraction: true,
        supportsTokenRefresh: true,
        supportsMultiTenant: true,
        authenticationScheme: "Bearer",
        priority: 90)
    {
    }
}
