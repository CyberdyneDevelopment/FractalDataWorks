using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Services.Authentication.Abstractions;
using Fdw.Services.Authentication.Abstractions.Methods;

namespace Fdw.Services.Authentication;

/// <summary>
/// SAML 2.0 authentication protocol.
/// </summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(AuthenticationProtocols), "SAML2", RestrictToCurrentCompilation = true)]
public sealed class SAML2Protocol : AuthenticationProtocolBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SAML2Protocol"/> class.
    /// </summary>
    public SAML2Protocol() : base(
        id: 3,
        name: "SAML2",
        version: "2.0",
        requiresSecureTransport: true,
        supportsTokens: false)
    {
    }
}
