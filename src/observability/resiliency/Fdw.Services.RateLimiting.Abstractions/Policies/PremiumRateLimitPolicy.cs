using Fdw.Collections.Attributes;

namespace Fdw.Services.RateLimiting.Abstractions.Policies;

/// <summary>
/// Rate limit policy for premium tier API users.
/// Held to significantly higher limits for paying customers with high-volume API usage requirements.
/// </summary>
/// <remarks>
/// Its numbers are the <c>RateLimit.Premium.*</c> settings, seeded in each host's baseline runtime script.
/// </remarks>
[TypeOption(typeof(RateLimitPolicies), "Premium", RestrictToCurrentCompilation = true)]
public sealed class PremiumRateLimitPolicy : RateLimitPolicyBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="PremiumRateLimitPolicy"/> class.
    /// </summary>
    public PremiumRateLimitPolicy() : base("Premium")
    {
    }
}
