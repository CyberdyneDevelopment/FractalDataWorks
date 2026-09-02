using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FastEndpoints;
using Fdw.Services.Abstractions.Health.Monitoring;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Fdw.Services.HealthChecks.Monitoring;

namespace Fdw.Operations.Endpoints.Health;

/// <summary>
/// Abstract endpoint that returns health check history for a service over a time window.
/// </summary>
public abstract class GetServiceHealthHistoryEndpointBase : Endpoint<ServiceHealthHistoryRequest, IReadOnlyList<HealthCheckPoint>>
{
    private readonly IHealthMonitorProvider _monitors;
    private readonly HealthMonitorSelectionConfigurationProvider _selection;
    private readonly ILogger _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="GetServiceHealthHistoryEndpointBase"/> class.
    /// </summary>
    /// <param name="monitors">The health monitor domain provider.</param>
    /// <param name="selection">The host's configured health monitor selector.</param>
    /// <param name="logger">The logger instance.</param>
    protected GetServiceHealthHistoryEndpointBase(
        IHealthMonitorProvider monitors,
        HealthMonitorSelectionConfigurationProvider selection,
        ILogger<GetServiceHealthHistoryEndpointBase>? logger)
    {
        _monitors = monitors;
        _selection = selection;
        _logger = logger ?? NullLogger<GetServiceHealthHistoryEndpointBase>.Instance;
    }

    /// <summary>Configures the endpoint route, policies, and OpenAPI metadata.</summary>
    public override void Configure()
    {
        Get("/health/services/{Name}/history");
        Policies("authenticated");
        Summary(s => s.Summary = "Get service health check history");
        ConfigureEndpoint();
    }

    /// <summary>Override to add tags or additional endpoint configuration.</summary>
    protected virtual void ConfigureEndpoint() { }

    /// <summary>Retrieves health check history for the specified service and time window.</summary>
    public override async Task HandleAsync(ServiceHealthHistoryRequest req, CancellationToken ct)
    {
        OperationsEndpointLog.GettingServiceHealthHistory(_logger, req.Name, req.Window);

        if (!TryParseWindow(req.Window, out var window))
        {
            OperationsEndpointLog.InvalidWindowFormat(_logger, req.Window);
            AddError("Invalid window format. Use format like '1h', '24h', '7d'.");
            await Send.ErrorsAsync(400, ct).ConfigureAwait(false);
            return;
        }

        try
        {
            var monitorResult = await _monitors.Get(await SelectedMonitorName(ct).ConfigureAwait(false), ct).ConfigureAwait(false);

            if (!monitorResult.IsSuccess || monitorResult.Value is null)
            {
                OperationsEndpointLog.GetHealthHistoryFailed(_logger, req.Name, monitorResult.CurrentMessage ?? "Unknown error");
                AddError("Failed to retrieve health history");
                await Send.ErrorsAsync(500, ct).ConfigureAwait(false);
                return;
            }

            var result = await monitorResult.Value.GetHealthHistory(req.Name, window, ct).ConfigureAwait(false);

            if (!result.IsSuccess)
            {
                OperationsEndpointLog.GetHealthHistoryFailed(_logger, req.Name, result.CurrentMessage ?? "Unknown error");
                AddError("Failed to retrieve health history");
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
            OperationsEndpointLog.GetHealthHistoryFailed(_logger, req.Name, ex.Message);
            AddError("Failed to retrieve health history");
            await Send.ErrorsAsync(500, ct).ConfigureAwait(false);
        }
    }

    /// <summary>Parses a time window string like '1h', '24h', '7d' into a <see cref="TimeSpan"/>.</summary>
    protected static bool TryParseWindow(string input, out TimeSpan window)
    {
        window = TimeSpan.Zero;

        if (string.IsNullOrWhiteSpace(input) || input.Length < 2)
            return false;

        var unit = input[^1];
        if (!int.TryParse(input[..^1], System.Globalization.CultureInfo.InvariantCulture, out var value) || value <= 0)
            return false;

        window = unit switch
        {
            'm' => TimeSpan.FromMinutes(value),
            'h' => TimeSpan.FromHours(value),
            'd' => TimeSpan.FromDays(value),
            _ => TimeSpan.Zero
        };

        return window > TimeSpan.Zero;
    }

    // Why a helper: the monitor rows are shared and the selection is this host's, so the name has
    // to be read rather than held.
    private async Task<string> SelectedMonitorName(CancellationToken ct)
    {
        var result = await _selection.Get("HealthMonitorSelection", ct).ConfigureAwait(false);
        if (result.IsFailure || result.Value is null)
        {
            throw new InvalidOperationException(
                "HealthMonitorSelection is not configured. This host does not know which monitor to report to.");
        }

        return result.Value.MonitorName;
    }
}
