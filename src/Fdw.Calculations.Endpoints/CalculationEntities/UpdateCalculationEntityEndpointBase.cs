using Fdw.Web.RestEndpoints.Crud;

namespace Fdw.Calculations.Endpoints.CalculationEntities;

/// <summary>
/// Base endpoint for updating a calculation entity.
/// Route: PUT /calculation-entities/{id}
/// </summary>
public abstract class UpdateCalculationEntityEndpointBase : CrudUpdateEndpoint<UpdateCalculationEntityRequest, CalculationEntityDetailDto>
{
    /// <inheritdoc/>
    protected override string ResourceName => "calculation-entities";

    /// <inheritdoc/>
    protected override string GetResourceIdentifier(UpdateCalculationEntityRequest request) => request.Id.ToString();
}
