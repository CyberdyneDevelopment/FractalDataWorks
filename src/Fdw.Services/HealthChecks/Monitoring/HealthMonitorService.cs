using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Fdw.Abstractions;
using Fdw.Results;
using Fdw.Services.Abstractions.Health;
using Fdw.Services.Abstractions.Health.Monitoring;
using Fdw.Services.Abstractions.Health.Monitoring.Logging;

namespace Fdw.Services.HealthChecks.Monitoring;

/// <summary>
/// Default implementation of <see cref="IHealthMonitorService"/> that aggregates health
/// from registered <see cref="IHealthCheckable"/> services, tracks throughput via in-memory
/// circular buffers, and stores history with configurable retention.
/// </summary>
public sealed class HealthMonitorService : IHealthMonitorService
{
    private readonly IEnumerable<IHealthCheckable> _healthCheckables;
    private readonly IServiceProvider _serviceProvider;
    private readonly HealthMonitorConfiguration _configuration;
    private readonly ILogger<HealthMonitorService> _logger;

    private readonly ConcurrentDictionary<string, CircularBuffer<HealthCheckPoint>> _historyBuffers = new(StringComparer.OrdinalIgnoreCase);
    private readonly ConcurrentDictionary<string, CircularBuffer<ThroughputDataPoint>> _throughputBuffers = new(StringComparer.OrdinalIgnoreCase);
    private readonly ConcurrentDictionary<string, DateTimeOffset> _serviceStartTimes = new(StringComparer.OrdinalIgnoreCase);

    private static readonly IHealthState HealthyState = HealthStates.ByName("Healthy");
    private static readonly IHealthState UnhealthyState = HealthStates.ByName("Unhealthy");

    /// <summary>
    /// Initializes a new instance of the <see cref="HealthMonitorService"/> class.
    /// </summary>
    /// <param name="healthCheckables">The registered health checkable services.</param>
    /// <param name="serviceProvider">The service provider for resolving dependencies.</param>
    /// <param name="configuration">The health monitor configuration.</param>
    /// <param name="logger">Optional logger instance.</param>
    public HealthMonitorService(
        IEnumerable<IHealthCheckable> healthCheckables,
        IServiceProvider serviceProvider,
        HealthMonitorConfiguration configuration,
        ILogger<HealthMonitorService>? logger = null)
    {
        _healthCheckables = healthCheckables;
        _serviceProvider = serviceProvider;
        _configuration = configuration;
        _logger = logger ?? NullLogger<HealthMonitorService>.Instance;

        HealthMonitorLog.HealthMonitorInitialized(
            _logger,
            _configuration.CheckIntervalSeconds,
            _configuration.HistoryRetentionMinutes);
    }

    // ── IGenericService ─────────────────────────────────────────────────────────

    /// <inheritdoc/>
    public string Id => nameof(HealthMonitorService);

    /// <inheritdoc />
    public string Name => _configuration.Name;

    /// <inheritdoc/>
    public string ServiceType => "HealthMonitor";

    /// <inheritdoc/>
    public bool IsAvailable => true;

    /// <inheritdoc/>
    // Why: the health monitor domain is query-only — commands fail loud with a structured message,
    // never a silent no-op (NO FALLBACKS).
    public Task<IGenericResult<T>> Execute<T>(IGenericCommand command, CancellationToken cancellationToken = default)
        => Task.FromResult(GenericResult<T>.Failure(HealthMonitorLog.CommandsNotSupported(_logger, Id)));

    /// <inheritdoc/>
    public Task<IGenericResult> Execute(IGenericCommand command, CancellationToken cancellationToken = default)
        => Task.FromResult(GenericResult.Failure(HealthMonitorLog.CommandsNotSupported(_logger, Id)));

    /// <inheritdoc/>
    public async Task<IGenericResult<SystemHealthSnapshot>> GetSystemHealth(CancellationToken cancellationToken = default)
    {
        var checkables = _healthCheckables.ToList();
        HealthMonitorLog.SystemHealthCheckStarting(_logger, checkables.Count);

        var serviceSnapshots = new List<ServiceHealthSnapshot>();
        IHealthState worstStatus = HealthyState;

        foreach (var checkable in checkables)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var snapshot = await CheckService(checkable, cancellationToken).ConfigureAwait(false);
            serviceSnapshots.Add(snapshot);

            if (!snapshot.Status.IsHealthy && worstStatus.IsHealthy)
            {
                worstStatus = snapshot.Status;
            }
            else if (snapshot.Status.Id == UnhealthyState.Id)
            {
                worstStatus = snapshot.Status;
            }
        }

