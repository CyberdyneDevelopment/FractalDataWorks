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
    /// <param name="monitorConfigurations">The health monitor configuration provider.</param>
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
        AllowAnonymous();
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
            var monitorResult = await ReportingMonitor(ct).ConfigureAwait(false);

            // A monitor that could not be resolved is answered with why (FDW-6466); ServiceNotFound is for a service the monitor does not know.
            if (monitorResult.IsFailure)
            {
                await Send.ResultAsync(ResultHttpStatusMapper.Answer(monitorResult, HttpContext)).ConfigureAwait(false);
                return;
            }

            if (monitorResult.Value is null)
            {
                await Send.ResultAsync(ResultHttpStatusMapper.Answer(GenericResult.Failure(monitorResult.NoValueReason()), HttpContext)).ConfigureAwait(false);
                return;
            }

            var result = await monitorResult.Value.GetServiceHealth(req.Name, ct).ConfigureAwait(false);

            // The monitor's own failure is the answer (FDW-6466): its not-found for a service it does not know is still a not found, and
            // any other failure says what it was.
            if (!result.IsSuccess)
            {
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
            OperationsEndpointLog.GetSystemHealthFailed(_logger, ex.Message);
            await Send.ResultAsync(ResultHttpStatusMapper.Answer(GenericResult.Failure(OperationsEndpointLog.GetServiceHealthFaulted(_logger)), HttpContext)).ConfigureAwait(false);
        }
    }

    // The monitor this host reports to, resolved by the one mechanism the four health endpoints share (FDW-6466): a failed read
    // comes back as it failed, with its own message and code.
    private Task<IGenericResult<IHealthMonitorService>> ReportingMonitor(CancellationToken ct)
        => HealthMonitorSelection.Reporting(_monitors, _monitorConfigurations, Resolve<IAuthenticationContextAccessor>(), _logger, ct);
}
