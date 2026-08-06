using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FastEndpoints;
using Fdw.Services.SecretManagers;
using Fdw.Services.SecretManagers.Abstractions;
using Fdw.Services.SecretManagers.Endpoints.Logging;
using Fdw.Web.RestEndpoints.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Services.SecretManagers.Endpoints;

/// <summary>
/// Base endpoint for listing all configured secret managers.
/// Route: GET /secret-managers
/// </summary>
public abstract class ListSecretManagersEndpointBase : EndpointWithoutRequest<PaginatedResponse<SecretManagerSummaryResponse>>
{
    private readonly SecretManagerConfigurationProvider _configProvider;
    private readonly ILogger<ListSecretManagersEndpointBase> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="ListSecretManagersEndpointBase"/> class.
    /// </summary>
    protected ListSecretManagersEndpointBase(
        SecretManagerConfigurationProvider configProvider,
        ILogger<ListSecretManagersEndpointBase> logger)
    {
        _configProvider = configProvider;
        _logger = logger ?? NullLogger<ListSecretManagersEndpointBase>.Instance;
    }

    /// <inheritdoc/>
    public override void Configure()
    {
        Get("/secret-managers");
        Policies("secretmanagers:read");
        Summary(s =>
        {
            s.Summary = "List secret managers";
            s.Description = "Returns all configured secret managers with their type and description.";
        });
        ConfigureEndpoint();
    }

    /// <summary>Override to add Tags or other endpoint configuration.</summary>
    protected virtual void ConfigureEndpoint() { }

    /// <inheritdoc/>
    public override async Task HandleAsync(CancellationToken ct)
    {
        SecretManagerEndpointLog.ListingSecretManagers(_logger);

        var getAllResult = await _configProvider.Get(ct).ConfigureAwait(false);
        var allConfigs = getAllResult.IsSuccess && getAllResult.Value is not null
            ? getAllResult.Value
            : [];
        var configs = allConfigs
            .Select(c => new SecretManagerSummaryResponse
            {
                Name = c.Name,
                SecretManagerType = c.SecretManagerType,
                Description = c.Description
            })
            .ToList();

        SecretManagerEndpointLog.SecretManagersListed(_logger, configs.Count);
        // Why: Newman/clients expect a paginated envelope {items, skip, take, totalCount, hasMore}
        // matching the response shape from /pipelines and other Crud-list endpoints.
        await Send.OkAsync(PaginatedResponse<SecretManagerSummaryResponse>.Create(configs, 0, configs.Count, configs.Count), ct).ConfigureAwait(false);
    }
}
