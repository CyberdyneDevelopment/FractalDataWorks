namespace Fdw.Web.Clients.Wasm.Logging;

using System;
using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

/// <summary>
/// MessageLogging for browser cache operations.
/// EventId range: 4486-4499
/// </summary>
[MessageLoggingTypeCode("WASM")]
public static partial class BrowserCacheLog
{
    /// <summary>
    /// Logged at Trace level when a cache get operation is attempted.
    /// </summary>
    [MessageLogging(
        EventId = 11000,
        Level = LogLevel.Trace,
        Message = "Cache get attempt for key '{cacheKey}'")]
    public static partial IGenericMessage CacheGetAttempt(
        ILogger logger,
        string cacheKey);

    /// <summary>
    /// Logged at Trace level when a cache hit occurs.
    /// </summary>
    [MessageLogging(
        EventId = 11001,
        Level = LogLevel.Trace,
        Message = "Cache hit for key '{cacheKey}'")]
    public static partial IGenericMessage CacheHit(
        ILogger logger,
        string cacheKey);

    /// <summary>
    /// Logged at Trace level when a cache miss occurs.
    /// </summary>
    [MessageLogging(
        EventId = 11002,
        Level = LogLevel.Trace,
        Message = "Cache miss for key '{cacheKey}'")]
    public static partial IGenericMessage CacheMiss(
        ILogger logger,
        string cacheKey);

    /// <summary>
    /// Logged at Trace level when a cache set operation is attempted.
    /// </summary>
    [MessageLogging(
        EventId = 11003,
        Level = LogLevel.Trace,
        Message = "Cache set attempt for key '{cacheKey}'")]
    public static partial IGenericMessage CacheSetAttempt(
        ILogger logger,
        string cacheKey);

    /// <summary>
    /// Logged at Trace level when a cache set operation completes.
    /// </summary>
    [MessageLogging(
        EventId = 11004,
        Level = LogLevel.Trace,
        Message = "Cache set completed for key '{cacheKey}'")]
    public static partial IGenericMessage CacheSetCompleted(
        ILogger logger,
        string cacheKey);

    /// <summary>
    /// Logged at Debug level when a cache invalidate operation is attempted.
    /// </summary>
    [MessageLogging(
        EventId = 11005,
        Level = LogLevel.Debug,
        Message = "Cache invalidate for key '{cacheKey}'")]
    public static partial IGenericMessage CacheInvalidateAttempt(
        ILogger logger,
        string cacheKey);

    /// <summary>
    /// Logged at Debug level when a cache invalidate operation completes.
    /// </summary>
    [MessageLogging(
        EventId = 11006,
        Level = LogLevel.Debug,
        Message = "Cache invalidate completed for key '{cacheKey}'")]
    public static partial IGenericMessage CacheInvalidateCompleted(
        ILogger logger,
        string cacheKey);

    /// <summary>
    /// Logged at Information level when the entire cache is cleared.
    /// </summary>
    [MessageLogging(
        EventId = 11007,
        Level = LogLevel.Information,
        Message = "Cache clear requested")]
    public static partial IGenericMessage CacheClearAttempt(
        ILogger logger);

    /// <summary>
    /// Logged at Information level when the cache clear operation completes.
    /// </summary>
    [MessageLogging(
        EventId = 11008,
        Level = LogLevel.Information,
        Message = "Cache cleared")]
    public static partial IGenericMessage CacheClearCompleted(
        ILogger logger);

    /// <summary>
    /// Logged at Warning level when a cache operation fails due to a JS interop error.
    /// </summary>
    [MessageLogging(
        EventId = 71000,
        Level = LogLevel.Warning,
        Message = "Cache operation '{operation}' failed for key '{cacheKey}'")]
    public static partial IGenericMessage CacheOperationFailed(
        ILogger logger,
        Exception ex,
        string operation,
        string cacheKey);

    /// <summary>
    /// Logged at Warning level when cache deserialization fails.
    /// </summary>
    [MessageLogging(
        EventId = 91000,
        Level = LogLevel.Warning,
        Message = "Cache deserialization failed for key '{cacheKey}'")]
    public static partial IGenericMessage CacheDeserializationFailed(
        ILogger logger,
        string cacheKey);
}
