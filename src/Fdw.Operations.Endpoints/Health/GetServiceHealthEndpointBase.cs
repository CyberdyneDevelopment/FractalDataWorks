using System;
using System.Threading;
using System.Threading.Tasks;
using FastEndpoints;
using Fdw.Services.Abstractions.Health.Monitoring;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Fdw.Services.HealthChecks.Monitoring;

using Fdw.Services.Abstractions;

namespace Fdw.Operations.Endpoints.Health;

/// <summary>
/// Abstract endpoint that returns the current health snapshot for a specific service.
/// </summary>
public abstract class GetServiceHealthEndpointBase : Endpoint<ServiceHealthRequest, ServiceHealthSnapshot>
{
    private readonly IHealthMonitorProvider _monitors;
    private readonly IHealthMonitorConfigurationProvider _monitorConfigurations;
    private readonly ILogger _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="GetServiceHealthEndpointBase"/> class.
    /// </summary>
    /// <param name="monitors">The health monitor domain provider.</param>
    /// <param name="selection">The host's configured health monitor selector.</param>
    /// <param name="logger">The logger instance.</param>
    protected GetServiceHealthEndpointBase(
        IHealthMonitorProvider monitors,
        IHealthMonitorConfigurationProvider monitorConfigurations,
        ILogger<GetServiceHealthEndpointBase>? logger)
    {
        _monitors = monitors;
        _monitorConfigurations = monitorConfigurations;
        _logger = logger ?? NullLogger<GetServiceHealthEndpointBase>.Instance;
    }

    /// <summary>Configures the endpoint route, policies, and OpenAPI metadata.</summary>
    public override void Configure()
    {
        Get("/health/services/{Name}");
        Policies("authenticated");
        Summary(s => s.Summary = "Get service health snapshot");
        ConfigureEndpoint();
    }

    /// <summary>Override to add tags or additional endpoint configuration.</summary>
    protected virtual void ConfigureEndpoint() { }

    /// <summary>Retrieves the current health snapshot for the specified service.</summary>
    public override async Task HandleAsync(ServiceHealthRequest req, CancellationToken ct)
    {
        OperationsEndpointLog.GettingServiceHealth(_logger, req.Name);

        try
        {
            var monitorResult = await _monitors.Get(await SelectedMonitorName(ct).ConfigureAwait(false), ct).ConfigureAwait(false);

            if (!monitorResult.IsSuccess || monitorResult.Value is null)
            {
                OperationsEndpointLog.ServiceNotFound(_logger, req.Name);
                await Send.NotFoundAsync(ct).ConfigureAwait(false);
                return;
            }

            var result = await monitorResult.Value.GetServiceHealth(req.Name, ct).ConfigureAwait(false);

            if (!result.IsSuccess)
            {
                OperationsEndpointLog.ServiceNotFound(_logger, req.Name);
                await Send.NotFoundAsync(ct).ConfigureAwait(false);
                return;
            }

            await Send.OkAsync(result.Value!, ct).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            OperationsEndpointLog.GetSystemHealthFailed(_logger, ex.Message);
            AddError("Failed to retrieve service health");
            await Send.ErrorsAsync(500, ct).ConfigureAwait(false);
        }
    }

    // Why a helper: the monitor rows are shared and the selection is this host's, so the name has
    // to be read rather than held.
    private async Task<string> SelectedMonitorName(CancellationToken ct)
    {
        // Which monitor this host reports to is the HealthMonitor domain row, the same way every
        // other domain names its implementation.
        var result = await _monitorConfigurations.Get(ct).ConfigureAwait(false);
        if (result.IsFailure || result.Value is null)
        {
            throw new InvalidOperationException(
                "The HealthMonitor domain could not be read. This host does not know which monitor to report to.");
        }

        // Exactly one, refusing both zero and more than one: which monitor this host reports to is
        // the decision the row exists to record, so picking by order would make it depend on read order.
        if (result.Value.Count != 1)
        {
            throw new InvalidOperationException(
                $"The HealthMonitor domain must hold exactly one current row; found {result.Value.Count}. "
                + "This host does not know which monitor to report to.");
        }

        return result.Value[0].Name;
    }
}
