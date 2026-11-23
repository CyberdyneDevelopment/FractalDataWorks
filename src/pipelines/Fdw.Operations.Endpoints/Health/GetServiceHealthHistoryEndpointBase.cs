using System;
using System.Collections.Generic;
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
/// Abstract endpoint that returns health check history for a service over a time window.
/// </summary>
public abstract class GetServiceHealthHistoryEndpointBase : Endpoint<ServiceHealthHistoryRequest, IReadOnlyList<HealthCheckPoint>>
{
    private readonly IHealthMonitorProvider _monitors;
    private readonly IHealthMonitorConfigurationProvider _monitorConfigurations;
    private readonly ILogger _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="GetServiceHealthHistoryEndpointBase"/> class.
    /// </summary>
    /// <param name="monitors">The health monitor domain provider.</param>
    /// <param name="monitorConfigurations">The health monitor configuration provider.</param>
    /// <param name="logger">The logger instance.</param>
    protected GetServiceHealthHistoryEndpointBase(
        IHealthMonitorProvider monitors,
        IHealthMonitorConfigurationProvider monitorConfigurations,
        ILogger<GetServiceHealthHistoryEndpointBase>? logger)
    {
        _monitors = monitors;
        _monitorConfigurations = monitorConfigurations;
        _logger = logger ?? NullLogger<GetServiceHealthHistoryEndpointBase>.Instance;
    }

    /// <summary>Configures the endpoint route, policies, and OpenAPI metadata.</summary>
    public override void Configure()
    {
        Get("/health/services/{Name}/history");
        AllowAnonymous();
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
            await Send.ResultAsync(ResultHttpStatusMapper.Answer(GenericResult.Failure(OperationsEndpointLog.GetServiceHealthHistoryInvalidWindowFormatUseFormat(_logger)), HttpContext)).ConfigureAwait(false);
            return;
        }

        try
        {
            var monitorResult = await ReportingMonitor(ct).ConfigureAwait(false);

            if (monitorResult.IsFailure)
            {
                OperationsEndpointLog.GetHealthHistoryFailed(_logger, req.Name, monitorResult.NoValueReason().Message);
                await Send.ResultAsync(ResultHttpStatusMapper.Answer(monitorResult, HttpContext)).ConfigureAwait(false);
                return;
            }

            if (monitorResult.Value is null)
            {
                OperationsEndpointLog.GetHealthHistoryFailed(_logger, req.Name, monitorResult.NoValueReason().Message);
                await Send.ResultAsync(ResultHttpStatusMapper.Answer(GenericResult.Failure(OperationsEndpointLog.GetServiceHealthHistoryReturnedNothing(_logger)), HttpContext)).ConfigureAwait(false);
                return;
            }

            var result = await monitorResult.Value.GetHealthHistory(req.Name, window, ct).ConfigureAwait(false);

            if (!result.IsSuccess)
            {
                OperationsEndpointLog.GetHealthHistoryFailed(_logger, req.Name, result.FailureReason().Message);
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
            OperationsEndpointLog.GetHealthHistoryFailed(_logger, req.Name, ex.Message);
            await Send.ResultAsync(ResultHttpStatusMapper.Answer(GenericResult.Failure(OperationsEndpointLog.GetServiceHealthHistoryFaulted(_logger)), HttpContext)).ConfigureAwait(false);
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

        var windowUnit = HealthWindowUnits.BySymbol(unit);
        if (ReferenceEquals(windowUnit, HealthWindowUnits.NotFound))
            return false;

        window = windowUnit.Window(value);
        return window > TimeSpan.Zero;
    }

    // The monitor this host reports to, resolved by the one mechanism the four health endpoints share (FDW-6466): a failed read
    // comes back as it failed, with its own message and code.
    private Task<IGenericResult<IHealthMonitorService>> ReportingMonitor(CancellationToken ct)
        => HealthMonitorSelection.Reporting(_monitors, _monitorConfigurations, Resolve<IAuthenticationContextAccessor>(), _logger, ct);
}
