using Fdw.Collections.Attributes;

namespace Fdw.Services.Authentication.Abstractions.Context;

/// <summary>Derived by a step from other context. Usable, and only as good as that step.</summary>
[TypeOption(typeof(ClaimSources), "Derived", RestrictToCurrentCompilation = true)]
public sealed class DerivedClaimSource : ClaimSourceBase
{
    /// <summary>Initializes a new instance of the <see cref="DerivedClaimSource"/> class.</summary>
    public DerivedClaimSource()
        : base(3, "Derived")
    {
    }
}
