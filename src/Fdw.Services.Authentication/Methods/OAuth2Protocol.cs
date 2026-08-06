using Fdw.Collections.Attributes;
using Fdw.Services.Authentication.Abstractions;
using Fdw.Services.Authentication.Abstractions.Methods;
using System.Diagnostics.CodeAnalysis;

namespace Fdw.Services.Authentication;

/// <summary>
/// OAuth 2.0 authentication protocol.
/// </summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(AuthenticationProtocols), "OAuth2", RestrictToCurrentCompilation = true)]
public sealed class OAuth2Protocol : AuthenticationProtocolBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="OAuth2Protocol"/> class.
    /// </summary>
    public OAuth2Protocol() : base(
        id: 1,
        name: "OAuth2",
        version: "2.0",
        requiresSecureTransport: true,
        supportsTokens: true)
    {
    }
}
