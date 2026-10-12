using Fdw.Services.Data;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Data.Endpoints;

/// <summary>
/// Base endpoint for listing the containers of one path of a data store, one page at a time (FDW-1194).
/// </summary>
/// <remarks>
/// <c>GET /datastores/{Name}/paths/{PathId}/containers?q=&amp;kind=&amp;skip=&amp;take=&amp;total=true</c>: the store's containers
/// search, kept to the path the route names. The items and the paging are the same.
/// </remarks>
public abstract class ListDataStorePathContainersEndpointBase : SearchDataStoreContainersEndpointBase
{
    /// <summary>Initializes a new instance of the <see cref="ListDataStorePathContainersEndpointBase"/> class.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="dataStoreProvider">Reads the store's paths and their containers.</param>
    protected ListDataStorePathContainersEndpointBase(
        ILogger<ListDataStorePathContainersEndpointBase> logger, DataStoreConfigurationProvider dataStoreProvider)
        : base(logger, dataStoreProvider)
    {
    }

    /// <inheritdoc />
    protected override string Route => $"/{ResourceName}/{{Name}}/paths/{{PathId}}/containers";

    /// <inheritdoc />
    protected override string EndpointSummary => "List the containers of a data store path";

    /// <inheritdoc />
    protected override bool PathInRoute => true;
}
