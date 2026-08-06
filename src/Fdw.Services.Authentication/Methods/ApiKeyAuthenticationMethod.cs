using Fdw.Collections.Attributes;
using Fdw.Services.Authentication.Abstractions;
using Fdw.Services.Authentication.Abstractions.Methods;
using System.Diagnostics.CodeAnalysis;

namespace Fdw.Services.Authentication;

/// <summary>
/// API Key authentication method.
/// </summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(AuthenticationMethods), "ApiKey", RestrictToCurrentCompilation = true)]
public sealed class ApiKeyAuthenticationMethod : AuthenticationMethodBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ApiKeyAuthenticationMethod"/> class.
    /// </summary>
    public ApiKeyAuthenticationMethod() : base(
        id: 4,
        name: "ApiKey",
        requiresUserInteraction: false,
        supportsTokenRefresh: false,
        supportsMultiTenant: false,
        authenticationScheme: "ApiKey",
        priority: 70)
    {
    }
}
