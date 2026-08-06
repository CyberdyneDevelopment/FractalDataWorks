using Fdw.Collections.Attributes;
using Fdw.Services.Authentication.Abstractions;
using Fdw.Services.Authentication.Abstractions.Methods;
using System.Diagnostics.CodeAnalysis;

namespace Fdw.Services.Authentication;

/// <summary>
/// OAuth 2.0 Client Credentials flow.
/// </summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(AuthenticationFlows), "ClientCredentials", RestrictToCurrentCompilation = true)]
public sealed class ClientCredentialsFlow : AuthenticationFlowBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ClientCredentialsFlow"/> class.
    /// </summary>
    public ClientCredentialsFlow() : base(
        id: 2,
        name: "ClientCredentials",
        requiresUserInteraction: false,
        supportsRefreshTokens: false,
        isServerToServer: true)
    {
    }
}
