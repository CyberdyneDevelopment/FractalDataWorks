using System;
using Microsoft.Extensions.Logging;
using Fdw.Messages;
using Fdw.MessageLogging;

namespace Fdw.Web.Analytics.Components.Health.Logging;

/// <summary>
/// MessageLogging methods for GaugeProvider operations.
/// EventId range: 8900-8909
/// </summary>
[MessageLoggingTypeCode("COMPONENTS19")]
public static partial class GaugeProviderLog
{
    /// <summary>Logs when gauge data load starts.</summary>
    [MessageLogging(EventId = 11000, Level = LogLevel.Trace,
        Message = "[GaugeProvider] Loading gauge data for '{serviceName}'")]
    public static partial IGenericMessage LoadStarted(
        ILogger logger,
        string serviceName);

    /// <summary>Logs when gauge data load completes.</summary>
    [MessageLogging(EventId = 11001, Level = LogLevel.Debug,
        Message = "[GaugeProvider] Gauge data loaded for '{serviceName}': value={value}")]
    public static partial IGenericMessage LoadCompleted(
        ILogger logger,
        string serviceName,
        double value);

    /// <summary>Logs when gauge data load fails.</summary>
    [MessageLogging(EventId = 71000, Level = LogLevel.Warning,
        Message = "[GaugeProvider] Failed to load gauge data for '{serviceName}'")]
    public static partial IGenericMessage LoadFailed(
        ILogger logger,
        string serviceName);

    /// <summary>Logs when gauge data load fails with exception.</summary>
    [MessageLogging(EventId = 71001, Level = LogLevel.Warning,
        Message = "[GaugeProvider] Exception loading gauge data for '{serviceName}'")]
    public static partial IGenericMessage LoadException(
        ILogger logger,
        Exception exception,
        string serviceName);
}
