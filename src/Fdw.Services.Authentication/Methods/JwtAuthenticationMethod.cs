using Fdw.Collections.Attributes;
using Fdw.Services.Authentication.Abstractions;
using Fdw.Services.Authentication.Abstractions.Methods;
using System.Diagnostics.CodeAnalysis;

namespace Fdw.Services.Authentication;

/// <summary>
/// JWT (JSON Web Token) authentication method.
/// </summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(AuthenticationMethods), "JWT", RestrictToCurrentCompilation = true)]
public sealed class JwtAuthenticationMethod : AuthenticationMethodBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="JwtAuthenticationMethod"/> class.
    /// </summary>
    public JwtAuthenticationMethod() : base(
        id: 2,
        name: "JWT",
        requiresUserInteraction: false,
        supportsTokenRefresh: false,
        supportsMultiTenant: true,
        authenticationScheme: "Bearer",
        priority: 85)
    {
    }
}
