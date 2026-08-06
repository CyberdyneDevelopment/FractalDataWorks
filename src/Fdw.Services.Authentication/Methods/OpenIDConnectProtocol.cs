using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Services.Authentication.Abstractions;
using Fdw.Services.Authentication.Abstractions.Methods;

namespace Fdw.Services.Authentication;

/// <summary>
/// OpenID Connect authentication protocol.
/// </summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(AuthenticationProtocols), "OpenIDConnect", RestrictToCurrentCompilation = true)]
public sealed class OpenIDConnectProtocol : AuthenticationProtocolBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="OpenIDConnectProtocol"/> class.
    /// </summary>
    public OpenIDConnectProtocol() : base(
        id: 2,
        name: "OpenIDConnect",
        version: "1.0",
        requiresSecureTransport: true,
        supportsTokens: true)
    {
    }
}
