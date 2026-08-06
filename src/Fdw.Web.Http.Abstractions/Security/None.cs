using Fdw.Collections.Attributes;
using System.Diagnostics.CodeAnalysis;

namespace Fdw.Web.Http.Abstractions.Security;

/// <summary>
/// No authentication required.
/// </summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(SecurityMethods), "None", RestrictToCurrentCompilation = true)]
public sealed class None : SecurityMethodBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="None"/> class.
    /// </summary>
    public None() : base(1, "None", requiresAuthentication: false, authenticationScheme: null, supportsTokenRefresh: false) { }
}
