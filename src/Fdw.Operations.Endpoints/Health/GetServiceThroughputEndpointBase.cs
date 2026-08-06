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
/// Abstract endpoint that returns throughput data for a service over a time window.
/// </summary>
public abstract class GetServiceThroughputEndpointBase : Endpoint<ServiceThroughputRequest, ThroughputData>
{
    private readonly IHealthMonitorProvider _monitors;
    private readonly IOptions<HealthMonitorSelectionOptions> _selection;
    private readonly ILogger _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="GetServiceThroughputEndpointBase"/> class.
    /// </summary>
    /// <param name="monitors">The health monitor domain provider.</param>
    /// <param name="selection">The host's configured health monitor selector.</param>
    /// <param name="logger">The logger instance.</param>
    protected GetServiceThroughputEndpointBase(
        IHealthMonitorProvider monitors,
        IOptions<HealthMonitorSelectionOptions> selection,
        ILogger<GetServiceThroughputEndpointBase>? logger)
    {
        _monitors = monitors;
        _selection = selection;
        _logger = logger ?? NullLogger<GetServiceThroughputEndpointBase>.Instance;
    }

    /// <summary>Configures the endpoint route, policies, and OpenAPI metadata.</summary>
    public override void Configure()
    {
        Get("/health/services/{Name}/throughput");
        Policies("authenticated");
        Summary(s => s.Summary = "Get service throughput data");
        ConfigureEndpoint();
    }

    /// <summary>Override to add tags or additional endpoint configuration.</summary>
    protected virtual void ConfigureEndpoint() { }

    /// <summary>Retrieves throughput data for the specified service and time window.</summary>
    public override async Task HandleAsync(ServiceThroughputRequest req, CancellationToken ct)
    {
        OperationsEndpointLog.GettingServiceThroughput(_logger, req.Name, req.Window);

        if (!TryParseWindow(req.Window, out var window))
        {
            OperationsEndpointLog.InvalidWindowFormat(_logger, req.Window);
            AddError("Invalid window format. Use format like '5m', '1h', '24h'.");
            await Send.ErrorsAsync(400, ct).ConfigureAwait(false);
            return;
        }

        try
        {
            var monitorResult = await _monitors.Get(_selection.Value.Name, ct).ConfigureAwait(false);

            if (!monitorResult.IsSuccess || monitorResult.Value is null)
            {
                OperationsEndpointLog.GetThroughputFailed(_logger, req.Name, monitorResult.CurrentMessage ?? "Unknown error");
                AddError("Failed to retrieve throughput data");
                await Send.ErrorsAsync(500, ct).ConfigureAwait(false);
                return;
            }

            var result = await monitorResult.Value.GetThroughput(req.Name, window, ct).ConfigureAwait(false);

            if (!result.IsSuccess)
            {
                OperationsEndpointLog.GetThroughputFailed(_logger, req.Name, result.CurrentMessage ?? "Unknown error");
                AddError("Failed to retrieve throughput data");
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
            OperationsEndpointLog.GetThroughputFailed(_logger, req.Name, ex.Message);
            AddError("Failed to retrieve throughput data");
            await Send.ErrorsAsync(500, ct).ConfigureAwait(false);
        }
    }

    /// <summary>Parses a time window string like '5m', '1h', '24h' into a <see cref="TimeSpan"/>.</summary>
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
}
