using System;

namespace Fdw.Calculations.Contracts.Hubs;

/// <summary>
/// Event raised to broadcast cache statistics.
/// </summary>
public sealed record CacheStatisticsEvent(
    long TotalHits,
    long TotalMisses,
    double HitRate,
    int CachedEntries,
    DateTimeOffset Timestamp);