using System;
using Fdw.Collections.Attributes;

namespace Fdw.Services.RateLimiting.Abstractions.Policies;

/// <summary>
/// Rate limit policy for premium tier API users.
/// Provides significantly higher limits and advanced features for paying customers
/// with high-volume API usage requirements.
/// </summary>
/// <remarks>
/// <para>
/// This policy uses the token bucket algorithm which provides the best experience
/// for high-volume integrations by allowing controlled bursts while maintaining
/// the average rate limit over time.
/// </para>
/// <para>
/// The generous burst limit (1.5x) accommodates batch operations and data
/// synchronization workflows common in enterprise integrations.
/// </para>
/// </remarks>
[TypeOption(typeof(RateLimitPolicies), "Premium", RestrictToCurrentCompilation = true)]
public sealed class PremiumRateLimitPolicy : RateLimitPolicyBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="PremiumRateLimitPolicy"/> class.
    /// </summary>
    public PremiumRateLimitPolicy() : base(3, "Premium")
    {
    }

    /// <summary>
    /// Gets the maximum requests per window (2000 requests).
    /// </summary>
    public override int RequestsPerWindow => 2000;

    /// <summary>
    /// Gets the rate limit window duration (1 minute).
    /// </summary>
    public override TimeSpan Window => TimeSpan.FromMinutes(1);

    /// <summary>
    /// Gets a value indicating burst mode is enabled for premium users.
    /// </summary>
    public override bool AllowBurst => true;

    /// <summary>
    /// Gets the burst limit (3000 requests, 1.5x the normal limit).
    /// </summary>
    public override int BurstLimit => 3000;

    /// <summary>
    /// Gets the rate limiting algorithm (TokenBucket for optimal burst handling).
    /// </summary>
    public override IRateLimitAlgorithm Algorithm => RateLimitAlgorithms.TokenBucket;

    /// <summary>
    /// Gets the number of segments for sliding window (not applicable for TokenBucket).
    /// </summary>
    public override int SegmentsPerWindow => 1;

    /// <summary>
    /// Gets a value indicating requests are rejected immediately when limit is exceeded.
    /// </summary>
    public override bool QueueExceededRequests => false;

    /// <summary>
    /// Gets the queue limit (0 since queuing is disabled).
    /// </summary>
    public override int QueueLimit => 0;
}
