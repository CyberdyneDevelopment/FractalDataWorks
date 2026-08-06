using System;
using System.Diagnostics.CodeAnalysis;
using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace Fdw.Web.Analytics.Clients.Logging;

/// <summary>
/// MessageLogging for analytics service operations.
/// EventId range: 4160-4179
/// </summary>
[ExcludeFromCodeCoverage]
[MessageLoggingTypeCode("ANALYTICSCLIENTS")]
public static partial class AnalyticsLog
{
    // ── Trace (method entry/exit) ────────────────────────────────────────

    /// <summary>Logs entry into RecordExecution for the specified calculation type.</summary>
    [MessageLogging(EventId = 11000, Level = LogLevel.Trace,
        Message = "RecordExecution: entering for '{calculationType}'")]
    public static partial IGenericMessage RecordExecutionEntering(ILogger logger, string calculationType);

    /// <summary>Logs completion of RecordExecution for the specified calculation type.</summary>
    [MessageLogging(EventId = 11001, Level = LogLevel.Trace,
        Message = "RecordExecution: completed for '{calculationType}'")]
    public static partial IGenericMessage RecordExecutionCompleted(ILogger logger, string calculationType);

    /// <summary>Logs entry into GetAnalytics for the specified date range.</summary>
    [MessageLogging(EventId = 11002, Level = LogLevel.Trace,
        Message = "GetAnalytics: entering for period {startDate} to {endDate}")]
    public static partial IGenericMessage GetAnalyticsEntering(ILogger logger, string startDate, string endDate);

    /// <summary>Logs completion of GetAnalytics with the number of entries returned.</summary>
    [MessageLogging(EventId = 11003, Level = LogLevel.Trace,
        Message = "GetAnalytics: completed with {entryCount} entries")]
    public static partial IGenericMessage GetAnalyticsCompleted(ILogger logger, int entryCount);

    /// <summary>Logs entry into GetTopCalculations with the requested count.</summary>
    [MessageLogging(EventId = 11004, Level = LogLevel.Trace,
        Message = "GetTopCalculations: entering with count={count}")]
    public static partial IGenericMessage GetTopCalculationsEntering(ILogger logger, int count);

    /// <summary>Logs completion of GetTopCalculations with the number of results returned.</summary>
    [MessageLogging(EventId = 11005, Level = LogLevel.Trace,
        Message = "GetTopCalculations: completed with {resultCount} results")]
    public static partial IGenericMessage GetTopCalculationsCompleted(ILogger logger, int resultCount);

    // ── Debug (operational detail) ───────────────────────────────────────

    /// <summary>Logs calculation performance with type and duration.</summary>
    [MessageLogging(EventId = 11006, Level = LogLevel.Debug,
        Message = "Calculation performance recorded: {calculationType} took {durationMs}ms")]
    public static partial IGenericMessage PerformanceRecorded(ILogger logger, string calculationType, long durationMs);

    /// <summary>Logs the calculated error rate for a specific calculation type.</summary>
    [MessageLogging(EventId = 11007, Level = LogLevel.Debug,
        Message = "Error rate for '{calculationType}': {errorRate:P2} ({errorCount} / {totalCount})")]
    public static partial IGenericMessage ErrorRateCalculated(ILogger logger, string calculationType, double errorRate, int errorCount, int totalCount);

    // ── Information (business events) ────────────────────────────────────

    /// <summary>Logs successful retrieval of analytics for a date range.</summary>
    [MessageLogging(EventId = 11008, Level = LogLevel.Information,
        Message = "Retrieved analytics for period {startDate} to {endDate}")]
    public static partial IGenericMessage AnalyticsRetrieved(ILogger logger, string startDate, string endDate);

    /// <summary>Logs a usage summary with total executions, unique types, and average duration.</summary>
    [MessageLogging(EventId = 11009, Level = LogLevel.Information,
        Message = "Usage summary - Total: {totalExecutions}, Unique Types: {uniqueTypes}, Avg Duration: {avgDurationMs}ms")]
    public static partial IGenericMessage UsageSummary(ILogger logger, long totalExecutions, int uniqueTypes, double avgDurationMs);

    /// <summary>Logs a cache performance report with hit rate and average response time.</summary>
    [MessageLogging(EventId = 11010, Level = LogLevel.Information,
        Message = "Cache performance report - Hit Rate: {hitRate:P2}, Avg Response Time: {avgMs}ms")]
    public static partial IGenericMessage CachePerformanceReport(ILogger logger, double hitRate, double avgMs);

    // ── Warning (expected/recoverable exceptions) ────────────────────────

    /// <summary>Logs a warning when analytics recording fails.</summary>
    [MessageLogging(EventId = 91000, Level = LogLevel.Warning,
        Message = "Analytics recording failed: {error}")]
    public static partial IGenericMessage RecordingFailed(ILogger logger, string error);

    /// <summary>Logs a warning for an invalid argument in an analytics operation.</summary>
    [MessageLogging(EventId = 21000, Level = LogLevel.Warning,
        Message = "Invalid argument in analytics operation '{operation}': {error}")]
    public static partial IGenericMessage InvalidArgument(ILogger logger, Exception exception, string operation, string error);

    /// <summary>Logs a warning for an invalid operation in analytics.</summary>
    [MessageLogging(EventId = 41000, Level = LogLevel.Warning,
        Message = "Invalid operation in analytics '{operation}': {error}")]
    public static partial IGenericMessage InvalidOperation(ILogger logger, Exception exception, string operation, string error);

    /// <summary>Logs a warning for an arithmetic overflow in an analytics operation.</summary>
    [MessageLogging(EventId = 91001, Level = LogLevel.Warning,
        Message = "Arithmetic overflow in analytics '{operation}': {error}")]
    public static partial IGenericMessage ArithmeticOverflow(ILogger logger, Exception exception, string operation, string error);

    // ── Error (unexpected exceptions) ────────────────────────────────────

    /// <summary>Logs an error when RecordExecution fails unexpectedly.</summary>
    [MessageLogging(EventId = 91002, Level = LogLevel.Error,
        Message = "RecordExecution failed for '{calculationType}': {error}")]
    public static partial IGenericMessage RecordExecutionFailed(ILogger logger, Exception exception, string calculationType, string error);

    /// <summary>Logs an error when GetAnalytics fails unexpectedly.</summary>
    [MessageLogging(EventId = 91003, Level = LogLevel.Error,
        Message = "GetAnalytics failed for period {startDate} to {endDate}: {error}")]
    public static partial IGenericMessage GetAnalyticsFailed(ILogger logger, Exception exception, string startDate, string endDate, string error);

    /// <summary>Logs an error when GetTopCalculations fails unexpectedly.</summary>
    [MessageLogging(EventId = 91004, Level = LogLevel.Error,
        Message = "GetTopCalculations failed: {error}")]
    public static partial IGenericMessage GetTopCalculationsFailed(ILogger logger, Exception exception, string error);

    // ── Critical ─────────────────────────────────────────────────────────

    /// <summary>Logs a critical alert when the analytics service enters a corrupted state.</summary>
    [MessageLogging(EventId = 91005, Level = LogLevel.Critical,
        Message = "Analytics service is in a corrupted state: {reason}")]
    public static partial IGenericMessage ServiceCorrupted(ILogger logger, string reason);
}
