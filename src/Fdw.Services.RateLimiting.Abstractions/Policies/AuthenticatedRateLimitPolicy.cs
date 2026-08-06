using System;
using Fdw.Collections.Attributes;

namespace Fdw.Services.RateLimiting.Abstractions.Policies;

/// <summary>
/// Rate limit policy for authenticated API users.
/// Provides higher limits than standard access as a reward for user registration
/// and to support typical authenticated application workflows.
/// </summary>
/// <remarks>
/// <para>
/// This policy enables burst mode to accommodate legitimate usage spikes
/// that authenticated applications may experience, such as initial data loading
/// or synchronization operations.
/// </para>
/// <para>
/// The sliding window algorithm ensures accurate rate tracking while the
/// burst capability provides flexibility for authenticated integrations.
/// </para>
/// </remarks>
[TypeOption(typeof(RateLimitPolicies), "Authenticated", RestrictToCurrentCompilation = true)]
public sealed class AuthenticatedRateLimitPolicy : RateLimitPolicyBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AuthenticatedRateLimitPolicy"/> class.
    /// </summary>
    public AuthenticatedRateLimitPolicy() : base(2, "Authenticated")
    {
    }

    /// <summary>
    /// Gets the maximum requests per window (500 requests).
    /// </summary>
    public override int RequestsPerWindow => 500;

    /// <summary>
    /// Gets the rate limit window duration (1 minute).
    /// </summary>
    public override TimeSpan Window => TimeSpan.FromMinutes(1);

    /// <summary>
    /// Gets a value indicating burst mode is enabled for authenticated users.
    /// </summary>
    public override bool AllowBurst => true;

    /// <summary>
    /// Gets the burst limit (750 requests, 1.5x the normal limit).
    /// </summary>
    public override int BurstLimit => 750;

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
