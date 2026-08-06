using Fdw.Collections.Attributes;

namespace Fdw.Web.Http.Abstractions.Policies;

/// <summary>
/// Token bucket algorithm rate limiting.
/// </summary>
// Why: data-bearing TypeOption; ctor only forwards literal/config data to the base class, no behavior
[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
[TypeOption(typeof(RateLimitPolicies), "TokenBucket", RestrictToCurrentCompilation = true)]
public sealed class TokenBucket : RateLimitPolicyBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="TokenBucket"/> class.
    /// </summary>
    public TokenBucket() : base(
        id: 4,
        name: "TokenBucket",
        maxRequests: 50,
        windowSizeInSeconds: 10,
        policyType: "TokenBucket",
        isEnabled: true,
        defaultRequestLimit: 50,
        defaultTimeWindowSeconds: 10,
        supportsBurstCapacity: true)
    {
    }
}
