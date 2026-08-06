using Fdw.Collections.Attributes;
using Fdw.Services.Authentication.Abstractions;
using Fdw.Services.Authentication.Abstractions.Methods;
using System.Diagnostics.CodeAnalysis;

namespace Fdw.Services.Authentication;

/// <summary>
/// OAuth 2.0 authentication method.
/// ExtendedEnum that wraps Microsoft's OAuth2 authentication with framework behaviors.
/// </summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(AuthenticationMethods), "OAuth2", RestrictToCurrentCompilation = true)]
public sealed class OAuth2AuthenticationMethod : AuthenticationMethodBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="OAuth2AuthenticationMethod"/> class.
    /// </summary>
    public OAuth2AuthenticationMethod() : base(
        id: 1,
        name: "OAuth2",
        requiresUserInteraction: true,
        supportsTokenRefresh: true,
        supportsMultiTenant: true,
        authenticationScheme: "Bearer",
        priority: 90)
    {
    }
}