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
using Fdw.Web.RestEndpoints.Pagination;
using Microsoft.AspNetCore.Http;
using Fdw.Web.RestEndpoints.ErrorMapping;
using Fdw.Services.Authentication.Abstractions.Security;

namespace Fdw.Services.SecretManagers.Endpoints;

/// <summary>
/// Base endpoint for listing all configured secret managers.
/// Route: GET /secret-managers
/// </summary>
public abstract class ListSecretManagersEndpointBase : EndpointWithoutRequest<PaginatedResponse<SecretManagerSummaryResponse>>
{
    private readonly ISecretManagerConfigurationProvider _configProvider;
    private readonly ILogger<ListSecretManagersEndpointBase> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="ListSecretManagersEndpointBase"/> class.
    /// </summary>
    protected ListSecretManagersEndpointBase(
        ISecretManagerConfigurationProvider configProvider,
        ILogger<ListSecretManagersEndpointBase> logger)
    {
        _configProvider = configProvider;
        _logger = logger ?? NullLogger<ListSecretManagersEndpointBase>.Instance;
    }

    /// <inheritdoc/>
    public override void Configure()
    {
        Get("/secret-managers");
        // Secret managers are the platform's, owned by the system tenant: only a platform administrator lists, reads
        // or changes one. A tenant never touches a secret manager or a secret; a connection's credentials are set
        // through the connection (FDW-1086).
        Policies(AuthConstants.PlatformAdminPolicy);
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
                SecretManagerType = c.Implementation,
            })
            .ToList();

        SecretManagerEndpointLog.SecretManagersListed(_logger, configs.Count);
        var paging = PaginationQuery.Parse(HttpContext, _logger);
        if (paging.IsFailure)
        {
            var (statusCode, errorResponse) = ResultHttpStatusMapper.Map(paging, HttpContext);
            await Send.ResultAsync(TypedResults.Json(errorResponse, statusCode: statusCode)).ConfigureAwait(false);
            return;
        }

        await Send.OkAsync(paging.Value!.Apply(configs), ct).ConfigureAwait(false);
    }
}
