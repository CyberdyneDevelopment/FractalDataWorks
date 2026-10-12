using System;
using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Connections.Logging;

/// <summary>
/// MessageLogging for <see cref="ConnectionHealthMonitorWorker"/> operations.
/// </summary>
[MessageLoggingTypeCode("CONNECTIONS")]
public static partial class ConnectionHealthMonitorWorkerLog
{
    /// <summary>Logs that the periodic connection health monitor worker has started.</summary>
    [MessageLogging(EventId = 12200, Level = LogLevel.Information, Message = "Connection health monitor worker started")]
    public static partial IGenericMessage WorkerStarted(ILogger logger);

    /// <summary>Logs that the periodic connection health monitor worker is stopping (graceful shutdown).</summary>
    [MessageLogging(EventId = 12201, Level = LogLevel.Information, Message = "Connection health monitor worker stopping")]
    public static partial IGenericMessage WorkerStopping(ILogger logger);

    /// <summary>Logs that loading connection configurations for a health check collect failed.</summary>
    [MessageLogging(EventId = 12202, Level = LogLevel.Error, Message = "Failed to load connection configurations during the health monitor pass: {reason}")]
    public static partial IGenericMessage LoadConnectionsFailed(ILogger logger, string reason);

    /// <summary>Logs that a specific connection is about to be probed.</summary>
    [MessageLogging(EventId = 12203, Level = LogLevel.Trace, Message = "Probing connection '{connectionName}' during the health monitor pass")]
    public static partial IGenericMessage ProbingConnection(ILogger logger, string connectionName);

    /// <summary>Logs that resolving a connection through IConnectionProvider failed during the collect.</summary>
    [MessageLogging(EventId = 12204, Level = LogLevel.Error, Message = "Failed to resolve connection '{connectionName}' during the health monitor pass: {reason}")]
    public static partial IGenericMessage ConnectionResolutionFailed(ILogger logger, string connectionName, string reason);

    /// <summary>Logs that a connection has health checks enabled but its type does not support probing.</summary>
    [MessageLogging(EventId = 12205, Level = LogLevel.Debug, Message = "Connection '{connectionName}' has health checks enabled but its connection type does not support health probing — skipping")]
    public static partial IGenericMessage NoProbeCapability(ILogger logger, string connectionName);

    /// <summary>Logs that a connection's health probe failed.</summary>
    [MessageLogging(EventId = 12206, Level = LogLevel.Error, Message = "Health probe failed for connection '{connectionName}': {reason}")]
    public static partial IGenericMessage ProbeFailed(ILogger logger, string connectionName, string reason);

    /// <summary>Logs that persisting the health check history record failed.</summary>
    [MessageLogging(EventId = 12208, Level = LogLevel.Error, Message = "Failed to persist health check history for connection '{connectionName}': {reason}")]
    public static partial IGenericMessage PersistHistoryFailed(ILogger logger, string connectionName, string reason);

    /// <summary>Logs a transition FROM unhealthy TO healthy for a connection (recovery).</summary>
    [MessageLogging(EventId = 12209, Level = LogLevel.Information, Message = "Connection '{connectionName}' health state changed: now {isHealthy}")]
    public static partial IGenericMessage HealthStateChanged(ILogger logger, string connectionName, bool isHealthy);

    /// <summary>Logs a transition FROM healthy (or unknown) TO unhealthy for a connection.</summary>
    [MessageLogging(EventId = 12213, Level = LogLevel.Error, Message = "Connection '{connectionName}' health state changed: now {isHealthy}")]
    public static partial IGenericMessage HealthStateChangedUnhealthy(ILogger logger, string connectionName, bool isHealthy);

    /// <summary>Logs how many connections are due for an on-startup probe.</summary>
    [MessageLogging(EventId = 12210, Level = LogLevel.Trace, Message = "{count} connection(s) due for on-startup health probe")]
    public static partial IGenericMessage StartupProbesEvaluating(ILogger logger, int count);

    /// <summary>Logs how many connections are due for a periodic probe on the current scan tick.</summary>
    [MessageLogging(EventId = 12211, Level = LogLevel.Trace, Message = "{count} connection(s) due for periodic health probe on this scan tick")]
    public static partial IGenericMessage ScheduledProbesEvaluating(ILogger logger, int count);

    /// <summary>Logs when the periodic scan loop is cancelled by host shutdown (a clean, expected exit).</summary>
    [MessageLogging(EventId = 12212, Level = LogLevel.Debug, Message = "Connection health monitor worker's scan loop cancelled during host shutdown")]
    public static partial IGenericMessage WorkerCancelledDuringShutdown(ILogger logger, Exception ex);

    /// <summary>Logs that this host's configuration store registers no connection container, so the monitor is idle.</summary>
    [MessageLogging(EventId = 12214, Level = LogLevel.Information, Message = "No connection container in this host's configuration store — connection health monitoring idle")]
    public static partial IGenericMessage MonitoringIdleNoConnectionContainer(ILogger logger);

    /// <summary>Logs that probing a connection threw instead of answering; the connection is recorded unhealthy and the pass goes on.</summary>
    [MessageLogging(EventId = 12215, Level = LogLevel.Error, Message = "Health probe for connection '{connectionName}' threw instead of answering; recorded unhealthy, the pass continues: {reason}")]
    public static partial IGenericMessage ProbeThrew(ILogger logger, Exception ex, string connectionName, string reason);

    /// <summary>Logs that persisting a connection's health check threw; the pass goes on to the next connection.</summary>
    [MessageLogging(EventId = 12216, Level = LogLevel.Error, Message = "Persisting the health check for connection '{connectionName}' threw; the pass continues: {reason}")]
    public static partial IGenericMessage PersistHistoryThrew(ILogger logger, Exception ex, string connectionName, string reason);

    /// <summary>Logs how long the monitor waits before its next check: until the soonest enabled connection falls due, within its scan bounds.</summary>
    // Why 10001: the catalog's Informational, a non-error outcome, which is what this is.
    // Why Debug: a per-scan outcome, like the count of due connections it follows; nothing an operator acts on.
    [MessageLogging(EventId = 10001, Level = LogLevel.Debug, Message = "Next connection health scan in {seconds} second(s)")]
    public static partial IGenericMessage NextScanScheduled(ILogger logger, double seconds);
}
