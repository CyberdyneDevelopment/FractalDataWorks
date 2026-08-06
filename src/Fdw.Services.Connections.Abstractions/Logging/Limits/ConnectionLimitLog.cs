using System;
using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Connections.Abstractions.Logging;

/// <summary>
/// MessageLogging methods for connection limit enforcement operations.
/// EventId range: 7268-7285
/// </summary>
[MessageLoggingTypeCode("ABSTRACTIONS6")]
public static partial class ConnectionLimitLog
{
    /// <summary>
    /// Logs when an outbound query is rejected because the rate limit is exceeded.
    /// </summary>
    [MessageLogging(EventId = 81000, Level = LogLevel.Warning,
        Message = "Rate limit exceeded on connection '{connectionId}' — {currentRate} req/s exceeds cap of {maxPerSecond}")]
    public static partial IGenericMessage RateExceeded(
        ILogger logger,
        Guid connectionId,
        double currentRate,
        int maxPerSecond);

    /// <summary>
    /// Logs when a query is cancelled because it exceeded the configured timeout.
    /// </summary>
    [MessageLogging(EventId = 81001, Level = LogLevel.Warning,
        Message = "Query timeout exceeded on connection '{connectionId}' — limit is {timeoutSeconds}s")]
    public static partial IGenericMessage QueryTimeoutExceeded(
        ILogger logger,
        Guid connectionId,
        int timeoutSeconds);

    /// <summary>
    /// Logs when a query is rejected because the result size would exceed the configured cap.
    /// </summary>
    [MessageLogging(EventId = 81002, Level = LogLevel.Warning,
        Message = "Max result size exceeded on connection '{connectionId}' — requested {requestedRows} rows exceeds cap of {maxRows}")]
    public static partial IGenericMessage MaxResultSizeExceeded(
        ILogger logger,
        Guid connectionId,
        int requestedRows,
        int maxRows);

    /// <summary>
    /// Logs when a query is rejected because the concurrency semaphore is at capacity.
    /// </summary>
    [MessageLogging(EventId = 81003, Level = LogLevel.Warning,
        Message = "Concurrency limit reached on connection '{connectionId}' — {currentCount} concurrent queries at cap of {maxConcurrent}")]
    public static partial IGenericMessage ConcurrencyBlocked(
        ILogger logger,
        Guid connectionId,
        int currentCount,
        int maxConcurrent);

    /// <summary>
    /// Logs when a query is rejected because the daily query budget is exhausted.
    /// </summary>
    [MessageLogging(EventId = 81004, Level = LogLevel.Warning,
        Message = "Daily query budget exhausted on connection '{connectionId}' — {queriesUsed} of {maxQueriesPerDay} queries used today")]
    public static partial IGenericMessage DailyQueryBudgetExhausted(
        ILogger logger,
        Guid connectionId,
        long queriesUsed,
        int maxQueriesPerDay);

    /// <summary>
    /// Logs when a query is rejected because the daily byte budget is exhausted.
    /// </summary>
    [MessageLogging(EventId = 81005, Level = LogLevel.Warning,
        Message = "Daily byte budget exhausted on connection '{connectionId}' — {bytesUsed} of {maxBytesPerDay} bytes used today")]
    public static partial IGenericMessage DailyByteBudgetExhausted(
        ILogger logger,
        Guid connectionId,
        long bytesUsed,
        long maxBytesPerDay);

    /// <summary>
    /// Logs when limit resolution fails for a connection (e.g., limit config record missing).
    /// </summary>
    [MessageLogging(EventId = 61001, Level = LogLevel.Error,
        Message = "Failed to resolve effective limits for connection '{connectionId}': {reason}")]
    public static partial IGenericMessage LimitResolutionFailed(
        ILogger logger,
        Guid connectionId,
        string reason);

    /// <summary>
    /// Logs when a limit check is skipped because no limits are configured.
    /// </summary>
    [MessageLogging(EventId = 11003, Level = LogLevel.Trace,
        Message = "No limits configured for connection '{connectionId}' — skipping enforcement")]
    public static partial IGenericMessage NoLimitsConfigured(
        ILogger logger,
        Guid connectionId);

    /// <summary>
    /// Logs when the daily limit counter is reset by the nightly job.
    /// </summary>
    [MessageLogging(EventId = 11004, Level = LogLevel.Information,
        Message = "Daily limit counters reset for {connectionCount} connections")]
    public static partial IGenericMessage DailyCountersReset(
        ILogger logger,
        int connectionCount);

    /// <summary>
    /// Logs when the daily-limit reset job's delay is cancelled by host shutdown (a clean, expected exit).
    /// </summary>
    [MessageLogging(EventId = 11005, Level = LogLevel.Debug,
        Message = "Daily limit reset job cancelled during host shutdown")]
    public static partial IGenericMessage ResetJobCancelledDuringShutdown(
        ILogger logger,
        Exception ex);

    /// <summary>
    /// Logs when daily counter persistence to the DB fails.
    /// </summary>
    [MessageLogging(EventId = 71001, Level = LogLevel.Error,
        Message = "Failed to persist daily limit counter for connection '{connectionId}'")]
    public static partial IGenericMessage DailyCounterPersistFailed(
        ILogger logger,
        Exception ex,
        Guid connectionId);

    /// <summary>
    /// Logs when an operation is rejected because the calling code cancelled it.
    /// </summary>
    [MessageLogging(EventId = 11006, Level = LogLevel.Debug,
        Message = "Operation on connection '{connectionId}' was cancelled by the caller before limit enforcement")]
    public static partial IGenericMessage OperationCancelled(
        ILogger logger,
        Guid connectionId);

    /// <summary>
    /// Logs when a query timed out and no timeout limit was resolvable for context.
    /// </summary>
    [MessageLogging(EventId = 81006, Level = LogLevel.Warning,
        Message = "Query timeout exceeded on connection '{connectionId}' (no configured limit resolved for details)")]
    public static partial IGenericMessage QueryTimeoutExceededNoLimit(
        ILogger logger,
        Guid connectionId);
}
