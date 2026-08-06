using Fdw.Collections.Attributes;
using Fdw.Services.Authentication.Abstractions;
using Fdw.Services.Authentication.Abstractions.Methods;
using System.Diagnostics.CodeAnalysis;

namespace Fdw.Services.Authentication;

/// <summary>
/// Certificate-based authentication method.
/// </summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(AuthenticationMethods), "Certificate", RestrictToCurrentCompilation = true)]
public sealed class CertificateAuthenticationMethod : AuthenticationMethodBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CertificateAuthenticationMethod"/> class.
    /// </summary>
    public CertificateAuthenticationMethod() : base(
        id: 5,
        name: "Certificate",
        requiresUserInteraction: false,
        supportsTokenRefresh: false,
        supportsMultiTenant: true,
        authenticationScheme: "Certificate",
        priority: 95)
    {
    }
}
