using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Fdw.Services.Calculations.Abstractions.Caching;

/// <summary>
/// Configuration options for calculation result caching.
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class CalculationCacheOptions
{
    /// <summary>
    /// Gets or sets whether caching is enabled.
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Gets or sets the default TTL in minutes for cached results.
    /// </summary>
    public int DefaultTtlMinutes { get; set; } = 60;

    /// <summary>
    /// Gets or sets the maximum TTL in minutes for cached results.
    /// </summary>
    public int MaxTtlMinutes { get; set; } = 1440;

    /// <summary>
    /// Gets or sets TTL overrides by calculation type.
    /// </summary>
    public IDictionary<string, int> TtlByCalculationType { get; set; } = new Dictionary<string, int>(StringComparer.Ordinal);

    /// <summary>
    /// Gets or sets whether to invalidate cache entries when underlying data changes.
    /// </summary>
    public bool InvalidateOnDataChange { get; set; } = true;

    /// <summary>
    /// Gets or sets whether to warm up the cache on startup.
    /// </summary>
    public bool WarmupOnStartup { get; set; }

    /// <summary>
    /// Gets or sets the maximum size in bytes for cached results.
    /// Results larger than this will not be cached.
    /// </summary>
    public int MaxCachedResultSizeBytes { get; set; } = 10 * 1024 * 1024;

    /// <summary>
    /// Gets or sets the cache provider type (Memory, Redis).
    /// </summary>
    public string Provider { get; set; } = "Memory";

    /// <summary>
    /// Gets or sets the cache key prefix.
    /// </summary>
    public string KeyPrefix { get; set; } = "calc:";
}
