using Fdw.Services.Calculations.Configuration;
using Fdw.Web.RestEndpoints.Query;
using Microsoft.Extensions.Logging;
using Fdw.Services.Calculations;

namespace Fdw.Calculations.Endpoints.CalculationEntities;

/// <summary>
/// Base endpoint for querying calculation entities: <c>POST /calculation-entities/query</c>.
/// </summary>
/// <remarks>
/// The calculation entities list, asked for by filter, order, page and field. It answers from the calculation
/// entities domain table, so no calculation is composed. The policy is the list's own,
/// <c>calculation-entities:read</c>. The fields it accepts are declared by the endpoint that registers it
/// (<see cref="QueryDomainEndpointBase{TDomain}.QueryableFields"/>).
/// </remarks>
public abstract class QueryCalculationEntitiesEndpointBase : QueryDomainEndpointBase<CalculationEntityDomainConfiguration>
{
    /// <summary>Initializes a new instance of the <see cref="QueryCalculationEntitiesEndpointBase"/> class.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="provider">Reads the page from the calculation entities domain table.</param>
    protected QueryCalculationEntitiesEndpointBase(ILogger logger, ICalculationConfigurationProvider provider)
        : base(logger, provider)
    {
    }

    /// <summary>Gets the resource name used for the route and the default policy: <c>/calculation-entities/query</c>.</summary>
    protected override string ResourceName => "calculation-entities";
}
