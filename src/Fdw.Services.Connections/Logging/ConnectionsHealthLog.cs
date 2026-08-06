using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Connections.Logging;

/// <summary>
/// MessageLogging for <see cref="ConnectionsHealthCheckable"/> operations.
/// EventId range: 12000-12010.
/// </summary>
[MessageLoggingTypeCode("CONNECTIONS")]
public static partial class ConnectionsHealthLog
{
    /// <summary>
    /// Logs that the Connections domain health check is starting.
    /// </summary>
    [MessageLogging(EventId = 12000, Level = LogLevel.Trace, Message = "Connections domain health check starting")]
    public static partial IGenericMessage CheckStarting(ILogger logger);

    /// <summary>
    /// Logs that the Connections domain health check completed with a Healthy overall status, with the
    /// number of connections probed.
    /// </summary>
    [MessageLogging(EventId = 12001, Level = LogLevel.Information, Message = "Connections domain health check completed: {connectionCount} connection(s) checked, overall status {overallStatus}")]
    public static partial IGenericMessage CheckCompleted(ILogger logger, int connectionCount, string overallStatus);

    /// <summary>
    /// Logs that the Connections domain health check completed with a non-Healthy overall status
    /// (e.g. every connection down), with the number of connections probed.
    /// </summary>
    // Why Error, not Information (FDW-583): CheckHealth returns Success(Unhealthy) — the monitor's
    // success branch — so this was printed as an Information record even when every connection is down.
    [MessageLogging(EventId = 12007, Level = LogLevel.Error, Message = "Connections domain health check completed: {connectionCount} connection(s) checked, overall status {overallStatus}")]
    public static partial IGenericMessage CheckCompletedUnhealthy(ILogger logger, int connectionCount, string overallStatus);

    /// <summary>
    /// Logs that no connection rows have health checks enabled, so the domain reports healthy
    /// with nothing to probe.
    /// </summary>
    [MessageLogging(EventId = 12002, Level = LogLevel.Information, Message = "No connection health checks are enabled — reporting healthy")]
    public static partial IGenericMessage NoConnectionsEnabled(ILogger logger);

    /// <summary>
    /// Logs that loading the connection configurations for the health check failed entirely.
    /// </summary>
    [MessageLogging(EventId = 12003, Level = LogLevel.Error, Message = "Failed to load connection configurations for health check: {reason}")]
    public static partial IGenericMessage AllConnectionsLoadFailed(ILogger logger, string reason);

    /// <summary>
    /// Logs that resolving a specific connection through <c>IConnectionProvider</c> failed during
    /// the health check.
    /// </summary>
    [MessageLogging(EventId = 12004, Level = LogLevel.Error, Message = "Failed to resolve connection '{connectionName}' for health check: {reason}")]
    public static partial IGenericMessage ConnectionResolutionFailed(ILogger logger, string connectionName, string reason);

    /// <summary>
    /// Logs that a connection's health probe failed.
    /// </summary>
    [MessageLogging(EventId = 12005, Level = LogLevel.Error, Message = "Health probe failed for connection '{connectionName}': {reason}")]
    public static partial IGenericMessage ProbeFailed(ILogger logger, string connectionName, string reason);

    /// <summary>
    /// Logs that a connection has health checks enabled but its connection type does not implement
    /// <c>ISupportsHealthProbe</c>, so it is reported as unprobed rather than healthy or failed.
    /// </summary>
    [MessageLogging(EventId = 12006, Level = LogLevel.Warning, Message = "Connection '{connectionName}' has health checks enabled but its connection type does not support health probing")]
    public static partial IGenericMessage NoProbeCapability(ILogger logger, string connectionName);
}
