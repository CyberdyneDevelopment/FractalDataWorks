using System;
using Microsoft.Extensions.Logging;
using Fdw.Messages;
using Fdw.MessageLogging;

namespace Fdw.Services.Data.Logging;

/// <summary>
/// MessageLogging for DataGateway caching operations.
/// EventId range: 7180-7189
/// </summary>
[MessageLoggingTypeCode("DATA")]
public static partial class DataGatewayCacheLog
{
    /// <summary>Logs a cache hit for a DataGateway query.</summary>
    [MessageLogging(
        EventId = 11027,
        Level = LogLevel.Debug,
        Message = "DataGateway cache hit for key '{cacheKey}'")]
    public static partial IGenericMessage CacheHit(ILogger logger, string cacheKey);

    /// <summary>Logs a cache miss and population for a DataGateway query.</summary>
    [MessageLogging(
        EventId = 11028,
        Level = LogLevel.Debug,
        Message = "DataGateway cache miss for key '{cacheKey}', caching for {cacheDuration}")]
    public static partial IGenericMessage CacheMiss(ILogger logger, string cacheKey, TimeSpan cacheDuration);

    /// <summary>Logs tag-based cache invalidation with eviction count.</summary>
    [MessageLogging(
        EventId = 11029,
        Level = LogLevel.Information,
        Message = "DataGateway cache invalidated tag '{tag}', {evictedCount} keys removed")]
    public static partial IGenericMessage TagInvalidated(ILogger logger, string tag, int evictedCount);

    /// <summary>Logs full cache invalidation with eviction count.</summary>
    [MessageLogging(
        EventId = 11030,
        Level = LogLevel.Information,
        Message = "DataGateway cache invalidated all entries, {evictedCount} keys removed")]
    public static partial IGenericMessage AllInvalidated(ILogger logger, int evictedCount);

    /// <summary>Logs when cache key computation fails for a command.</summary>
    [MessageLogging(
        EventId = 91002,
        Level = LogLevel.Warning,
        Message = "DataGateway cache key computation failed for {commandType} on '{containerName}': {error}")]
    public static partial IGenericMessage KeyComputationFailed(
        ILogger logger,
        string commandType,
        string containerName,
        string error);
}
