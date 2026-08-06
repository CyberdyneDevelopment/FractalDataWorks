using System.Diagnostics.CodeAnalysis;
using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Calculations.Logging;

/// <summary>
/// MessageLogging for calculation cache operations.
/// EventId range: 4180-4199
/// </summary>
[ExcludeFromCodeCoverage]
[MessageLoggingTypeCode("CALCULATIONS")]
internal static partial class CalculationCacheLog
{
    // --- TryGet ---

    [MessageLogging(
        EventId = 11000,
        Level = LogLevel.Trace,
        Message = "TryGet starting for calculation '{calculationType}' with {valueCount} values")]
    public static partial IGenericMessage TryGetStarting(
        ILogger logger, string calculationType, int valueCount);

    [MessageLogging(
        EventId = 11001,
        Level = LogLevel.Trace,
        Message = "TryGet generated cache key '{cacheKey}' for calculation '{calculationType}'")]
    public static partial IGenericMessage TryGetKeyGenerated(
        ILogger logger, string cacheKey, string calculationType);

    [MessageLogging(
        EventId = 11002,
        Level = LogLevel.Debug,
        Message = "Cache hit for calculation '{calculationType}' with key '{cacheKey}'")]
    public static partial IGenericMessage CacheHit(
        ILogger logger, string calculationType, string cacheKey);

    [MessageLogging(
        EventId = 11003,
        Level = LogLevel.Debug,
        Message = "Cache miss for calculation '{calculationType}' with key '{cacheKey}'")]
    public static partial IGenericMessage CacheMiss(
        ILogger logger, string calculationType, string cacheKey);

    [MessageLogging(
        EventId = 11004,
        Level = LogLevel.Trace,
        Message = "TryGet skipped, caching is disabled")]
    public static partial IGenericMessage TryGetSkippedDisabled(ILogger logger);

    // --- Set ---

    [MessageLogging(
        EventId = 11005,
        Level = LogLevel.Trace,
        Message = "Set starting for calculation '{calculationType}' with {valueCount} values, result={result}")]
    public static partial IGenericMessage SetStarting(
        ILogger logger, string calculationType, int valueCount, decimal result);

    [MessageLogging(
        EventId = 11006,
        Level = LogLevel.Trace,
        Message = "Set serialized result to {sizeBytes} bytes for key '{cacheKey}'")]
    public static partial IGenericMessage SetSerialized(
        ILogger logger, int sizeBytes, string cacheKey);

    [MessageLogging(
        EventId = 11007,
        Level = LogLevel.Information,
        Message = "Cached calculation result for '{calculationType}', TTL: {ttlMinutes} minutes")]
    public static partial IGenericMessage ResultCached(
        ILogger logger, string calculationType, int ttlMinutes);

    [MessageLogging(
        EventId = 11008,
        Level = LogLevel.Trace,
        Message = "Cache entry set for key '{cacheKey}'")]
    public static partial IGenericMessage CacheEntrySet(
        ILogger logger, string cacheKey);

    [MessageLogging(
        EventId = 11009,
        Level = LogLevel.Trace,
        Message = "Set skipped, caching is disabled")]
    public static partial IGenericMessage SetSkippedDisabled(ILogger logger);

    [MessageLogging(
        EventId = 21000,
        Level = LogLevel.Debug,
        Message = "Result too large to cache ({sizeBytes} bytes > {maxBytes} bytes)")]
    public static partial IGenericMessage ResultTooLarge(
        ILogger logger, long sizeBytes, long maxBytes);

    // --- Invalidate ---

    [MessageLogging(
        EventId = 11010,
        Level = LogLevel.Trace,
        Message = "Invalidate starting for calculation type '{calculationType}'")]
    public static partial IGenericMessage InvalidateStarting(
        ILogger logger, string calculationType);

    [MessageLogging(
        EventId = 11011,
        Level = LogLevel.Information,
        Message = "Invalidated {count} cache entries for '{definitionId}'")]
    public static partial IGenericMessage CacheInvalidated(
        ILogger logger, int count, string definitionId);

    // --- InvalidateByDataSource ---

    [MessageLogging(
        EventId = 11012,
        Level = LogLevel.Trace,
        Message = "InvalidateByDataSource starting for data source '{dataSourceId}'")]
    public static partial IGenericMessage InvalidateByDataSourceStarting(
        ILogger logger, string dataSourceId);

    [MessageLogging(
        EventId = 11013,
        Level = LogLevel.Information,
        Message = "Invalidated {count} cache entries for data source '{dataSourceId}'")]
    public static partial IGenericMessage InvalidateByDataSourceCompleted(
        ILogger logger, int count, string dataSourceId);

    // --- GetStatistics ---

    [MessageLogging(
        EventId = 11014,
        Level = LogLevel.Trace,
        Message = "GetStatistics starting")]
    public static partial IGenericMessage GetStatisticsStarting(ILogger logger);

    [MessageLogging(
        EventId = 11015,
        Level = LogLevel.Information,
        Message = "Cache statistics - Hits: {hits}, Misses: {misses}, Hit Rate: {hitRate:P2}")]
    public static partial IGenericMessage CacheStatisticsRetrieved(
        ILogger logger, long hits, long misses, double hitRate);

    // --- Errors / Warnings ---

    [MessageLogging(
        EventId = 71000,
        Level = LogLevel.Warning,
        Message = "Cache operation failed for key '{cacheKey}': {error}")]
    public static partial IGenericMessage CacheOperationFailed(
        ILogger logger, string cacheKey, string error);

    [MessageLogging(
        EventId = 91000,
        Level = LogLevel.Warning,
        Message = "Failed to deserialize cached result for key '{cacheKey}': {error}")]
    public static partial IGenericMessage CacheDeserializationFailed(
        ILogger logger, string cacheKey, string error);

    [MessageLogging(
        EventId = 71001,
        Level = LogLevel.Warning,
        Message = "Failed to remove cache key '{cacheKey}' during invalidation: {error}")]
    public static partial IGenericMessage InvalidateKeyFailed(
        ILogger logger, string cacheKey, string error);
}
