using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Data.Abstractions.Results;
using Fdw.Results;
using Fdw.Services.Abstractions;
using Fdw.Services.Authentication.Abstractions.Security;
using Fdw.Services.Connections.Abstractions;
using Fdw.Services.Connections.Logging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Services.Connections;

/// <summary>
/// Periodic background worker that probes <c>conn.Connection</c> rows with
/// <see cref="IConnectionImplementationConfiguration.HealthCheckEnabled"/> set, honoring each row's own
/// <see cref="IConnectionImplementationConfiguration.HealthCheckOnStartup"/> and
/// <see cref="IConnectionImplementationConfiguration.HealthCheckIntervalSeconds"/>, and persisting results.
/// </summary>
/// <remarks>
/// Why this exists alongside <see cref="ConnectionsHealthCheckable"/>: that class is a pull-only
/// aggregate snapshot for <c>GET /api/v1/health/system</c> — it never persists anything and ignores
/// <c>HealthCheckOnStartup</c>/<c>HealthCheckIntervalSeconds</c>. This worker is the actual engine
/// those two columns were designed for: it runs unattended, writes <c>LastTested*</c> back through
/// <c>ConnectionConfigurationProvider.Save(IConnectionImplementationConfiguration, CancellationToken)</c> (inherited,
/// generic-closed; not a resolvable cref target),
/// and records history through <see cref="IConnectionHealthService"/> — the same two writes
/// <c>TestConnectionEndpointBase</c> performs for a manual test, just on a timer instead of a click.
/// <para>
/// Why a scan interval separate from each row's own interval: each row's own
/// <see cref="IConnectionImplementationConfiguration.HealthCheckIntervalSeconds"/> is the domain value that governs
/// how often THAT connection is actually probed (never defaulted — NO FALLBACKS). The wait between scans
/// is only when this worker next re-evaluates "has enough time elapsed" against every enabled row: it
/// waits until the soonest of them falls due, kept between the shortest and longest scan waits it is
/// registered with. Neither bound stands in for a missing domain value.
/// </para>
/// <para>
/// Why <see cref="IServiceScopeFactory"/> in the constructor, not <see cref="IConnectionProvider"/> or
/// <see cref="IConnectionHealthService"/> directly: this worker is registered as a singleton
/// <see cref="IHostedService"/>, but <c>IConnectionProvider</c> and <c>IConnectionHealthService</c> are
/// Scoped (the latter depends on the Scoped <c>IDataGateway</c>). A scope is created per collect — the
/// same pattern <see cref="ConnectionsHealthCheckable"/> documents and
/// <c>PipelineExecutionBackgroundService</c> uses for its per-execution scope.
/// </para>
/// </remarks>
public sealed class ConnectionHealthMonitorWorker : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly TimeSpan _shortestScan;
    private readonly TimeSpan _longestScan;
    private readonly ILogger<ConnectionHealthMonitorWorker> _logger;

    private readonly Dictionary<Guid, DateTimeOffset> _lastChecked = new();
    private readonly Dictionary<Guid, bool> _lastHealthy = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="ConnectionHealthMonitorWorker"/> class.
    /// </summary>
    /// <param name="scopeFactory">Factory used to create one DI scope per collect.</param>
    /// <param name="shortestScan">The shortest wait between scans, and the wait before the first scheduled one.</param>
    /// <param name="longestScan">
    /// The longest wait between scans: the longest a health check enabled after a scan waits to be noticed.
    /// </param>
    /// <param name="logger">Optional logger; falls back to <see cref="NullLogger{T}"/>.</param>
    public ConnectionHealthMonitorWorker(
        IServiceScopeFactory scopeFactory,
        TimeSpan shortestScan,
        TimeSpan longestScan,
        ILogger<ConnectionHealthMonitorWorker>? logger = null)
    {
        _scopeFactory = scopeFactory ?? throw new ArgumentNullException(nameof(scopeFactory));
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(shortestScan, TimeSpan.Zero);
        ArgumentOutOfRangeException.ThrowIfLessThan(longestScan, shortestScan);
        _shortestScan = shortestScan;
        _longestScan = longestScan;
        _logger = logger ?? NullLogger<ConnectionHealthMonitorWorker>.Instance;
    }

    /// <inheritdoc/>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        ConnectionHealthMonitorWorkerLog.WorkerStarted(_logger);

        if (!await RunStartupProbes(stoppingToken).ConfigureAwait(false))
        {
            ConnectionHealthMonitorWorkerLog.MonitoringIdleNoConnectionContainer(_logger);
            return;
        }

        using var timer = new PeriodicTimer(_shortestScan);
        try
        {
            while (await timer.WaitForNextTickAsync(stoppingToken).ConfigureAwait(false))
            {
                if (await RunScheduledProbes(stoppingToken).ConfigureAwait(false) is not { } nextScan)
                {
                    ConnectionHealthMonitorWorkerLog.MonitoringIdleNoConnectionContainer(_logger);
                    return;
                }

                timer.Period = nextScan;
            }
        }
        catch (OperationCanceledException ex) when (stoppingToken.IsCancellationRequested)
        {
            ConnectionHealthMonitorWorkerLog.WorkerCancelledDuringShutdown(_logger, ex);
        }

        ConnectionHealthMonitorWorkerLog.WorkerStopping(_logger);
    }

    /// <summary>Runs the one-time HealthCheckOnStartup collect over every enabled connection.</summary>
    /// <param name="ct">Token observed for host shutdown.</param>
    /// <returns>
    /// <c>false</c> when this host's configuration store registers no connection container — the caller
    /// must stop monitoring. <c>true</c> in every other case, including a genuine load failure (which is
    /// reported at Error here and retried on the next tick).
    /// </returns>
    private async Task<bool> RunStartupProbes(CancellationToken ct)
    {
        var scope = _scopeFactory.CreateAsyncScope();
        await using (scope.ConfigureAwait(false))
        {
            // This worker runs on its own internal timer, never inside an HTTP request, so it has
            // no caller identity to read a context from -- without this elevation every probe (and
            // the config read above it) lands on DenySessionContext instead of the intended system
            // bypass. Same remediation FDW-767 applied to PipelineExecutionBackgroundService /
            // OrchestrationNodeOrchestratorBackgroundService.
            using var elevation = new SystemAuthenticationContextScope(
                scope.ServiceProvider.GetRequiredService<IAuthenticationContextAccessor>());

            // The rows alone: whether a connection is probed is decided by fields of its row, and Probe
            // reads the one it probes whole (FDW-970).
            var configProvider = scope.ServiceProvider.GetRequiredService<IConnectionConfigurationProvider>();
            var allResult = await configProvider.Get(ConfigurationLevels.RecordOnly, ct).ConfigureAwait(false);
            if (!allResult.IsSuccess)
            {
                if (IsConnectionContainerAbsent(allResult))
                    return false;

                ConnectionHealthMonitorWorkerLog.LoadConnectionsFailed(_logger, allResult.CurrentMessage ?? "Unknown error");
                return true;
            }

            var startupConnections = (allResult.Value ?? [])
                .Where(c => c.HealthCheckEnabled && c.HealthCheckOnStartup)
                .ToList();

            ConnectionHealthMonitorWorkerLog.StartupProbesEvaluating(_logger, startupConnections.Count);

            foreach (var connection in startupConnections)
            {
                ct.ThrowIfCancellationRequested();
                await ProbeAndPersist(scope.ServiceProvider, connection, ct).ConfigureAwait(false);
            }
        }

        return true;
    }

    /// <summary>Runs one scan-tick collect, probing every enabled connection whose own interval is due.</summary>
    /// <param name="ct">Token observed for host shutdown.</param>
    /// <returns>
    /// How long to wait before the next scan; <see langword="null"/> when this host's configuration store
    /// registers no connection container, and the caller must stop monitoring. A genuine load failure is
    /// reported at Error here and retried after the shortest scan wait.
    /// </returns>
    private async Task<TimeSpan?> RunScheduledProbes(CancellationToken ct)
    {
        var scope = _scopeFactory.CreateAsyncScope();
        await using (scope.ConfigureAwait(false))
        {
            // Same elevation as RunStartupProbes, and for the same reason: this fires on the
            // worker's own internal timer, never inside an HTTP request, so without
            // it every tick's config read (and any due probe) lands on DenySessionContext instead
            // of the intended system bypass -- observed live as a deny-principal warning burst every
            // 15 seconds, unattributed to any real request.
            using var elevation = new SystemAuthenticationContextScope(
                scope.ServiceProvider.GetRequiredService<IAuthenticationContextAccessor>());

            // The rows alone, as in RunStartupProbes: every scan read every connection whole (FDW-970).
            var configProvider = scope.ServiceProvider.GetRequiredService<IConnectionConfigurationProvider>();
            var allResult = await configProvider.Get(ConfigurationLevels.RecordOnly, ct).ConfigureAwait(false);
            if (!allResult.IsSuccess)
            {
                if (IsConnectionContainerAbsent(allResult))
                    return null;

                ConnectionHealthMonitorWorkerLog.LoadConnectionsFailed(_logger, allResult.CurrentMessage ?? "Unknown error");
                return _shortestScan;
            }

            var connections = allResult.Value ?? [];
            var now = DateTimeOffset.UtcNow;

            var dueConnections = connections
                .Where(c => c.HealthCheckEnabled
                    && c.HealthCheckIntervalSeconds.HasValue
                    && (!_lastChecked.TryGetValue(c.Id, out var last)
                        || (now - last).TotalSeconds >= c.HealthCheckIntervalSeconds.Value))
                .ToList();

            ConnectionHealthMonitorWorkerLog.ScheduledProbesEvaluating(_logger, dueConnections.Count);

            foreach (var connection in dueConnections)
            {
                ct.ThrowIfCancellationRequested();
                await ProbeAndPersist(scope.ServiceProvider, connection, ct).ConfigureAwait(false);
            }

            return NextScanIn(connections, DateTimeOffset.UtcNow);
        }
    }

    // Why the next scan waits for the soonest enabled connection to fall due: every scan reads every
    // connection's row, and a fixed 15-second scan re-read them all on a host whose connections were
    // nowhere near due, or had no health check at all. Waiting for the soonest one probes nothing
    // later than before. The wait is kept between the shortest and longest scan waits, so a health
    // check enabled after this scan is noticed within the longest (FDW-970).
    private TimeSpan NextScanIn(IReadOnlyList<IConnectionImplementationConfiguration> connections, DateTimeOffset now)
    {
        var wait = _longestScan;
        foreach (var connection in connections)
        {
            if (!connection.HealthCheckEnabled || connection.HealthCheckIntervalSeconds is not { } seconds)
                continue;

            var due = _lastChecked.TryGetValue(connection.Id, out var last)
                ? last.AddSeconds(seconds) - now
                : TimeSpan.Zero;
            if (due < wait)
                wait = due;
        }

        var next = wait < _shortestScan ? _shortestScan : wait;
        ConnectionHealthMonitorWorkerLog.NextScanScheduled(_logger, next.TotalSeconds);
        return next;
    }

    private static bool IsConnectionContainerAbsent(IGenericResult result) =>
        result.CodeChain.Any(code => code is DataPathNotFoundCode or ContainerNotFoundInPathCode);

    // Why nothing that happens to one connection can end the pass: the pass probes connection after
    // connection, so a probe or a write that threw used to leave every connection after it unprobed
    // for that tick -- and, out of ExecuteAsync, end monitoring for the life of the host. Each
    // connection is its own boundary: what it cannot answer is recorded against it, and the pass goes
    // on. Host shutdown is the one thing let through, and it is never recorded as a result.
    private async Task ProbeAndPersist(IServiceProvider services, IConnectionImplementationConfiguration connection, CancellationToken ct)
    {
        ConnectionHealthMonitorWorkerLog.ProbingConnection(_logger, connection.Name);

        _lastChecked[connection.Id] = DateTimeOffset.UtcNow;

        // A probe cut short by shutdown measured nothing; recording it would mark a healthy
        // connection unhealthy on every restart.
        if (await Probe(services, connection, ct).ConfigureAwait(false) is not { } outcome || ct.IsCancellationRequested)
            return;

        try
        {
            await PersistResult(services.GetRequiredService<IConnectionHealthService>(), connection,
                outcome.Success, outcome.Message, outcome.ResponseTimeMs, ct).ConfigureAwait(false);
        }
        catch (Exception ex) when (!ct.IsCancellationRequested)
        {
            ConnectionHealthMonitorWorkerLog.PersistHistoryThrew(_logger, ex, connection.Name, ex.Message);
        }
    }

    // Null when the connection's type has no probe: there is nothing to record for it.
    private async Task<(bool Success, string? Message, int? ResponseTimeMs)?> Probe(
        IServiceProvider services, IConnectionImplementationConfiguration connection, CancellationToken ct)
    {
        try
        {
            // By Id, which reads the connection whole and builds it: the scan read rows alone, and a
            // connection is built from its children too (AdditionalProperties, Limits) (FDW-970).
            var getResult = await services.GetRequiredService<IConnectionProvider>().Get(connection.Id, ct).ConfigureAwait(false);
            if (!getResult.IsSuccess || getResult.Value is null)
            {
                var reason = getResult.CurrentMessage ?? "Connection could not be resolved";
                ConnectionHealthMonitorWorkerLog.ConnectionResolutionFailed(_logger, connection.Name, reason);
                return (false, reason, null);
            }

            if (getResult.Value is not ISupportsHealthProbe probe)
            {
                ConnectionHealthMonitorWorkerLog.NoProbeCapability(_logger, connection.Name);
                return null;
            }

            var stopwatch = Stopwatch.StartNew();
            var probeResult = await probe.Probe(ct).ConfigureAwait(false);
            stopwatch.Stop();
            var responseTimeMs = (int)stopwatch.ElapsedMilliseconds;

            if (probeResult.IsSuccess)
                return (true, null, responseTimeMs);

            var message = probeResult.CurrentMessage ?? "Probe failed";
            ConnectionHealthMonitorWorkerLog.ProbeFailed(_logger, connection.Name, message);
            return (false, message, responseTimeMs);
        }
        catch (Exception ex) when (!ct.IsCancellationRequested)
        {
            ConnectionHealthMonitorWorkerLog.ProbeThrew(_logger, ex, connection.Name, ex.Message);
            return (false, ex.Message, null);
        }
    }

    private async Task PersistResult(
        IConnectionHealthService healthService,
        IConnectionImplementationConfiguration connection,
        bool success,
        string? message,
        int? responseTimeMs,
        CancellationToken ct)
    {
        var recordResult = await healthService.RecordHealthCheck(
            connection.Id, connection.Name, success, responseTimeMs, message, ct).ConfigureAwait(false);
        if (!recordResult.IsSuccess)
        {
            ConnectionHealthMonitorWorkerLog.PersistHistoryFailed(_logger, connection.Name, recordResult.CurrentMessage ?? "Record failed");
            return;
        }

        var hadPrevious = _lastHealthy.TryGetValue(connection.Id, out var wasHealthy);
        _lastHealthy[connection.Id] = success;
        if (hadPrevious && wasHealthy != success)
        {
            if (success)
                ConnectionHealthMonitorWorkerLog.HealthStateChanged(_logger, connection.Name, success);
            else
                ConnectionHealthMonitorWorkerLog.HealthStateChangedUnhealthy(_logger, connection.Name, success);
        }
    }
}