        var systemSnapshot = new SystemHealthSnapshot
        {
            OverallStatus = worstStatus,
            Services = serviceSnapshots,
            Timestamp = DateTimeOffset.UtcNow
        };

        // Why (FDW-583): branch on outcome, not on whether anything threw — an Unhealthy overall
        // status must print at Error; only the Healthy case is a routine Information breadcrumb.
        if (worstStatus.IsHealthy)
            HealthMonitorLog.SystemHealthCheckCompleted(_logger, worstStatus.Name);
        else
            HealthMonitorLog.SystemHealthCheckUnhealthy(_logger, worstStatus.Name);

        return GenericResult<SystemHealthSnapshot>.Success(systemSnapshot);
    }

    /// <inheritdoc/>
    public async Task<IGenericResult<ServiceHealthSnapshot>> GetServiceHealth(string serviceName, CancellationToken cancellationToken = default)
    {
        HealthMonitorLog.ServiceHealthCheckStarting(_logger, serviceName);

        var checkable = _healthCheckables.FirstOrDefault(c =>
            string.Equals(c.ServiceName, serviceName, StringComparison.OrdinalIgnoreCase));

        if (checkable is null)
        {
            var message = HealthMonitorLog.ServiceNotFound(_logger, serviceName);
            return GenericResult<ServiceHealthSnapshot>.Failure(message);
        }

        var snapshot = await CheckService(checkable, cancellationToken).ConfigureAwait(false);
        return GenericResult<ServiceHealthSnapshot>.Success(snapshot);
    }

    /// <inheritdoc/>
    public Task<IGenericResult<ThroughputData>> GetThroughput(string serviceName, TimeSpan window, CancellationToken cancellationToken = default)
    {
        HealthMonitorLog.GettingThroughput(_logger, serviceName, window.TotalSeconds);

        if (!_throughputBuffers.TryGetValue(serviceName, out var buffer))
        {
            var message = HealthMonitorLog.ServiceNotFound(_logger, serviceName);
            return Task.FromResult(GenericResult<ThroughputData>.Failure(message));
        }

        var cutoff = DateTimeOffset.UtcNow - window;
        var dataPoints = buffer.GetItems()
            .Where(dp => dp.Timestamp >= cutoff)
            .OrderBy(dp => dp.Timestamp)
            .ToList();

        var totalRequests = dataPoints.Sum(dp => dp.RequestCount);
        var totalErrors = dataPoints.Sum(dp => dp.ErrorCount);
        var windowSeconds = window.TotalSeconds;

        double avgLatency = 0;
        double p95Latency = 0;

        if (dataPoints.Count > 0)
        {
            avgLatency = dataPoints.Average(dp => dp.AverageLatencyMs);
            var sorted = dataPoints.OrderBy(dp => dp.AverageLatencyMs).ToList();
            var p95Index = (int)Math.Ceiling(sorted.Count * 0.95) - 1;
            if (p95Index >= 0 && p95Index < sorted.Count)
            {
                p95Latency = sorted[p95Index].AverageLatencyMs;
            }
        }

        var throughput = new ThroughputData
        {
            ServiceName = serviceName,
            RequestsPerSecond = windowSeconds > 0 ? totalRequests / windowSeconds : 0,
            AvgLatencyMs = avgLatency,
            P95LatencyMs = p95Latency,
            ErrorRate = totalRequests > 0 ? (double)totalErrors / totalRequests : 0,
            DataPoints = dataPoints
        };

        HealthMonitorLog.ThroughputComputed(
            _logger,
            serviceName,
            throughput.RequestsPerSecond,
            throughput.AvgLatencyMs,
            dataPoints.Count);

        return Task.FromResult(GenericResult<ThroughputData>.Success(throughput));
    }

    /// <inheritdoc/>
    public Task<IGenericResult<IReadOnlyList<HealthCheckPoint>>> GetHealthHistory(string serviceName, TimeSpan window, CancellationToken cancellationToken = default)
    {
        HealthMonitorLog.GettingHealthHistory(_logger, serviceName, window.TotalSeconds);

        if (!_historyBuffers.TryGetValue(serviceName, out var buffer))
        {
            var message = HealthMonitorLog.ServiceNotFound(_logger, serviceName);
            return Task.FromResult(GenericResult<IReadOnlyList<HealthCheckPoint>>.Failure(message));
        }

        var cutoff = DateTimeOffset.UtcNow - window;
        var history = buffer.GetItems()
            .Where(cp => cp.Timestamp >= cutoff)
            .OrderBy(cp => cp.Timestamp)
            .ToList();

        HealthMonitorLog.HealthHistoryReturned(_logger, history.Count, serviceName);

        return Task.FromResult(GenericResult<IReadOnlyList<HealthCheckPoint>>.Success(history));
    }

    private async Task<ServiceHealthSnapshot> CheckService(IHealthCheckable checkable, CancellationToken cancellationToken)
    {
        var serviceName = checkable.ServiceName;
        HealthMonitorLog.ServiceHealthCheckStarting(_logger, serviceName);

        var stopwatch = Stopwatch.StartNew();
        IHealthState status;
        string? details = null;

        try
        {
            var result = await checkable.CheckHealth(_serviceProvider, cancellationToken).ConfigureAwait(false);
            stopwatch.Stop();

            if (result.IsSuccess && result.Value is not null)
            {
                status = result.Value.Status;
                details = result.Value.Description;
            }
            else
            {
                status = UnhealthyState;
                details = result.CurrentMessage;
                // Why (FDW-583): the non-exception failure branch previously logged nothing — the
                // reason was discarded into the snapshot's Details field only. This is the
                // non-exception twin of ServiceHealthCheckFailed below.
                HealthMonitorLog.ServiceHealthCheckReturnedFailure(_logger, serviceName, details);
            }
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            HealthMonitorLog.ServiceHealthCheckFailed(_logger, ex, serviceName);
            status = UnhealthyState;
            details = ex.Message;
        }

        var responseTimeMs = stopwatch.Elapsed.TotalMilliseconds;

        // Why (FDW-583): branch on outcome, not on whether anything threw — a non-Healthy status must
        // print at Error even when CheckHealth returned cleanly instead of throwing.
        if (status.IsHealthy)
            HealthMonitorLog.ServiceHealthCheckCompleted(_logger, serviceName, status.Name, responseTimeMs);
        else
            HealthMonitorLog.ServiceHealthCheckCompletedUnhealthy(_logger, serviceName, status.Name, responseTimeMs);

        var startTime = _serviceStartTimes.GetOrAdd(serviceName, DateTimeOffset.UtcNow);

        var checkpoint = new HealthCheckPoint
        {
            Timestamp = DateTimeOffset.UtcNow,
            Status = status,
            ResponseTimeMs = responseTimeMs,
            Details = details
        };

        var maxHistoryEntries = (_configuration.HistoryRetentionMinutes * 60) / Math.Max(_configuration.CheckIntervalSeconds, 1);
        var historyBuffer = _historyBuffers.GetOrAdd(serviceName, _ => new CircularBuffer<HealthCheckPoint>(Math.Max(maxHistoryEntries, 100)));
        historyBuffer.Add(checkpoint);

        var throughputBuffer = _throughputBuffers.GetOrAdd(serviceName, _ => new CircularBuffer<ThroughputDataPoint>(Math.Max(_configuration.ThroughputWindowSeconds, 100)));
        throughputBuffer.Add(new ThroughputDataPoint
        {
            Timestamp = DateTimeOffset.UtcNow,
            RequestCount = 1,
            AverageLatencyMs = responseTimeMs,
            ErrorCount = status.IsHealthy ? 0 : 1
        });

        return new ServiceHealthSnapshot
        {
            Name = serviceName,
            Status = status,
            ResponseTimeMs = responseTimeMs,
            LastCheckAt = DateTimeOffset.UtcNow,
            Uptime = DateTimeOffset.UtcNow - startTime
        };
    }
}
