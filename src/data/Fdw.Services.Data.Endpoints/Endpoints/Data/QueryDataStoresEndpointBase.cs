using Fdw.Services.Data.Configuration;
using Fdw.Web.RestEndpoints.Query;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Data.Endpoints;

/// <summary>
/// Base endpoint for querying data stores: <c>POST /datastores/query</c>.
/// </summary>
/// <remarks>
/// The data stores list, asked for by filter, order, page and field: a picker that wants only ids and names,
/// a page with its total. It answers from the data stores domain table, so a store's paths, containers and
/// fields are never read. The policy is the list's own, <c>datastores:read</c>. The fields it accepts are
/// declared by the endpoint that registers it (<see cref="QueryDomainEndpointBase{TDomain}.QueryableFields"/>).
/// A query answers a store's <c>connectionId</c> and can filter on it, but never its connection's NAME: that is
/// the connection domain's, which only a caller who may read connections is shown, so the list composes it and
/// a query does not.
/// </remarks>
public abstract class QueryDataStoresEndpointBase : QueryDomainEndpointBase<DataStoreDomainConfiguration>
{
    /// <summary>Initializes a new instance of the <see cref="QueryDataStoresEndpointBase"/> class.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="dataStoreProvider">Reads the page from the data stores domain table.</param>
    protected QueryDataStoresEndpointBase(ILogger logger, DataStoreConfigurationProvider dataStoreProvider)
        : base(logger, dataStoreProvider)
    {
    }

    /// <summary>Gets the resource name used for the route and the default policy: <c>/datastores/query</c>, <c>datastores:read</c>.</summary>
    protected override string ResourceName => "datastores";
}
