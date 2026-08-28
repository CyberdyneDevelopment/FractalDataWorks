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

    /// <inheritdoc />
    protected override string Route => $"/{ResourceName}/{{Id}}";

    /// <inheritdoc/>
    protected override string GetResourceIdentifier(CalculationEntityIdRequest request) => request.Id.ToString();
}
