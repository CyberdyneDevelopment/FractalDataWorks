using System.Threading;
using System.Threading.Tasks;
using FastEndpoints;
using Fdw.Data.DataStores.Abstractions;
using Fdw.Results;
using Fdw.Services.Data.Abstractions;
using Microsoft.Extensions.Logging;
using Fdw.Services.Data.Clients.Models;

namespace Fdw.Services.Data.Endpoints;

/// <summary>
/// Generic base endpoint for discovering a data store to discover its schema.
/// </summary>
public abstract class DiscoverDataStoreEndpointBase : Endpoint<DiscoverDataStoreRequest, DiscoveryResultPayload>
{
    private readonly IDataStoreProvider _dataStoreProvider;
    private readonly ILogger _logger;

    /// <inheritdoc />
    protected DiscoverDataStoreEndpointBase(
        IDataStoreProvider dataStoreProvider,
        ILoggerFactory loggerFactory)
    {
        _dataStoreProvider = dataStoreProvider;
        _logger = loggerFactory.CreateLogger(GetType());
    }

    /// <summary>Gets the resource name used for route and policy generation.</summary>
    protected virtual string ResourceName => "datastores";

    /// <summary>Gets the authorization policy for write access.</summary>
    protected virtual string WritePolicy => $"{ResourceName}:write";

    /// <summary>Configures the endpoint route, policies, and OpenAPI metadata.</summary>
    public override void Configure()
    {
        Post($"/{ResourceName}/-/discover");
#if DEVELOP
        AllowAnonymous();
#else
        Policies(WritePolicy);
#endif
        Summary(s =>
        {
            s.Summary = "Discover data store schema";
            s.Description = "Discovers the schema (paths, containers, fields) for a data store.";
        });
    }

    /// <summary>Handles the discovery request.</summary>
    public override async Task HandleAsync(DiscoverDataStoreRequest request, CancellationToken ct)
    {
        var result = await PerformDiscovery(request.Name, request.Refresh, ct).ConfigureAwait(false);
        if (result.IsFailure)
        {
            HttpContext.Response.StatusCode = 500;
            return;
        }

        await Send.OkAsync(result.Value!, ct).ConfigureAwait(false);
    }

    /// <summary>Performs the actual discovery operation. Override for implementation-specific logic.</summary>
    protected abstract Task<IGenericResult<DiscoveryResultPayload>> PerformDiscovery(
        string dataStoreName,
        bool refresh,
        CancellationToken ct);
}
