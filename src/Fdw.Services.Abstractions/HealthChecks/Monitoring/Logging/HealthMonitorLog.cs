using System;
using Microsoft.Extensions.Logging;
using Fdw.Messages;
using Fdw.MessageLogging;

namespace Fdw.Services.Abstractions.Health.Monitoring.Logging;

/// <summary>
/// Static logger class for health monitoring operations.
/// Uses MessageLogging source generator for high-performance structured logging.
/// </summary>
[MessageLoggingTypeCode("ABSTRACTIONS3")]
public static partial class HealthMonitorLog
{
    // ═══════════════════════════════════════════════════════════════════════════
    // Health Check Events (8501-8520)
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Logs when a system health check starts.
    /// </summary>
    [MessageLogging(
        EventId = 11000,
        Level = LogLevel.Debug,
        Message = "[HealthMonitor] Starting system health check with {serviceCount} registered service(s)")]
    public static partial IGenericMessage SystemHealthCheckStarting(
        ILogger logger,
        int serviceCount);

    /// <summary>
    /// Logs when a system health check completes with an overall Healthy status.
    /// </summary>
    [MessageLogging(
        EventId = 11001,
        Level = LogLevel.Information,
        Message = "[HealthMonitor] System health check completed - overall status: {overallStatus}")]
    public static partial IGenericMessage SystemHealthCheckCompleted(
        ILogger logger,
        string overallStatus);

    /// <summary>
    /// Logs when a system health check completes with a non-Healthy overall status.
    /// </summary>
    // Why Error, not Information (FDW-583): SystemHealthCheckCompleted above printed "overall status:
    // Unhealthy" as an Information record — indistinguishable from a healthy breadcrumb. Branched at
    // the GetSystemHealth call site on worstStatus.IsHealthy.
    [MessageLogging(
        EventId = 71001,
        Level = LogLevel.Error,
        Message = "[HealthMonitor] System health check completed - overall status: {overallStatus}")]
    public static partial IGenericMessage SystemHealthCheckUnhealthy(
        ILogger logger,
        string overallStatus);

    /// <summary>
    /// Logs when a service health check starts.
    /// </summary>
    [MessageLogging(
        EventId = 11002,
        Level = LogLevel.Trace,
        Message = "[HealthMonitor] Checking health for service '{serviceName}'")]
    public static partial IGenericMessage ServiceHealthCheckStarting(
        ILogger logger,
        string serviceName);

    /// <summary>
    /// Logs when a service health check completes with a Healthy status.
    /// </summary>
    [MessageLogging(
        EventId = 11003,
        Level = LogLevel.Debug,
        Message = "[HealthMonitor] Service '{serviceName}' health check completed - status: {status}, response time: {responseTimeMs}ms")]
    public static partial IGenericMessage ServiceHealthCheckCompleted(
        ILogger logger,
        string serviceName,
        string status,
        double responseTimeMs);

    /// <summary>
    /// Logs when a service health check completes with a non-Healthy status (no exception thrown).
    /// </summary>
    // Why Error, not Debug (FDW-583): ServiceHealthCheckCompleted above was the SOLE record when a
    // service reports Unhealthy without throwing, at Debug — effectively invisible. Branched at the
    // CheckService call site on status.IsHealthy.
    [MessageLogging(
        EventId = 71002,
        Level = LogLevel.Error,
        Message = "[HealthMonitor] Service '{serviceName}' health check completed - status: {status}, response time: {responseTimeMs}ms")]
    public static partial IGenericMessage ServiceHealthCheckCompletedUnhealthy(
        ILogger logger,
        string serviceName,
        string status,
        double responseTimeMs);

    /// <summary>
    /// Logs when a service health check fails with an exception.
    /// </summary>
    [MessageLogging(
        EventId = 71000,
        Level = LogLevel.Error,
        Message = "[HealthMonitor] Service '{serviceName}' health check failed")]
    public static partial IGenericMessage ServiceHealthCheckFailed(
        ILogger logger,
        Exception exception,
        string serviceName);

    /// <summary>
    /// Logs when a service health check returns a non-success/null result WITHOUT throwing — the
    /// non-exception twin of <see cref="ServiceHealthCheckFailed"/>.
    /// </summary>
    // Why Error (FDW-583): the "else" branch in HealthMonitorService.CheckService previously discarded
    // result.CurrentMessage into a snapshot field and logged nothing — the reason for the failure was
    // never printed.
    [MessageLogging(
        EventId = 71003,
        Level = LogLevel.Error,
        Message = "[HealthMonitor] Service '{serviceName}' health check returned a failure result: {reason}")]
    public static partial IGenericMessage ServiceHealthCheckReturnedFailure(
        ILogger logger,
        string serviceName,
        string? reason);

    /// <summary>
    /// Logs when a requested service is not found.
    /// </summary>
    [MessageLogging(
        EventId = 31000,
        Level = LogLevel.Warning,
        Message = "[HealthMonitor] Service '{serviceName}' not found in registered services")]
    public static partial IGenericMessage ServiceNotFound(
        ILogger logger,
        string serviceName);

    // ═══════════════════════════════════════════════════════════════════════════
    // Throughput Events (8521-8540)
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Logs when throughput data is requested.
    /// </summary>
    [MessageLogging(
        EventId = 11004,
        Level = LogLevel.Trace,
        Message = "[HealthMonitor] Getting throughput for '{serviceName}' over {windowSeconds}s window")]
    public static partial IGenericMessage GettingThroughput(
        ILogger logger,
        string serviceName,
        double windowSeconds);

