using Fdw.Collections.Attributes;
using System.Diagnostics.CodeAnalysis;

namespace Fdw.Web.Http.Abstractions.Security;

/// <summary>
/// OAuth 2.0 authentication.
/// </summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(SecurityMethods), "OAuth2", RestrictToCurrentCompilation = true)]
public sealed class OAuth2 : SecurityMethodBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="OAuth2"/> class.
    /// </summary>
    public OAuth2() : base(4, "OAuth2", requiresAuthentication: true, authenticationScheme: "Bearer", supportsTokenRefresh: true) { }
}
