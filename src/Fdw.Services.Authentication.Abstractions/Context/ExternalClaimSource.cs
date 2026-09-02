using Fdw.Collections.Attributes;

namespace Fdw.Services.Authentication.Abstractions.Context;

/// <summary>Asserted by an external authority. Advisory until an explicit mapping promotes it.</summary>
[TypeOption(typeof(ClaimSources), "External", RestrictToCurrentCompilation = true)]
public sealed class ExternalClaimSource : ClaimSourceBase
{
    /// <summary>Initializes a new instance of the <see cref="ExternalClaimSource"/> class.</summary>
    public ExternalClaimSource()
        : base(2, "External")
    {
    }
}
