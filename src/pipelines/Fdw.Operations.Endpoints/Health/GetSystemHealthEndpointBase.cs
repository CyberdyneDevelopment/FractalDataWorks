using System;
using System.Threading;
using System.Threading.Tasks;
using FastEndpoints;
using Fdw.Messages;
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
/// Abstract endpoint that returns the current system health snapshot.
/// </summary>
public abstract class GetSystemHealthEndpointBase : EndpointWithoutRequest<SystemHealthSnapshot>
{
    private readonly IHealthMonitorProvider _monitors;
    private readonly IHealthMonitorConfigurationProvider _monitorConfigurations;
    private readonly ILogger _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="GetSystemHealthEndpointBase"/> class.
    /// </summary>
    /// <param name="monitors">The health monitor domain provider.</param>
    /// <param name="monitorConfigurations">The health monitor configuration provider.</param>
    /// <param name="logger">The logger instance.</param>
    protected GetSystemHealthEndpointBase(
        IHealthMonitorProvider monitors,
        IHealthMonitorConfigurationProvider monitorConfigurations,
        ILogger<GetSystemHealthEndpointBase>? logger)
    {
        _monitors = monitors;
        _monitorConfigurations = monitorConfigurations;
        _logger = logger ?? NullLogger<GetSystemHealthEndpointBase>.Instance;
    }

    /// <summary>Configures the endpoint route, policies, and OpenAPI metadata.</summary>
    public override void Configure()
    {
        Get("/health/system");
        AllowAnonymous();
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
            var monitorResult = await ReportingMonitor(ct).ConfigureAwait(false);

            if (monitorResult.IsFailure)
            {
                OperationsEndpointLog.GetSystemHealthFailed(_logger, Describe(monitorResult.FailureReason()));
                await Send.ResultAsync(ResultHttpStatusMapper.Answer(monitorResult, HttpContext)).ConfigureAwait(false);
                return;
            }

            if (monitorResult.Value is null)
            {
                OperationsEndpointLog.GetSystemHealthFailed(_logger, monitorResult.NoValueReason().Message);
                await Send.ResultAsync(ResultHttpStatusMapper.Answer(GenericResult.Failure(OperationsEndpointLog.GetSystemHealthReturnedNothing(_logger)), HttpContext)).ConfigureAwait(false);
                return;
            }

            var result = await monitorResult.Value.GetSystemHealth(ct).ConfigureAwait(false);

            if (!result.IsSuccess)
            {
                OperationsEndpointLog.GetSystemHealthFailed(_logger, result.FailureReason().Message);
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
            await Send.ResultAsync(ResultHttpStatusMapper.Answer(GenericResult.Failure(OperationsEndpointLog.GetSystemHealthFaulted(_logger)), HttpContext)).ConfigureAwait(false);
        }
    }

    // The monitor this host reports to, resolved by the one mechanism the four health endpoints share (FDW-6466): a failed read
    // comes back as it failed, with its own message and code.
    private Task<IGenericResult<IHealthMonitorService>> ReportingMonitor(CancellationToken ct)
        => HealthMonitorSelection.Reporting(_monitors, _monitorConfigurations, Resolve<IAuthenticationContextAccessor>(), _logger, ct);

    // The message and its code, so the line in the log names what the answer names.
    private static string Describe(IGenericMessage message)
        => string.IsNullOrEmpty(message.Code) ? message.Message : $"{message.Code}: {message.Message}";
}
