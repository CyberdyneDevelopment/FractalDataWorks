using System;
using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Connections.Logging;

/// <summary>
/// MessageLogging for <see cref="ConnectionHealthMonitorWorker"/> operations.
/// EventId range: 12200-12215.
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

    /// <summary>Logs that loading connection configurations for a health check sweep failed.</summary>
    [MessageLogging(EventId = 12202, Level = LogLevel.Error, Message = "Failed to load connection configurations for health monitor sweep: {reason}")]
    public static partial IGenericMessage LoadConnectionsFailed(ILogger logger, string reason);

    /// <summary>Logs that a specific connection is about to be probed.</summary>
    [MessageLogging(EventId = 12203, Level = LogLevel.Trace, Message = "Probing connection '{connectionName}' for health monitor sweep")]
    public static partial IGenericMessage ProbingConnection(ILogger logger, string connectionName);

    /// <summary>Logs that resolving a connection through IConnectionProvider failed during the sweep.</summary>
    [MessageLogging(EventId = 12204, Level = LogLevel.Error, Message = "Failed to resolve connection '{connectionName}' for health monitor sweep: {reason}")]
    public static partial IGenericMessage ConnectionResolutionFailed(ILogger logger, string connectionName, string reason);

    /// <summary>Logs that a connection has health checks enabled but its type does not support probing.</summary>
    // Why Debug, not Warning (FDW-583): whether a connection type implements ISupportsHealthProbe is a
    // permanent static property of that type — this fires on every sweep forever and can never be
    // "fixed" at runtime, so it is not an actionable warning.
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
    // Why Error, not Information (FDW-583): a transition to unhealthy means the connection just went
    // down — the operation cannot complete, unlike the recovery transition above.
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
    // Why Information, not Error, and emitted exactly once: whether the store registers the connection
    // container is a STRUCTURAL property of THIS host's configurationSchema.json — the IDataStore tree is
    // built once through a Lazy, so a container absent at startup cannot appear later in the process. A
    // host that manages zero connections by design (the normal shape for a FileSystem-gateway client whose
    // only connection is the bootstrap one in configurationSchema.json) is not an incident, and a store
    // truthfully answering "this container does not exist here" is not a defect. Reporting it every scan
    // tick at Error produced thousands of identical LoadConnectionsFailed pairs a day, burying real
    // errors. Fail-loud is for defects; this is neither transient nor a defect, so the worker states the
    // condition once and stops rather than restating it forever.
    [MessageLogging(EventId = 12214, Level = LogLevel.Information, Message = "No connection container in this host's configuration store — connection health monitoring idle")]
    public static partial IGenericMessage MonitoringIdleNoConnectionContainer(ILogger logger);
}
