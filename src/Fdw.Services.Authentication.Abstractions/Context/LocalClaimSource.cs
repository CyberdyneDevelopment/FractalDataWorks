using Fdw.Collections.Attributes;

namespace Fdw.Services.Authentication.Abstractions.Context;

/// <summary>Read from a store this platform owns. Usable as authorization input.</summary>
[TypeOption(typeof(ClaimSources), "Local", RestrictToCurrentCompilation = true)]
public sealed class LocalClaimSource : ClaimSourceBase
{
    /// <summary>Initializes a new instance of the <see cref="LocalClaimSource"/> class.</summary>
    public LocalClaimSource()
        : base(1, "Local")
    {
    }
}