    /// <summary>
    /// Logs when throughput data is computed.
    /// </summary>
    [MessageLogging(
        EventId = 11005,
        Level = LogLevel.Debug,
        Message = "[HealthMonitor] Throughput for '{serviceName}': {requestsPerSecond} req/s, avg latency {avgLatencyMs}ms, {dataPointCount} data point(s)")]
    public static partial IGenericMessage ThroughputComputed(
        ILogger logger,
        string serviceName,
        double requestsPerSecond,
        double avgLatencyMs,
        int dataPointCount);

    // ═══════════════════════════════════════════════════════════════════════════
    // History Events (8541-8560)
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Logs when health history is requested.
    /// </summary>
    [MessageLogging(
        EventId = 11006,
        Level = LogLevel.Trace,
        Message = "[HealthMonitor] Getting health history for '{serviceName}' over {windowSeconds}s window")]
    public static partial IGenericMessage GettingHealthHistory(
        ILogger logger,
        string serviceName,
        double windowSeconds);

    /// <summary>
    /// Logs when health history is returned.
    /// </summary>
    [MessageLogging(
        EventId = 11007,
        Level = LogLevel.Debug,
        Message = "[HealthMonitor] Returning {checkPointCount} health check point(s) for '{serviceName}'")]
    public static partial IGenericMessage HealthHistoryReturned(
        ILogger logger,
        int checkPointCount,
        string serviceName);

    // ═══════════════════════════════════════════════════════════════════════════
    // Lifecycle Events (8561-8580)
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Logs when a health checkable service is registered.
    /// </summary>
    [MessageLogging(
        EventId = 11008,
        Level = LogLevel.Information,
        Message = "[HealthMonitor] Registered health checkable service '{serviceName}'")]
    public static partial IGenericMessage ServiceRegistered(
        ILogger logger,
        string serviceName);

    /// <summary>
    /// Logs when the health monitor service is initialized.
    /// </summary>
    [MessageLogging(
        EventId = 11009,
        Level = LogLevel.Information,
        Message = "[HealthMonitor] Health monitor initialized with check interval {checkIntervalSeconds}s, history retention {historyRetentionMinutes}m")]
    public static partial IGenericMessage HealthMonitorInitialized(
        ILogger logger,
        int checkIntervalSeconds,
        int historyRetentionMinutes);

    /// <summary>
    /// Logs when old history entries are pruned.
    /// </summary>
    [MessageLogging(
        EventId = 11010,
        Level = LogLevel.Trace,
        Message = "[HealthMonitor] Pruned {prunedCount} expired history entries for '{serviceName}'")]
    public static partial IGenericMessage HistoryPruned(
        ILogger logger,
        int prunedCount,
        string serviceName);

    /// <summary>
    /// Logs when Execute is called on a health monitor — the domain is query-only; commands are
    /// not supported (structured failure, never a silent no-op).
    /// </summary>
    [MessageLogging(
        EventId = 41001,
        Level = LogLevel.Warning,
        Message = "[HealthMonitor] '{monitorName}' does not accept commands — the health monitor domain is query-only")]
    public static partial IGenericMessage CommandsNotSupported(
        ILogger logger,
        string monitorName);

    /// <summary>
    /// Logs when a health monitor factory receives a configuration of the wrong concrete type.
    /// </summary>
    [MessageLogging(
        EventId = 41002,
        Level = LogLevel.Warning,
        Message = "[HealthMonitor] Factory '{factoryName}' requires {expectedType} but received {actualType}")]
    public static partial IGenericMessage FactoryConfigurationCastFailed(
        ILogger logger,
        string factoryName,
        string expectedType,
        string actualType);

    /// <summary>
    /// Logs when a host's HealthMonitors options resolve to no rows or more than one row — the
    /// domain expects exactly one monitor per host (fail loud, no first-row fallback).
    /// </summary>
    [MessageLogging(
        EventId = 41003,
        Level = LogLevel.Warning,
        Message = "[HealthMonitor] Host declares {rowCount} HealthMonitors rows — exactly one is required")]
    public static partial IGenericMessage MonitorRowCountInvalid(
        ILogger logger,
        int rowCount);

    /// <summary>
    /// Logs when no HealthMonitors row matches the requested name/id — check the host's
    /// HealthMonitors appsettings section and its HealthMonitor:Name selector.
    /// </summary>
    [MessageLogging(
        EventId = 61000,
        Level = LogLevel.Warning,
        Message = "[HealthMonitor] No HealthMonitors row named '{name}' is bound from this host's configuration")]
    public static partial IGenericMessage MonitorRowNotFound(
        ILogger logger,
        string name);

    /// <summary>
    /// Logs when a write operation is attempted against health monitor configuration — host-topology
    /// configuration (appsettings/environment) is not mutable at runtime.
    /// </summary>
    [MessageLogging(
        EventId = 41004,
        Level = LogLevel.Warning,
        Message = "[HealthMonitor] {operation} is not supported — health monitor rows are host configuration (appsettings/environment)")]
    public static partial IGenericMessage WriteNotSupported(
        ILogger logger,
        string operation);
}
