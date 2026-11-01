using Fdw.Data.Abstractions;
using Fdw.Data.DataSets.Abstractions;
using Fdw.Web.RestEndpoints.Query;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Workspaces.Endpoints;

/// <summary>
/// Base endpoint for querying data views: <c>POST /data-views/query</c>.
/// </summary>
/// <remarks>
/// The data views list, asked for by filter, order, page and field. It answers from the variation domain table, held to
/// the data views (<see cref="DataViewScope"/>), so no view's implementation is read. The policy is the list's own,
/// <c>data-views:read</c>. The fields it accepts are declared by the endpoint that registers it
/// (<see cref="QueryDomainEndpointBase{TDomain}.QueryableFields"/>); a view's filters, encoding and chart type live on its
/// implementation and are not among them (FDW-1032).
/// </remarks>
public abstract class QueryDataViewsEndpointBase : QueryDomainEndpointBase<DataSetVariationDomainConfiguration>
{
    /// <summary>Initializes a new instance of the <see cref="QueryDataViewsEndpointBase"/> class.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="variations">Reads the page from the variation domain table.</param>
    protected QueryDataViewsEndpointBase(ILogger logger, IDataSetVariationConfigurationProvider variations)
        : base(logger, variations)
    {
    }

    /// <summary>Gets the resource name used for the route and the default policy: <c>/data-views/query</c>.</summary>
    protected override string ResourceName => "data-views";

    /// <inheritdoc />
    protected override IFilterExpression? ScopeFilter => DataViewScope.Filter;
}
