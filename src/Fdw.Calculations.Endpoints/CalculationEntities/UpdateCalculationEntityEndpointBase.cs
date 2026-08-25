using Fdw.Web.RestEndpoints.Crud;

namespace Fdw.Calculations.Endpoints.CalculationEntities;

/// <summary>
/// Base endpoint for updating a calculation entity.
/// Route: PATCH /calculation-entities/{id}
/// </summary>
public abstract class UpdateCalculationEntityEndpointBase : CrudUpdateEndpointBase<UpdateCalculationEntityRequest, CalculationEntityDetailDto>
{
    /// <inheritdoc/>
    protected override string ResourceName => "calculation-entities";

    // Why the route is declared rather than inherited: the base builds "/{ResourceName}/{Name}",
    // and every endpoint here identifies a calculation entity by its id. The request this update
    // takes has no Name property at all, so the inherited route bound nothing from the URL and the
    // id it looks up stayed empty.
    /// <inheritdoc />
    protected override string Route => $"/{ResourceName}/{{Id}}";

    /// <inheritdoc/>
    protected override string GetResourceIdentifier(UpdateCalculationEntityRequest request) => request.Id.ToString();
}
