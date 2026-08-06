using Fdw.Collections.Attributes;
using System.Diagnostics.CodeAnalysis;

namespace Fdw.Web.Http.Abstractions.Security;

/// <summary>
/// Client certificate authentication.
/// </summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(SecurityMethods), "Certificate", RestrictToCurrentCompilation = true)]
public sealed class Certificate : SecurityMethodBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="Certificate"/> class.
    /// </summary>
    public Certificate() : base(5, "Certificate", requiresAuthentication: true, authenticationScheme: "Certificate", supportsTokenRefresh: false) { }
}
