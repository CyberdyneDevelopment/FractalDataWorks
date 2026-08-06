using System;
using Fdw.Collections.Attributes;

namespace Fdw.Services.RateLimiting.Abstractions.Policies;

/// <summary>
/// Rate limit policy for standard unauthenticated API access.
/// Provides conservative limits to protect the API from abuse while allowing
/// reasonable usage for anonymous or unpaid users.
/// </summary>
/// <remarks>
/// <para>
/// This policy uses a sliding window algorithm for accurate rate tracking
/// without the boundary burst issues of fixed window algorithms.
/// </para>
/// <para>
/// Burst mode is disabled for standard users to ensure consistent
/// enforcement of rate limits and prevent resource exhaustion.
/// </para>
/// </remarks>
[TypeOption(typeof(RateLimitPolicies), "Standard", RestrictToCurrentCompilation = true)]
public sealed class StandardRateLimitPolicy : RateLimitPolicyBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="StandardRateLimitPolicy"/> class.
    /// </summary>
    public StandardRateLimitPolicy() : base(1, "Standard")
    {
    }

    /// <summary>
    /// Gets the maximum requests per window (100 requests).
    /// </summary>
    public override int RequestsPerWindow => 100;

    /// <summary>
    /// Gets the rate limit window duration (1 minute).
    /// </summary>
    public override TimeSpan Window => TimeSpan.FromMinutes(1);

    /// <summary>
    /// Gets a value indicating burst mode is disabled for standard users.
    /// </summary>
    public override bool AllowBurst => false;

    /// <summary>
    /// Gets the burst limit (same as regular limit since burst is disabled).
    /// </summary>
    public override int BurstLimit => 100;

    /// <summary>
    /// Gets the rate limiting algorithm (SlidingWindow).
    /// </summary>
    public override IRateLimitAlgorithm Algorithm => RateLimitAlgorithms.SlidingWindow;

    /// <summary>
    /// Gets the number of segments for sliding window (10 segments).
    /// </summary>
    public override int SegmentsPerWindow => 10;

    /// <summary>
    /// Gets a value indicating requests are rejected immediately when limit is exceeded.
    /// </summary>
    public override bool QueueExceededRequests => false;

    /// <summary>
    /// Gets the queue limit (0 since queuing is disabled).
    /// </summary>
    public override int QueueLimit => 0;
}
