using System;
using Microsoft.Extensions.Logging;
using Fdw.Messages;
using Fdw.MessageLogging;

namespace Fdw.Web.Analytics.Components.Health.Logging;

/// <summary>
/// MessageLogging methods for SparklineProvider operations.
/// EventId range: 8910-8919
/// </summary>
[MessageLoggingTypeCode("COMPONENTS19")]
public static partial class SparklineProviderLog
{
    /// <summary>Logs when sparkline data load starts.</summary>
    [MessageLogging(EventId = 11004, Level = LogLevel.Trace,
        Message = "[SparklineProvider] Loading sparkline data for '{serviceName}' over {windowSeconds}s")]
    public static partial IGenericMessage LoadStarted(
        ILogger logger,
        string serviceName,
        double windowSeconds);

    /// <summary>Logs when sparkline data load completes.</summary>
    [MessageLogging(EventId = 11005, Level = LogLevel.Debug,
        Message = "[SparklineProvider] Sparkline data loaded for '{serviceName}': {dataPointCount} point(s)")]
    public static partial IGenericMessage LoadCompleted(
        ILogger logger,
        string serviceName,
        int dataPointCount);

    /// <summary>Logs when sparkline data load fails.</summary>
    [MessageLogging(EventId = 71004, Level = LogLevel.Warning,
        Message = "[SparklineProvider] Failed to load sparkline data for '{serviceName}'")]
    public static partial IGenericMessage LoadFailed(
        ILogger logger,
        string serviceName);

    /// <summary>Logs when sparkline data load fails with exception.</summary>
    [MessageLogging(EventId = 71005, Level = LogLevel.Warning,
        Message = "[SparklineProvider] Exception loading sparkline data for '{serviceName}'")]
    public static partial IGenericMessage LoadException(
        ILogger logger,
        Exception exception,
        string serviceName);
}
