using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Fdw.Results;
using Fdw.Services.Abstractions.Health;
using Fdw.Services.Connections.Abstractions;
using Fdw.Services.Connections.Logging;

namespace Fdw.Services.Connections;

/// <summary>
/// Domain-level <see cref="IHealthCheckable"/> for the Connections domain. At check time, enumerates
/// every <c>conn.Connection</c> row with <see cref="ConnectionConfiguration.HealthCheckEnabled"/> set,
/// resolves each through <see cref="IConnectionProvider"/>, and probes it via
/// <see cref="ISupportsHealthProbe"/> when the resolved connection implements that capability.
/// </summary>
/// <remarks>
/// Why ONE domain-level checkable, not one per connection: connection rows are runtime data (added,
/// edited, and enabled/disabled through the admin UI) — they can never be individual compile-time DI
/// registrations. This class enumerates the current rows fresh on every <see cref="CheckHealth"/>
/// call instead.
/// <para>
/// Why the Scoped <see cref="IConnectionProvider"/> is never captured in the constructor: this
/// checkable is registered as a Singleton (see
/// <see cref="ConnectionTypes"/>),
/// alongside every other domain-level <see cref="IHealthCheckable"/>. Capturing a Scoped service in a
/// Singleton constructor would pin it to the first scope that ever resolved this class. Instead, both
/// <see cref="ConnectionConfigurationProvider"/> and <see cref="IConnectionProvider"/> are resolved
/// from the <c>serviceProvider</c> parameter <see cref="CheckHealth"/> receives — the
/// scope the health monitor domain hands to every checkable it invokes. This is the contract's
/// intended use, not a service-locator workaround.
/// </para>
/// </remarks>
public sealed class ConnectionsHealthCheckable : IHealthCheckable
{
    private static readonly IHealthState HealthyState = HealthStates.ByName("Healthy");
    private static readonly IHealthState UnhealthyState = HealthStates.ByName("Unhealthy");
    private static readonly IHealthState DegradedState = HealthStates.ByName("Degraded");

    private readonly ILogger<ConnectionsHealthCheckable> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="ConnectionsHealthCheckable"/> class.
    /// </summary>
    /// <param name="logger">Optional logger; falls back to <see cref="NullLogger{T}"/>.</param>
    public ConnectionsHealthCheckable(ILogger<ConnectionsHealthCheckable>? logger = null)
    {
        _logger = logger ?? NullLogger<ConnectionsHealthCheckable>.Instance;
    }

    /// <inheritdoc/>
    public string ServiceName => "Connections";

    /// <inheritdoc/>
    public async Task<IGenericResult<IHealthCheckResult>> CheckHealth(
        IServiceProvider serviceProvider,
        CancellationToken cancellationToken = default)
    {
        ConnectionsHealthLog.CheckStarting(_logger);
        var stopwatch = Stopwatch.StartNew();

        // Why: resolved from the scope CheckHealth receives — see the class remarks. Never captured
        // as instance fields.
        var configProvider = serviceProvider.GetRequiredService<ConnectionConfigurationProvider>();
        var connectionProvider = serviceProvider.GetRequiredService<IConnectionProvider>();

        var allResult = await configProvider.Get(cancellationToken).ConfigureAwait(false);
        if (!allResult.IsSuccess)
        {
            var reason = allResult.CurrentMessage ?? "Unknown error";
            return GenericResult<IHealthCheckResult>.Failure(
                ConnectionsHealthLog.AllConnectionsLoadFailed(_logger, reason));
        }

        var checkedConnections = (allResult.Value ?? []).Where(c => c.HealthCheckEnabled).ToList();
        if (checkedConnections.Count == 0)
        {
            stopwatch.Stop();
            ConnectionsHealthLog.NoConnectionsEnabled(_logger);
            return GenericResult<IHealthCheckResult>.Success(new HealthCheckResult
            {
                Status = HealthyState,
                Description = "No connection health checks are enabled.",
                Duration = stopwatch.Elapsed
            });
        }

        var worstStatus = HealthyState;
        var detailLines = new List<string>();

        foreach (var connection in checkedConnections)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var (status, detail) = await ProbeOne(connection, connectionProvider, cancellationToken).ConfigureAwait(false);
            detailLines.Add(FormatDetail(connection.Name, status, detail));
            worstStatus = WorseOf(worstStatus, status);
        }

        stopwatch.Stop();

        // Why (FDW-583): branch on outcome, not on whether anything threw — CheckHealth returns
        // Success(Unhealthy) when every connection is down, so the completion record must print at
        // Error for that outcome instead of Information.
        if (worstStatus.Id == HealthyState.Id)
            ConnectionsHealthLog.CheckCompleted(_logger, checkedConnections.Count, worstStatus.Name);
        else
            ConnectionsHealthLog.CheckCompletedUnhealthy(_logger, checkedConnections.Count, worstStatus.Name);

        return GenericResult<IHealthCheckResult>.Success(new HealthCheckResult
        {
            Status = worstStatus,
            Description = string.Join("; ", detailLines),
            Duration = stopwatch.Elapsed
        });
    }

    // Why: one connection's outcome, isolated so a single resolution/probe failure never aborts the
    // rest of the collect — every enabled connection gets its own line in the aggregate Description.
    private async Task<(IHealthState Status, string Detail)> ProbeOne(
        ConnectionConfiguration connection,
        IConnectionProvider connectionProvider,
        CancellationToken cancellationToken)
    {
        var getResult = await connectionProvider.Get(connection.Name, cancellationToken).ConfigureAwait(false);
        if (!getResult.IsSuccess || getResult.Value is null)
        {
            var reason = getResult.CurrentMessage ?? "Connection could not be resolved";
            ConnectionsHealthLog.ConnectionResolutionFailed(_logger, connection.Name, reason);
            return (UnhealthyState, reason);
        }

        // Why: a connection whose type does not implement ISupportsHealthProbe is neither healthy
        // (unverified) nor a failure (nothing was wrong) — Degraded is the most honest representation
        // the snapshot model allows for "enabled but unprobeable" (NO FALLBACKS: never silently
        // reported as healthy).
        if (getResult.Value is not ISupportsHealthProbe probe)
        {
            ConnectionsHealthLog.NoProbeCapability(_logger, connection.Name);
            return (DegradedState, "connection type does not support health probing");
        }

        var probeResult = await probe.Probe(cancellationToken).ConfigureAwait(false);
        if (!probeResult.IsSuccess)
        {
            var reason = probeResult.CurrentMessage ?? "Probe failed";
            ConnectionsHealthLog.ProbeFailed(_logger, connection.Name, reason);
            return (UnhealthyState, reason);
        }

        return (HealthyState, "OK");
    }

    private static string FormatDetail(string name, IHealthState status, string detail)
        => $"connection:{name}: {status.Name} ({detail})";

    // Why: mirrors HealthMonitorService's worst-of rule — Unhealthy always wins (most severe), then
    // Degraded, then Healthy. Comparing TypeOption Ids (not string names) keeps this StringComparison-free.
    private static IHealthState WorseOf(IHealthState current, IHealthState candidate)
    {
        if (candidate.Id == UnhealthyState.Id || current.Id == UnhealthyState.Id)
            return UnhealthyState;
        if (candidate.Id == DegradedState.Id || current.Id == DegradedState.Id)
            return DegradedState;
        return HealthyState;
    }
}
