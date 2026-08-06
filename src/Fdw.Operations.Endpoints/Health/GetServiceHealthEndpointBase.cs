using System;
using System.Threading;
using System.Threading.Tasks;
using FastEndpoints;
using Fdw.Services.Abstractions.Health.Monitoring;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Fdw.Operations.Endpoints.Health;

/// <summary>
/// Abstract endpoint that returns the current health snapshot for a specific service.
/// </summary>
public abstract class GetServiceHealthEndpointBase : Endpoint<ServiceHealthRequest, ServiceHealthSnapshot>
{
    private readonly IHealthMonitorProvider _monitors;
    private readonly IOptions<HealthMonitorSelectionOptions> _selection;
    private readonly ILogger _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="GetServiceHealthEndpointBase"/> class.
    /// </summary>
    /// <param name="monitors">The health monitor domain provider.</param>
    /// <param name="selection">The host's configured health monitor selector.</param>
    /// <param name="logger">The logger instance.</param>
    protected GetServiceHealthEndpointBase(
        IHealthMonitorProvider monitors,
        IOptions<HealthMonitorSelectionOptions> selection,
        ILogger<GetServiceHealthEndpointBase>? logger)
    {
        _monitors = monitors;
        _selection = selection;
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
            var monitorResult = await _monitors.Get(_selection.Value.Name, ct).ConfigureAwait(false);

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
}
