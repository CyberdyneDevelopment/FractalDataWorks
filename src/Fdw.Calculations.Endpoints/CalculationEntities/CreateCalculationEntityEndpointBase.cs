using Fdw.Web.RestEndpoints.Crud;
using Microsoft.Extensions.Logging;

namespace Fdw.Calculations.Endpoints.CalculationEntities;

/// <summary>
/// Base endpoint for creating a calculation entity.
/// Route: POST /calculation-entities
/// </summary>
public abstract class CreateCalculationEntityEndpointBase : CrudCreateEndpointBase<CreateCalculationEntityRequest, CalculationEntityDetailDto>
{
    /// <summary>Initializes a new instance of the <see cref="CreateCalculationEntityEndpointBase"/> class.</summary>
    protected CreateCalculationEntityEndpointBase(ILogger<CreateCalculationEntityEndpointBase> logger) : base(logger)
    {
    }

    /// <inheritdoc/>
    protected override string ResourceName => "calculation-entities";

    /// <inheritdoc/>
    protected override string GetResourceName(CreateCalculationEntityRequest request) => request.Name;
}
