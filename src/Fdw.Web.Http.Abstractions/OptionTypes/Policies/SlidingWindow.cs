using Fdw.Collections.Attributes;

namespace Fdw.Web.Http.Abstractions.Policies;

/// <summary>
/// Sliding time window rate limiting.
/// </summary>
// Why: data-bearing TypeOption; ctor only forwards literal/config data to the base class, no behavior
[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
[TypeOption(typeof(RateLimitPolicies), "SlidingWindow", RestrictToCurrentCompilation = true)]
public sealed class SlidingWindow : RateLimitPolicyBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SlidingWindow"/> class.
    /// </summary>
    public SlidingWindow() : base(
        id: 3,
        name: "SlidingWindow",
        maxRequests: 150,
        windowSizeInSeconds: 60,
        policyType: "SlidingWindow",
        isEnabled: true,
        defaultRequestLimit: 150,
        defaultTimeWindowSeconds: 60,
        supportsBurstCapacity: true)
    {
    }
}
