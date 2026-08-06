using Fdw.Collections.Attributes;
using Fdw.Services.Authentication.Abstractions;
using Fdw.Services.Authentication.Abstractions.Methods;
using System.Diagnostics.CodeAnalysis;

namespace Fdw.Services.Authentication;

/// <summary>
/// Bearer token authentication method.
/// </summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(AuthenticationMethods), "BearerToken", RestrictToCurrentCompilation = true)]
public sealed class BearerTokenAuthenticationMethod : AuthenticationMethodBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="BearerTokenAuthenticationMethod"/> class.
    /// </summary>
    public BearerTokenAuthenticationMethod() : base(
        id: 3,
        name: "BearerToken",
        requiresUserInteraction: false,
        supportsTokenRefresh: true,
        supportsMultiTenant: true,
        authenticationScheme: "Bearer",
        priority: 80)
    {
    }
}
