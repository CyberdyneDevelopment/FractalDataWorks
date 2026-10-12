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
using Fdw.Services.Abstractions;
using Fdw.Services.Abstractions.Health;
using Fdw.Services.Authentication.Abstractions.Security;
using Fdw.Services.Connections.Abstractions;
using Fdw.Services.Connections.Logging;

namespace Fdw.Services.Connections;

/// <summary>
/// Domain-level <see cref="IHealthCheckable"/> for the Connections domain. At check time, enumerates
/// every <c>conn.Connection</c> row with <see cref="IConnectionImplementationConfiguration.HealthCheckEnabled"/> set,
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
/// <para>
/// Why the check runs under a system elevation (FDW-1098): which connections exist and whether they
/// answer is a fact about the platform, not about whoever asked. The caller is usually an anonymous
/// probe (<c>GET /health/system</c>), so without it every read and every probe opened its connection
/// as the deny principal -- a warning (11067) per connection on every probe, and a check that silently
/// skipped every tenant-owned connection. <see cref="ConnectionHealthMonitorWorker"/> does the same
/// reads under the same elevation (FDW-767).
/// </para>
/// <para>
/// Why the description gives counts and no names: the health monitor keeps it as history that
/// <c>GET /health/services/{name}/history</c> serves anonymously. Under the elevation it covers every
/// connection, tenant-owned ones included, so naming them (or quoting a probe's error) would show an
/// anonymous caller what it could never read itself. Each failure is logged with its connection's name.
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
        // Scoped to this check alone: the accessor is an AsyncLocal, so the elevation covers the reads and
        // probes awaited below and ends with this call, never reaching the caller's own request.
        using var elevation = new SystemAuthenticationContextScope(
            serviceProvider.GetRequiredService<IAuthenticationContextAccessor>());

        return await Check(serviceProvider, cancellationToken).ConfigureAwait(false);
    }

    private async Task<IGenericResult<IHealthCheckResult>> Check(
        IServiceProvider serviceProvider,
        CancellationToken cancellationToken)
    {
        ConnectionsHealthLog.CheckStarting(_logger);
        var stopwatch = Stopwatch.StartNew();

        var connectionProvider = serviceProvider.GetRequiredService<IConnectionProvider>();

        // The rows alone: which connections are checked is decided by fields of each row, and ProbeOne
        // resolves each checked one by name, which reads it whole (FDW-970).
        var allResult = await serviceProvider.GetRequiredService<IConnectionConfigurationProvider>()
            .Get(ConfigurationLevels.RecordOnly, cancellationToken).ConfigureAwait(false);
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
        var statuses = new List<IHealthState>();

        foreach (var connection in checkedConnections)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var status = await ProbeOne(connection, connectionProvider, cancellationToken).ConfigureAwait(false);
            statuses.Add(status);
            worstStatus = WorseOf(worstStatus, status);
        }

        stopwatch.Stop();

        if (worstStatus.Id == HealthyState.Id)
            ConnectionsHealthLog.CheckCompleted(_logger, checkedConnections.Count, worstStatus.Name);
        else
            ConnectionsHealthLog.CheckCompletedUnhealthy(_logger, checkedConnections.Count, worstStatus.Name);

        return GenericResult<IHealthCheckResult>.Success(new HealthCheckResult
        {
            Status = worstStatus,
            Description = Describe(statuses),
            Duration = stopwatch.Elapsed
        });
    }

    private async Task<IHealthState> ProbeOne(
        IConnectionImplementationConfiguration connection,
        IConnectionProvider connectionProvider,
        CancellationToken cancellationToken)
    {
        // Why one connection that throws cannot sink the rest: this checks every enabled connection in
        // turn, and an exception used to fail the whole Connections check -- so one misconfigured
        // connection hid the state of all the others. It is reported unhealthy, and what it threw is logged.
        try
        {
            var getResult = await connectionProvider.Get(connection.Name, cancellationToken).ConfigureAwait(false);
            if (!getResult.IsSuccess || getResult.Value is null)
            {
                ConnectionsHealthLog.ConnectionResolutionFailed(
                    _logger, connection.Name, getResult.CurrentMessage ?? "Connection could not be resolved");
                return UnhealthyState;
            }

            if (getResult.Value is not ISupportsHealthProbe probe)
            {
                ConnectionsHealthLog.NoProbeCapability(_logger, connection.Name);
                return DegradedState;
            }

            var probeResult = await probe.Probe(cancellationToken).ConfigureAwait(false);
            if (!probeResult.IsSuccess)
            {
                ConnectionsHealthLog.ProbeFailed(_logger, connection.Name, probeResult.CurrentMessage ?? "Probe failed");
                return UnhealthyState;
            }

            return HealthyState;
        }
        catch (Exception ex) when (!cancellationToken.IsCancellationRequested)
        {
            ConnectionsHealthLog.ProbeThrew(_logger, ex, connection.Name, ex.Message);
            return UnhealthyState;
        }
    }

    private static string Describe(List<IHealthState> statuses)
        => $"{statuses.Count} connection(s) checked: {statuses.Count(s => s.Id == HealthyState.Id)} healthy, "
           + $"{statuses.Count(s => s.Id == DegradedState.Id)} degraded, {statuses.Count(s => s.Id == UnhealthyState.Id)} unhealthy";

    private static IHealthState WorseOf(IHealthState current, IHealthState candidate)
    {
        if (candidate.Id == UnhealthyState.Id || current.Id == UnhealthyState.Id)
            return UnhealthyState;
        if (candidate.Id == DegradedState.Id || current.Id == DegradedState.Id)
            return DegradedState;
        return HealthyState;
    }
}
