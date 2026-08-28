using Fdw.Collections.Attributes;

namespace Fdw.Web.Http.Abstractions.Policies;

/// <summary>
/// Fixed time window rate limiting.
/// </summary>
[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
[TypeOption(typeof(RateLimitPolicies), "FixedWindow", RestrictToCurrentCompilation = true)]
public sealed class FixedWindow : RateLimitPolicyBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="FixedWindow"/> class.
    /// </summary>
    public FixedWindow() : base(
        id: 2,
        name: "FixedWindow",
        maxRequests: 100,
        windowSizeInSeconds: 60,
        policyType: "FixedWindow",
        isEnabled: true,
        defaultRequestLimit: 100,
        defaultTimeWindowSeconds: 60,
        supportsBurstCapacity: false)
    {
    }
}
