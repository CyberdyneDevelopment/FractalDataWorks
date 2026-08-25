using Fdw.Web.RestEndpoints.Crud;

namespace Fdw.Calculations.Endpoints.CalculationEntities;

/// <summary>
/// Base endpoint for deleting a calculation entity (soft delete).
/// Route: DELETE /calculation-entities/{id}
/// </summary>
public abstract class DeleteCalculationEntityEndpointBase : CrudDeleteEndpointBase<CalculationEntityIdRequest>
{
    /// <inheritdoc/>
    protected override string ResourceName => "calculation-entities";

    // Why the route is declared rather than inherited: the base builds "/{ResourceName}/{Name}",
    // and every endpoint here identifies a calculation entity by its id. The request this delete
    // takes has no Name property at all, so the inherited route bound nothing from the URL and the
    // id it looks up stayed empty.
    /// <inheritdoc />
    protected override string Route => $"/{ResourceName}/{{Id}}";

    /// <inheritdoc/>
    protected override string GetResourceIdentifier(CalculationEntityIdRequest request) => request.Id.ToString();
}
