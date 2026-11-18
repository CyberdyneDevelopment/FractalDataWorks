using System;
using System.Threading;
using System.Threading.Tasks;
using FastEndpoints;
using Fdw.Results;
using Fdw.Services.Authentication.Abstractions.Security;
using Fdw.Services.Abstractions.Health.Monitoring;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Fdw.Services.HealthChecks.Monitoring;

using Fdw.Services.Abstractions;
using Fdw.Web.RestEndpoints.ErrorMapping;

namespace Fdw.Operations.Endpoints.Health;

/// <summary>
/// Abstract endpoint that returns throughput data for a service over a time window.
/// </summary>
public abstract class GetServiceThroughputEndpointBase : Endpoint<ServiceThroughputRequest, ThroughputData>
{
    private readonly IHealthMonitorProvider _monitors;
    private readonly IHealthMonitorConfigurationProvider _monitorConfigurations;
    private readonly ILogger _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="GetServiceThroughputEndpointBase"/> class.
    /// </summary>
    /// <param name="monitors">The health monitor domain provider.</param>
    /// <param name="monitorConfigurations">The health monitor configuration provider.</param>
    /// <param name="logger">The logger instance.</param>
    protected GetServiceThroughputEndpointBase(
        IHealthMonitorProvider monitors,
        IHealthMonitorConfigurationProvider monitorConfigurations,
        ILogger<GetServiceThroughputEndpointBase>? logger)
    {
        _monitors = monitors;
        _monitorConfigurations = monitorConfigurations;
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
            await Send.ResultAsync(ResultHttpStatusMapper.Answer(GenericResult.Failure(OperationsEndpointLog.GetServiceThroughputInvalidWindowFormatUseFormat(_logger)), HttpContext)).ConfigureAwait(false);
            return;
        }

        try
        {
            var monitorResult = await ReportingMonitor(ct).ConfigureAwait(false);

            if (monitorResult.IsFailure)
            {
                OperationsEndpointLog.GetThroughputFailed(_logger, req.Name, monitorResult.NoValueReason().Message);
                await Send.ResultAsync(ResultHttpStatusMapper.Answer(monitorResult, HttpContext)).ConfigureAwait(false);
                return;
            }

            if (monitorResult.Value is null)
            {
                OperationsEndpointLog.GetThroughputFailed(_logger, req.Name, monitorResult.NoValueReason().Message);
                await Send.ResultAsync(ResultHttpStatusMapper.Answer(GenericResult.Failure(OperationsEndpointLog.GetServiceThroughputReturnedNothing(_logger)), HttpContext)).ConfigureAwait(false);
                return;
            }

            var result = await monitorResult.Value.GetThroughput(req.Name, window, ct).ConfigureAwait(false);

            if (!result.IsSuccess)
            {
                OperationsEndpointLog.GetThroughputFailed(_logger, req.Name, result.FailureReason().Message);
                await Send.ResultAsync(ResultHttpStatusMapper.Answer(result, HttpContext)).ConfigureAwait(false);
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
            await Send.ResultAsync(ResultHttpStatusMapper.Answer(GenericResult.Failure(OperationsEndpointLog.GetServiceThroughputFaulted(_logger)), HttpContext)).ConfigureAwait(false);
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

    // The monitor this host reports to, resolved by the one mechanism the four health endpoints share (FDW-6466): a failed read
    // comes back as it failed, with its own message and code.
    private Task<IGenericResult<IHealthMonitorService>> ReportingMonitor(CancellationToken ct)
        => HealthMonitorSelection.Reporting(_monitors, _monitorConfigurations, Resolve<IAuthenticationContextAccessor>(), _logger, ct);
}
