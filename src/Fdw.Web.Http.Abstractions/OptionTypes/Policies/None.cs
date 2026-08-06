using Fdw.Collections.Attributes;

namespace Fdw.Web.Http.Abstractions.Policies;

/// <summary>
/// No rate limiting applied.
/// </summary>
// Why: data-bearing TypeOption; ctor only forwards literal/config data to the base class, no behavior
[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
[TypeOption(typeof(RateLimitPolicies), "None", RestrictToCurrentCompilation = true)]
public sealed class None : RateLimitPolicyBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="None"/> class.
    /// </summary>
    public None() : base(
        id: 1,
        name: "None",
        maxRequests: int.MaxValue,
        windowSizeInSeconds: 0,
        policyType: "None",
        isEnabled: false,
        defaultRequestLimit: null,
        defaultTimeWindowSeconds: null,
        supportsBurstCapacity: false)
    {
    }
}
