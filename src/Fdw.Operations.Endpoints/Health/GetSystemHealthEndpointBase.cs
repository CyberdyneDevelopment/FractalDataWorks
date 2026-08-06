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
/// Abstract endpoint that returns the current system health snapshot.
/// </summary>
public abstract class GetSystemHealthEndpointBase : EndpointWithoutRequest<SystemHealthSnapshot>
{
    private readonly IHealthMonitorProvider _monitors;
    private readonly IOptions<HealthMonitorSelectionOptions> _selection;
    private readonly ILogger _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="GetSystemHealthEndpointBase"/> class.
    /// </summary>
    /// <param name="monitors">The health monitor domain provider.</param>
    /// <param name="selection">The host's configured health monitor selector.</param>
    /// <param name="logger">The logger instance.</param>
    protected GetSystemHealthEndpointBase(
        IHealthMonitorProvider monitors,
        IOptions<HealthMonitorSelectionOptions> selection,
        ILogger<GetSystemHealthEndpointBase>? logger)
    {
        _monitors = monitors;
        _selection = selection;
        _logger = logger ?? NullLogger<GetSystemHealthEndpointBase>.Instance;
    }

    /// <summary>Configures the endpoint route, policies, and OpenAPI metadata.</summary>
    public override void Configure()
    {
        Get("/health/system");
        Policies("authenticated");
        Summary(s => s.Summary = "Get system health snapshot");
        ConfigureEndpoint();
    }

    /// <summary>Override to add tags or additional endpoint configuration.</summary>
    protected virtual void ConfigureEndpoint() { }

    /// <summary>Retrieves the current system health snapshot.</summary>
    public override async Task HandleAsync(CancellationToken ct)
    {
        OperationsEndpointLog.GettingSystemHealth(_logger);

        try
        {
            var monitorResult = await _monitors.Get(_selection.Value.Name, ct).ConfigureAwait(false);

            if (!monitorResult.IsSuccess || monitorResult.Value is null)
            {
                OperationsEndpointLog.GetSystemHealthFailed(_logger, monitorResult.CurrentMessage ?? "Unknown error");
                AddError("Failed to retrieve system health");
                await Send.ErrorsAsync(500, ct).ConfigureAwait(false);
                return;
            }

            var result = await monitorResult.Value.GetSystemHealth(ct).ConfigureAwait(false);

            if (!result.IsSuccess)
            {
                OperationsEndpointLog.GetSystemHealthFailed(_logger, result.CurrentMessage ?? "Unknown error");
                AddError("Failed to retrieve system health");
                await Send.ErrorsAsync(500, ct).ConfigureAwait(false);
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
            AddError("Failed to retrieve system health");
            await Send.ErrorsAsync(500, ct).ConfigureAwait(false);
        }
    }
}
