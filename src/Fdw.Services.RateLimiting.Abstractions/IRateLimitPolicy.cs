using System;
using Fdw.Collections;

namespace Fdw.Services.RateLimiting.Abstractions;

/// <summary>
/// Interface defining the contract for rate limit policy options.
/// Rate limit policies define request limits, time windows, and throttling behavior
/// for different user tiers or operation types.
/// </summary>
public interface IRateLimitPolicy : ITypeOption<int, IRateLimitPolicy>
{
    /// <summary>
    /// Gets the maximum number of requests allowed within the time window.
    /// </summary>
    int RequestsPerWindow { get; }

    /// <summary>
    /// Gets the duration of the time window for rate limiting.
    /// </summary>
    TimeSpan Window { get; }

    /// <summary>
    /// Gets a value indicating whether burst requests above the normal limit are allowed.
    /// When enabled, temporary spikes up to <see cref="BurstLimit"/> are permitted.
    /// </summary>
    bool AllowBurst { get; }

    /// <summary>
    /// Gets the maximum burst limit when <see cref="AllowBurst"/> is enabled.
    /// This value is only used when burst mode is active.
    /// </summary>
    int BurstLimit { get; }

    /// <summary>
    /// Gets the rate limiting algorithm to use for this policy.
    /// </summary>
    IRateLimitAlgorithm Algorithm { get; }

    /// <summary>
    /// Gets the number of segments per window for sliding window algorithms.
    /// Higher values provide smoother rate limiting but require more memory.
    /// Only applicable when <see cref="Algorithm"/> name is "SlidingWindow".
    /// </summary>
    int SegmentsPerWindow { get; }

    /// <summary>
    /// Gets a value indicating whether requests that exceed the limit should be queued
    /// rather than immediately rejected.
    /// </summary>
    bool QueueExceededRequests { get; }

    /// <summary>
    /// Gets the maximum number of requests that can be queued when limits are exceeded.
    /// Only applicable when <see cref="QueueExceededRequests"/> is enabled.
    /// </summary>
    int QueueLimit { get; }
}
