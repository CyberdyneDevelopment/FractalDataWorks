using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FastEndpoints;
using Fdw.Services.Connections.Abstractions;
using Fdw.Services.Connections.Endpoints.Logging;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Services.Connections.Endpoints;

/// <summary>
/// Base endpoint for retrieving connection health check history by connection name.
/// Resolves the connection via <see cref="ConnectionConfigurationProvider"/>, then delegates
/// to <see cref="IConnectionHealthService"/> for the persisted history.
/// </summary>
public abstract class GetConnectionHealthEndpointBase : Endpoint<ConnectionNameRequest, List<ConnectionHealthCheckDto>>
{
    // Why: ConnectionConfigurationProvider (dual-source) replaces IConnectionProvider.GetAllConnectionConfigurations()
    // which was removed. The provider merges system (ctrl) and user (cfg) connection configs.
    private readonly ConnectionConfigurationProvider _configProvider;
    private readonly IConnectionHealthService _healthService;
    private readonly ILogger<GetConnectionHealthEndpointBase> _logger;

    /// <inheritdoc />
    protected GetConnectionHealthEndpointBase(
        ConnectionConfigurationProvider configProvider,
        IConnectionHealthService healthService,
        ILogger<GetConnectionHealthEndpointBase> logger)
    {
        _configProvider = configProvider;
        _healthService = healthService;
        _logger = logger ?? NullLogger<GetConnectionHealthEndpointBase>.Instance;
    }

    /// <summary>Configures the endpoint route, policies, and OpenAPI metadata.</summary>
    public override void Configure()
    {
        Get("/connections/{Name}/health");
#if DEVELOP
        AllowAnonymous();
#else
        Policies("connections:read");
#endif
        Summary(s =>
        {
            s.Summary = "Get connection health check history";
            s.Description = "Returns recent health check results for a connection identified by name.";
        });
    }

    /// <summary>Resolves the connection by name and returns its health check history.</summary>
    public override async Task HandleAsync(ConnectionNameRequest req, CancellationToken ct)
    {
        // Why: resolve connection by name to get the logical Id needed for the history query;
        // ConnectionConfigurationProvider merges all registered connection types.
        var configResult = await _configProvider.Get(req.Name, ct).ConfigureAwait(false);

        if (!configResult.IsSuccess || configResult.Value is null)
        {
            ConnectionEndpointLog.HealthHistoryConnectionNotFound(_logger, req.Name);
            await Send.NotFoundAsync(ct).ConfigureAwait(false);
            return;
        }

        var config = configResult.Value;
        var result = await _healthService.GetHistory(config.Id, count: 20, ct).ConfigureAwait(false);

        if (!result.IsSuccess)
        {
            ConnectionEndpointLog.HealthHistoryLoadFailed(_logger, req.Name, result.CurrentMessage ?? "Unknown error");
            HttpContext.Response.StatusCode = 500;
            return;
        }

        var dtos = result.Value!
            .Select(r => new ConnectionHealthCheckDto
            {
                IsHealthy = r.IsHealthy,
                ResponseTimeMs = r.ResponseTimeMs,
                ErrorMessage = r.ErrorMessage,
                CheckedAt = r.CheckedAt,
                CheckedBy = r.CheckedBy
            })
            .ToList();

        await Send.OkAsync(dtos, ct).ConfigureAwait(false);
    }
}
