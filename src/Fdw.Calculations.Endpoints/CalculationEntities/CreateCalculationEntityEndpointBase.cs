using Fdw.Web.RestEndpoints.Crud;

namespace Fdw.Calculations.Endpoints.CalculationEntities;

/// <summary>
/// Base endpoint for creating a calculation entity.
/// Route: POST /calculation-entities
/// </summary>
public abstract class CreateCalculationEntityEndpointBase : CrudCreateEndpointBase<CreateCalculationEntityRequest, CalculationEntityDetailDto>
{
    /// <inheritdoc/>
    protected override string ResourceName => "calculation-entities";

    /// <inheritdoc/>
    protected override string GetResourceName(CreateCalculationEntityRequest request) => request.Name;
}
