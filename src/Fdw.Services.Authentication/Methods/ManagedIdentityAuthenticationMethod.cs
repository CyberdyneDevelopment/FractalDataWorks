using Fdw.Collections.Attributes;
using Fdw.Services.Authentication.Abstractions;
using Fdw.Services.Authentication.Abstractions.Methods;
using System.Diagnostics.CodeAnalysis;

namespace Fdw.Services.Authentication;

/// <summary>
/// Managed Identity authentication method for Azure resources.
/// </summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(AuthenticationMethods), "ManagedIdentity", RestrictToCurrentCompilation = true)]
public sealed class ManagedIdentityAuthenticationMethod : AuthenticationMethodBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ManagedIdentityAuthenticationMethod"/> class.
    /// </summary>
    public ManagedIdentityAuthenticationMethod() : base(
        id: 6,
        name: "ManagedIdentity",
        requiresUserInteraction: false,
        supportsTokenRefresh: true,
        supportsMultiTenant: true,
        authenticationScheme: "Bearer",
        priority: 100)
    {
    }
}
