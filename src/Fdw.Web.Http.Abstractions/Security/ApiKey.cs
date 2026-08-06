using Fdw.Collections.Attributes;
using System.Diagnostics.CodeAnalysis;

namespace Fdw.Web.Http.Abstractions.Security;

/// <summary>
/// API key-based authentication.
/// </summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(SecurityMethods), "ApiKey", RestrictToCurrentCompilation = true)]
public sealed class ApiKey : SecurityMethodBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ApiKey"/> class.
    /// </summary>
    public ApiKey() : base(3, "ApiKey", requiresAuthentication: true, authenticationScheme: "ApiKey", supportsTokenRefresh: false) { }
}
