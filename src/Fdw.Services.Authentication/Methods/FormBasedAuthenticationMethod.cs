using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Services.Authentication.Abstractions;
using Fdw.Services.Authentication.Abstractions.Methods;

namespace Fdw.Services.Authentication;

/// <summary>
/// Form-based authentication method.
/// ExtendedEnum that wraps Microsoft's form-based authentication with framework behaviors.
/// </summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(AuthenticationMethods), "FormBased", RestrictToCurrentCompilation = true)]
public sealed class FormBasedAuthenticationMethod : AuthenticationMethodBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="FormBasedAuthenticationMethod"/> class.
    /// </summary>
    public FormBasedAuthenticationMethod() : base(
        id: 2,
        name: "FormBased",
        requiresUserInteraction: true,
        supportsTokenRefresh: false,
        supportsMultiTenant: false,
        authenticationScheme: "Cookies",
        priority: 30)
    {
    }
}