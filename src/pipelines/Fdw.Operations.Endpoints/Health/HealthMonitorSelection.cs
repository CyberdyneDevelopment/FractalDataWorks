using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.Services.Abstractions;
using Fdw.Services.Abstractions.Health.Monitoring;
using Fdw.Services.Authentication.Abstractions.Security;
using Microsoft.Extensions.Logging;

namespace Fdw.Operations.Endpoints.Health;

/// <summary>
/// Resolves the health monitor this host reports to, for the four health endpoints (FDW-6466).
/// </summary>
/// <remarks>
/// A read that failed is passed on as it failed, with its own message and code: an endpoint that answered it with a text of its own
/// named a different cause (a denied SELECT read as "does not know which monitor"). "Does not know which monitor to report to" is
/// said only for a read that succeeded and named none, or several, each with its own coded message.
/// </remarks>
internal static class HealthMonitorSelection
{
    /// <summary>Resolves the monitor the HealthMonitor domain names, under a system elevation (FDW-1098).</summary>
    /// <param name="monitors">The health monitor domain provider.</param>
    /// <param name="monitorConfigurations">The health monitor configuration provider.</param>
    /// <param name="accessor">The accessor whose context is elevated for the two reads.</param>
    /// <param name="logger">The logger the coded messages are written through.</param>
    /// <param name="ct">A token to cancel the reads.</param>
    /// <returns>The monitor; the failed read's own failure; or a coded failure when the domain names none or several.</returns>
    /// <remarks>
    /// Why under a system elevation: which monitor this host reports to, and that monitor's own row, are this host's configuration,
    /// not the caller's. The caller is usually an anonymous probe, and without the elevation both reads opened their connection as
    /// the deny principal (event 11067) on every cold read. Only the reads are elevated: the monitor's checks elevate for themselves,
    /// and nothing read here is returned.
    /// </remarks>
    internal static async Task<IGenericResult<IHealthMonitorService>> Reporting(
        IHealthMonitorProvider monitors,
        IHealthMonitorConfigurationProvider monitorConfigurations,
        IAuthenticationContextAccessor accessor,
        ILogger logger,
        CancellationToken ct)
    {
        using var elevation = new SystemAuthenticationContextScope(accessor);
        var name = await SelectedName(monitorConfigurations, logger, ct).ConfigureAwait(false);
        return name.IsFailure
            ? name.ToNewResult<IHealthMonitorService>()
            : await monitors.Get(name.Value!, ct).ConfigureAwait(false);
    }

    // Which monitor this host reports to is the HealthMonitor domain row, the same way every other domain names its
    // implementation. Exactly one, refusing both zero and more than one: which monitor this host reports to is the decision
    // the row exists to record, so picking by order would make it depend on read order.
    private static async Task<IGenericResult<string>> SelectedName(
        IHealthMonitorConfigurationProvider monitorConfigurations,
        ILogger logger,
        CancellationToken ct)
    {
        var result = await monitorConfigurations.Get(ct).ConfigureAwait(false);
        if (result.IsFailure)
        {
            return result.ToNewResult<string>();
        }

        if (result.Value is null)
        {
            return GenericResult<string>.Failure(result.NoValueReason());
        }

        return result.Value.Count switch
        {
            0 => GenericResult<string>.Failure(OperationsEndpointLog.HealthMonitorNoneSelected(logger)),
            1 => GenericResult<string>.Success(result.Value[0].Name),
            _ => GenericResult<string>.Failure(OperationsEndpointLog.HealthMonitorSeveralSelected(logger, result.Value.Count)),
        };
    }
}
