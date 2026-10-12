using System;

namespace Fdw.Orchestration.Abstractions.Caching;

/// <summary>
/// Options for cache entries.
/// </summary>
public sealed class CacheEntryOptions
{
    /// <summary>
    /// Gets or sets the absolute expiration time.
    /// </summary>
    public DateTimeOffset? AbsoluteExpiration { get; set; }

    /// <summary>
    /// Gets or sets the absolute expiration relative to now.
    /// </summary>
    public TimeSpan? AbsoluteExpirationRelativeToNow { get; set; }

    /// <summary>
    /// Gets or sets the sliding expiration time.
    /// </summary>
    public TimeSpan? SlidingExpiration { get; set; }

    /// <summary>
    /// Gets or sets the cache priority.
    /// </summary>
    public ICachePriority Priority { get; set; } = CachePriorities.Normal;

    /// <summary>
    /// Creates options with absolute expiration.
    /// </summary>
    /// <param name="duration">The expiration duration.</param>
    /// <returns>Cache entry options.</returns>
    public static CacheEntryOptions AbsoluteExpiring(TimeSpan duration) =>
        new() { AbsoluteExpirationRelativeToNow = duration };

    /// <summary>
    /// Creates options with sliding expiration.
    /// </summary>
    /// <param name="duration">The sliding window duration.</param>
    /// <returns>Cache entry options.</returns>
    public static CacheEntryOptions SlidingExpiring(TimeSpan duration) =>
        new() { SlidingExpiration = duration };
}