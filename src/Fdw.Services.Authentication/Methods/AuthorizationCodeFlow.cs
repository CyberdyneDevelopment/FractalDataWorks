using Fdw.Collections.Attributes;
using Fdw.Services.Authentication.Abstractions;
using Fdw.Services.Authentication.Abstractions.Methods;
using System.Diagnostics.CodeAnalysis;

namespace Fdw.Services.Authentication;

/// <summary>
/// OAuth 2.0 Authorization Code flow.
/// </summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(AuthenticationFlows), "AuthorizationCode", RestrictToCurrentCompilation = true)]
public sealed class AuthorizationCodeFlow : AuthenticationFlowBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AuthorizationCodeFlow"/> class.
    /// </summary>
    public AuthorizationCodeFlow() : base(
        id: 1,
        name: "AuthorizationCode",
        requiresUserInteraction: true,
        supportsRefreshTokens: true,
        isServerToServer: false)
    {
    }
}
