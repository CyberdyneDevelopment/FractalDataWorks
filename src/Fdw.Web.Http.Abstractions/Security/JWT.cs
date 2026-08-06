using Fdw.Collections.Attributes;
using System.Diagnostics.CodeAnalysis;

namespace Fdw.Web.Http.Abstractions.Security;

/// <summary>
/// JSON Web Token authentication.
/// </summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(SecurityMethods), "JWT", RestrictToCurrentCompilation = true)]
public sealed class JWT : SecurityMethodBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="JWT"/> class.
    /// </summary>
    public JWT() : base(2, "JWT", requiresAuthentication: true, authenticationScheme: "Bearer", supportsTokenRefresh: true) { }
}
