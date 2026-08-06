using System;

namespace Fdw.Expressions;

/// <summary>
/// Expression cache statistics implementation.
/// </summary>
internal sealed class ExpressionCacheStatistics : IExpressionCacheStatistics
{
    /// <inheritdoc/>
    public int CachedExpressionCount { get; }

    /// <inheritdoc/>
    public long CacheHits { get; }

    /// <inheritdoc/>
    public long CacheMisses { get; }

    /// <inheritdoc/>
    public double HitRate =>
        (CacheHits + CacheMisses) > 0
            ? (double)CacheHits / (CacheHits + CacheMisses)
            : 0;

    /// <inheritdoc/>
    public TimeSpan TotalCompilationTime { get; }

    /// <inheritdoc/>
    public TimeSpan AverageCompilationTime =>
        CacheMisses > 0
            ? TimeSpan.FromTicks(TotalCompilationTime.Ticks / CacheMisses)
            : TimeSpan.Zero;

    /// <summary>
    /// Initializes a new instance of the <see cref="ExpressionCacheStatistics"/> class.
    /// </summary>
    public ExpressionCacheStatistics(
        long hits,
        long misses,
        int count,
        TimeSpan compilationTime)
    {
        CacheHits = hits;
        CacheMisses = misses;
        CachedExpressionCount = count;
        TotalCompilationTime = compilationTime;
    }

    /// <inheritdoc/>
    public override string ToString() =>
        $"Cache: {CachedExpressionCount} expressions, " +
        $"{HitRate:P1} hit rate, " +
        $"{TotalCompilationTime.TotalMilliseconds:F2}ms compilation time";
}