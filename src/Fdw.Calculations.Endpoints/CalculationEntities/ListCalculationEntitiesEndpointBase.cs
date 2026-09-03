using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.Web.RestEndpoints.Crud;
using Microsoft.Extensions.Logging;

namespace Fdw.Calculations.Endpoints.CalculationEntities;

/// <summary>
/// Base endpoint for listing calculation entities.
/// Route: GET /calculation-entities
/// </summary>
public abstract class ListCalculationEntitiesEndpointBase : CrudListEndpointBase<CalculationEntitySummaryDto>
{
    /// <summary>Initializes a new instance of the <see cref="ListCalculationEntitiesEndpointBase"/> class.</summary>
    protected ListCalculationEntitiesEndpointBase(ILogger<ListCalculationEntitiesEndpointBase> logger) : base(logger)
    {
    }

    /// <inheritdoc/>
    protected override string ResourceName => "calculation-entities";

    /// <inheritdoc/>
    protected override string EndpointSummary => "List all calculation entities";

    /// <inheritdoc/>
    protected override string EndpointDescription => "Returns a list of all configured calculation entities.";
}
