using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Data.Abstractions.Results;
using Fdw.Results;
using Fdw.Services.Connections.Abstractions;
using Fdw.Services.Connections.Logging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Services.Connections;

/// <summary>
/// Periodic background worker that probes <c>conn.Connection</c> rows with
/// <see cref="ConnectionConfiguration.HealthCheckEnabled"/> set, honoring each row's own
/// <see cref="ConnectionConfiguration.HealthCheckOnStartup"/> and
/// <see cref="ConnectionConfiguration.HealthCheckIntervalSeconds"/>, and persisting results.
/// </summary>
/// <remarks>
/// Why this exists alongside <see cref="ConnectionsHealthCheckable"/>: that class is a pull-only
/// aggregate snapshot for <c>GET /api/v1/health/system</c> — it never persists anything and ignores
/// <c>HealthCheckOnStartup</c>/<c>HealthCheckIntervalSeconds</c>. This worker is the actual engine
/// those two columns were designed for: it runs unattended, writes <c>LastTested*</c> back through
/// <c>ConnectionConfigurationProvider.Save(ConnectionConfiguration, CancellationToken)</c> (inherited,
/// generic-closed; not a resolvable cref target),
/// and records history through <see cref="IConnectionHealthService"/> — the same two writes
/// <c>TestConnectionEndpointBase</c> performs for a manual test, just on a timer instead of a click.
/// <para>
/// Why a fixed internal scan tick rather than a config value: each row's own
/// <see cref="ConnectionConfiguration.HealthCheckIntervalSeconds"/> is the domain value that governs
/// how often THAT connection is actually probed (never defaulted — NO FALLBACKS). <see cref="ScanTick"/>
/// is only the resolution at which this worker re-evaluates "has enough time elapsed" against every
/// enabled row, analogous to a file-watcher's poll granularity — it is not standing in for a missing
/// domain value.
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
    private static readonly TimeSpan ScanTick = TimeSpan.FromSeconds(15);

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<ConnectionHealthMonitorWorker> _logger;

    // Why (FDW-623): in-memory scheduling cursors keyed by the connection's durable Id. This worker is a
    // singleton, so they survive across scan ticks. They replace the LastTested*/LastTestSuccess columns
    // that used to live on conn.Connection — health status now lives ONLY in conn.ConnectionHealthCheck,
    // and re-reading history every tick just to schedule would be wasteful. On restart the maps start
    // empty: every enabled connection is due once (like a fresh probe), then its own interval applies.
    // Access is single-threaded (the execute loop probes sequentially), so no synchronization is needed.
    private readonly Dictionary<Guid, DateTimeOffset> _lastChecked = new();
    private readonly Dictionary<Guid, bool> _lastHealthy = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="ConnectionHealthMonitorWorker"/> class.
    /// </summary>
    /// <param name="scopeFactory">Factory used to create one DI scope per collect.</param>
    /// <param name="logger">Optional logger; falls back to <see cref="NullLogger{T}"/>.</param>
    public ConnectionHealthMonitorWorker(IServiceScopeFactory scopeFactory, ILogger<ConnectionHealthMonitorWorker>? logger = null)
    {
        _scopeFactory = scopeFactory ?? throw new ArgumentNullException(nameof(scopeFactory));
        _logger = logger ?? NullLogger<ConnectionHealthMonitorWorker>.Instance;
    }

    /// <inheritdoc/>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        ConnectionHealthMonitorWorkerLog.WorkerStarted(_logger);

        // Why HealthCheckOnStartup runs once, up front, before the periodic loop: it is the
        // Initialize-phase probe every row asked for at host startup, independent of whether that
        // row also has a periodic HealthCheckIntervalSeconds.
        if (!await RunStartupProbes(stoppingToken).ConfigureAwait(false))
        {
            // Why the periodic loop is never entered: this host's configuration store registers no
            // connection container, so there is nothing for this worker to monitor and — the tree being
            // built once from configurationSchema.json — nothing that can appear later. Stating it once
            // at Information is the whole report; the loop would only restate it every ScanTick forever.
            ConnectionHealthMonitorWorkerLog.MonitoringIdleNoConnectionContainer(_logger);
            return;
        }

        using var timer = new PeriodicTimer(ScanTick);
        try
        {
            while (await timer.WaitForNextTickAsync(stoppingToken).ConfigureAwait(false))
            {
                // Why the same early exit is honored per tick and not only at startup: the startup collect
                // can miss the condition when the very first load fails transiently (that path logs the
                // usual Error and keeps monitoring), so the first tick that resolves to container-absence
                // is where the idle state is recognized instead.
                if (!await RunScheduledProbes(stoppingToken).ConfigureAwait(false))
                {
                    ConnectionHealthMonitorWorkerLog.MonitoringIdleNoConnectionContainer(_logger);
                    return;
                }
            }
        }
        catch (OperationCanceledException ex) when (stoppingToken.IsCancellationRequested)
        {
            // Why: graceful shutdown — PeriodicTimer.WaitForNextTickAsync throws when the linked
            // token is cancelled; this is the expected exit path, not a fault. Observed via
            // MessageLogging (FDW022 requires the caught exception be logged, returned, or rethrown)
            // rather than swallowed silently, mirroring DailyLimitResetJob's shutdown handling.
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
            var configProvider = scope.ServiceProvider.GetRequiredService<ConnectionConfigurationProvider>();
            var allResult = await configProvider.Get(ct).ConfigureAwait(false);
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
    /// <c>false</c> when this host's configuration store registers no connection container — the caller
    /// must stop monitoring. <c>true</c> in every other case, including a genuine load failure (which is
    /// reported at Error here and retried on the next tick).
    /// </returns>
    private async Task<bool> RunScheduledProbes(CancellationToken ct)
    {
        var scope = _scopeFactory.CreateAsyncScope();
        await using (scope.ConfigureAwait(false))
        {
            var configProvider = scope.ServiceProvider.GetRequiredService<ConnectionConfigurationProvider>();
            var allResult = await configProvider.Get(ct).ConfigureAwait(false);
            if (!allResult.IsSuccess)
            {
                if (IsConnectionContainerAbsent(allResult))
                    return false;

                ConnectionHealthMonitorWorkerLog.LoadConnectionsFailed(_logger, allResult.CurrentMessage ?? "Unknown error");
                return true;
            }

            var now = DateTimeOffset.UtcNow;

            // Why: a row is due when it has never been probed by this worker instance, or enough time has
            // elapsed since its last probe relative to ITS OWN HealthCheckIntervalSeconds — never a
            // worker-wide default. Last-probe time is the in-memory cursor, not a config column (FDW-623).
            var dueConnections = (allResult.Value ?? [])
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
        }

        return true;
    }

    // Why branch on the TYPED code and never on message text: "this store does not register the
    // connection container" is a distinct OUTCOME, not a flavour of failure, and the framework already
    // models outcomes as IResultCode. The node lookup (DataStore.Path / DataPath.Container) attaches
    // DataPathNotFound / ContainerNotFoundInPath, and ConfigurationGateway.Execute propagates the result
    // rather than flattening it, so the code arrives here intact in CodeChain. Matching
    // CurrentMessage strings instead would silently re-break the moment a message is reworded.
    // Why CodeChain and not Code: the cause is raised at the innermost node lookup and reaches this
    // caller wrapped by the provider's ToNewResult conversions, so it is a link in the chain, not the head.
    // Anything else — a dropped connection, a timeout, a malformed row — carries neither code and keeps
    // the existing per-tick Error, so a genuinely broken load in reference-api still fails loud.
    private static bool IsConnectionContainerAbsent(IGenericResult result) =>
        result.CodeChain.Any(code => code is DataPathNotFoundCode or ContainerNotFoundInPathCode);

    private async Task ProbeAndPersist(IServiceProvider services, ConnectionConfiguration connection, CancellationToken ct)
    {
        ConnectionHealthMonitorWorkerLog.ProbingConnection(_logger, connection.Name);

        // Why (FDW-623): mark this connection probed for the current cycle up front, keyed by its durable
        // Id, so the due-calc's interval is measured from now regardless of the probe outcome below.
        _lastChecked[connection.Id] = DateTimeOffset.UtcNow;

        var connectionProvider = services.GetRequiredService<IConnectionProvider>();
        var healthService = services.GetRequiredService<IConnectionHealthService>();

        var getResult = await connectionProvider.Get(connection.Name, ct).ConfigureAwait(false);
        if (!getResult.IsSuccess || getResult.Value is null)
        {
            var reason = getResult.CurrentMessage ?? "Connection could not be resolved";
            ConnectionHealthMonitorWorkerLog.ConnectionResolutionFailed(_logger, connection.Name, reason);
            await PersistResult(healthService, connection, false, reason, null, ct).ConfigureAwait(false);
            return;
        }

        // Why: a connection whose type does not implement ISupportsHealthProbe is neither persisted
        // healthy nor unhealthy — mirrors ConnectionsHealthCheckable's Degraded handling. Skipping the
        // persist here leaves LastTested* untouched rather than recording a misleading result.
        if (getResult.Value is not ISupportsHealthProbe probe)
        {
            ConnectionHealthMonitorWorkerLog.NoProbeCapability(_logger, connection.Name);
            return;
        }

        var stopwatch = Stopwatch.StartNew();
        var probeResult = await probe.Probe(ct).ConfigureAwait(false);
        stopwatch.Stop();
        var responseTimeMs = (int)stopwatch.ElapsedMilliseconds;

        var success = probeResult.IsSuccess;
        var message = success ? null : (probeResult.CurrentMessage ?? "Probe failed");

        if (!success)
        {
            ConnectionHealthMonitorWorkerLog.ProbeFailed(_logger, connection.Name, message!);
        }

        await PersistResult(healthService, connection, success, message, responseTimeMs, ct).ConfigureAwait(false);
    }

    private async Task PersistResult(
        IConnectionHealthService healthService,
        ConnectionConfiguration connection,
        bool success,
        string? message,
        int? responseTimeMs,
        CancellationToken ct)
    {
        // Why (FDW-623): the probe result is written ONLY to conn.ConnectionHealthCheck via the health
        // service — never back onto the connection configuration. Persisting LastTested* on the config and
        // calling Save re-versioned the whole connection aggregate on every probe (version-on-write) and
        // stranded its child rows; the health table is a plain, non-versioned record, so a probe is a
        // single insert with no cascade.
        var recordResult = await healthService.RecordHealthCheck(
            connection.Id, connection.Name, success, responseTimeMs, message, ct).ConfigureAwait(false);
        if (!recordResult.IsSuccess)
        {
            ConnectionHealthMonitorWorkerLog.PersistHistoryFailed(_logger, connection.Name, recordResult.CurrentMessage ?? "Record failed");
            return;
        }

        // Why (FDW-583): branch on outcome — a transition TO healthy (recovery) is Information; a
        // transition TO unhealthy is Error. The previous outcome is tracked in-memory (this worker is a
        // singleton) rather than re-read from config, since health status no longer lives on the connection.
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
