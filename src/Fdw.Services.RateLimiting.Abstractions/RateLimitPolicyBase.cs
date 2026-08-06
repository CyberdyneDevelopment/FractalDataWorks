using System;
using Fdw.Collections;

namespace Fdw.Services.RateLimiting.Abstractions;

/// <summary>
/// Base class for rate limit policy implementations.
/// Provides the common structure for all rate limit policies including
/// request limits, time windows, and throttling configuration.
/// </summary>
public abstract class RateLimitPolicyBase : TypeOptionBase<int, IRateLimitPolicy>, ITypeOption<int, RateLimitPolicyBase>, IRateLimitPolicy
{
    /// <summary>
    /// Initializes a new instance of the <see cref="RateLimitPolicyBase"/> class.
    /// </summary>
    /// <param name="id">Unique identifier for this policy.</param>
    /// <param name="name">Name of the policy.</param>
    protected RateLimitPolicyBase(int id, string name) : base(id, name)
    {
    }

    /// <summary>
    /// Gets the maximum number of requests allowed within the time window.
    /// </summary>
    public abstract int RequestsPerWindow { get; }

    /// <summary>
    /// Gets the duration of the time window for rate limiting.
    /// </summary>
    public abstract TimeSpan Window { get; }

    /// <summary>
    /// Gets a value indicating whether burst requests above the normal limit are allowed.
    /// When enabled, temporary spikes up to <see cref="BurstLimit"/> are permitted.
    /// </summary>
    public abstract bool AllowBurst { get; }

    /// <summary>
    /// Gets the maximum burst limit when <see cref="AllowBurst"/> is enabled.
    /// This value is only used when burst mode is active.
    /// </summary>
    public abstract int BurstLimit { get; }

    /// <summary>
    /// Gets the rate limiting algorithm to use for this policy.
    /// </summary>
    public abstract IRateLimitAlgorithm Algorithm { get; }

    /// <summary>
    /// Gets the number of segments per window for sliding window algorithms.
    /// Higher values provide smoother rate limiting but require more memory.
    /// Only applicable when <see cref="Algorithm"/> name is "SlidingWindow".
    /// </summary>
    public abstract int SegmentsPerWindow { get; }

    /// <summary>
    /// Gets a value indicating whether requests that exceed the limit should be queued
    /// rather than immediately rejected.
    /// </summary>
    public abstract bool QueueExceededRequests { get; }

    /// <summary>
    /// Gets the maximum number of requests that can be queued when limits are exceeded.
    /// Only applicable when <see cref="QueueExceededRequests"/> is enabled.
    /// </summary>
    public abstract int QueueLimit { get; }
}
