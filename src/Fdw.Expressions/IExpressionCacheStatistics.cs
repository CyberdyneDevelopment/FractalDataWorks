using System;

namespace Fdw.Expressions;

/// <summary>
/// Statistics about expression compilation and caching.
/// </summary>
public interface IExpressionCacheStatistics
{
    /// <summary>
    /// Gets the total number of cached expressions.
    /// </summary>
    int CachedExpressionCount { get; }

    /// <summary>
    /// Gets the number of cache hits.
    /// </summary>
    long CacheHits { get; }

    /// <summary>
    /// Gets the number of cache misses (compilations).
    /// </summary>
    long CacheMisses { get; }

    /// <summary>
    /// Gets the cache hit rate (0.0 to 1.0).
    /// </summary>
    double HitRate { get; }

    /// <summary>
    /// Gets the total time spent compiling expressions.
    /// </summary>
    TimeSpan TotalCompilationTime { get; }

    /// <summary>
    /// Gets the average compilation time per expression.
    /// </summary>
    TimeSpan AverageCompilationTime { get; }
}