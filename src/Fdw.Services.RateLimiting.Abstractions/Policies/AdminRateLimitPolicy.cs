using System;
using Fdw.Collections.Attributes;

namespace Fdw.Services.RateLimiting.Abstractions.Policies;

/// <summary>
/// Rate limit policy for administrative API access.
/// Provides the highest limits and most permissive settings for internal tools,
/// administrative dashboards, and system integration services.
/// </summary>
/// <remarks>
/// <para>
/// This policy is designed for trusted internal applications that require
/// high-volume API access for administrative operations such as bulk data
/// management, system monitoring, and internal tooling.
/// </para>
/// <para>
/// The token bucket algorithm with generous burst limits ensures administrative
/// operations can complete efficiently without being throttled during critical
/// maintenance windows or batch processing.
/// </para>
/// </remarks>
[TypeOption(typeof(RateLimitPolicies), "Admin", RestrictToCurrentCompilation = true)]
public sealed class AdminRateLimitPolicy : RateLimitPolicyBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AdminRateLimitPolicy"/> class.
    /// </summary>
    public AdminRateLimitPolicy() : base(4, "Admin")
    {
    }

    /// <summary>
    /// Gets the maximum requests per window (10000 requests).
    /// </summary>
    public override int RequestsPerWindow => 10000;

    /// <summary>
    /// Gets the rate limit window duration (1 minute).
    /// </summary>
    public override TimeSpan Window => TimeSpan.FromMinutes(1);

    /// <summary>
    /// Gets a value indicating burst mode is enabled for admin users.
    /// </summary>
    public override bool AllowBurst => true;

    /// <summary>
    /// Gets the burst limit (15000 requests, 1.5x the normal limit).
    /// </summary>
    public override int BurstLimit => 15000;

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
