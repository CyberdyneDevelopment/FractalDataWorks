using System;
using Microsoft.Extensions.Logging;
using Fdw.Messages;
using Fdw.MessageLogging;

namespace Fdw.Web.Analytics.Components.Health.Logging;

/// <summary>
/// MessageLogging methods for ThroughputProvider operations.
/// EventId range: 8920-8929
/// </summary>
[MessageLoggingTypeCode("COMPONENTS19")]
public static partial class ThroughputProviderLog
{
    /// <summary>Logs when throughput data load starts.</summary>
    [MessageLogging(EventId = 11006, Level = LogLevel.Trace,
        Message = "[ThroughputProvider] Loading throughput data for '{serviceName}' over {windowSeconds}s")]
    public static partial IGenericMessage LoadStarted(
        ILogger logger,
        string serviceName,
        double windowSeconds);

    /// <summary>Logs when throughput data load completes.</summary>
    [MessageLogging(EventId = 11007, Level = LogLevel.Debug,
        Message = "[ThroughputProvider] Throughput data loaded for '{serviceName}': {requestsPerSecond} req/s, avg {avgLatencyMs}ms")]
    public static partial IGenericMessage LoadCompleted(
        ILogger logger,
        string serviceName,
        double requestsPerSecond,
        double avgLatencyMs);

    /// <summary>Logs when throughput data load fails.</summary>
    [MessageLogging(EventId = 71006, Level = LogLevel.Warning,
        Message = "[ThroughputProvider] Failed to load throughput data for '{serviceName}'")]
    public static partial IGenericMessage LoadFailed(
        ILogger logger,
        string serviceName);

    /// <summary>Logs when throughput data load fails with exception.</summary>
    [MessageLogging(EventId = 71007, Level = LogLevel.Warning,
        Message = "[ThroughputProvider] Exception loading throughput data for '{serviceName}'")]
    public static partial IGenericMessage LoadException(
        ILogger logger,
        Exception exception,
        string serviceName);
}
