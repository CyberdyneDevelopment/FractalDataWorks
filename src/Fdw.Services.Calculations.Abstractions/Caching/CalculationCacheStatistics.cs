using System.Diagnostics.CodeAnalysis;

namespace Fdw.Services.Calculations.Abstractions.Caching;

/// <summary>
/// Statistics about calculation cache performance.
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class CalculationCacheStatistics
{
    /// <summary>
    /// Gets or sets the total number of cache hits.
    /// </summary>
    public long TotalHits { get; set; }

    /// <summary>
    /// Gets or sets the total number of cache misses.
    /// </summary>
    public long TotalMisses { get; set; }

    /// <summary>
    /// Gets the cache hit rate.
    /// </summary>
    public double HitRate => TotalHits + TotalMisses > 0
        ? (double)TotalHits / (TotalHits + TotalMisses)
        : 0;

    /// <summary>
    /// Gets or sets the number of cached entries.
    /// </summary>
    public int CachedEntries { get; set; }

    /// <summary>
    /// Gets or sets the total size of cached data in bytes.
    /// </summary>
    public long TotalSizeBytes { get; set; }
}
