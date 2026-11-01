using Fdw.Web.RestEndpoints.Crud;
using Microsoft.Extensions.Logging;

namespace Fdw.Calculations.Endpoints.CalculationEntities;

/// <summary>
/// Base endpoint for deleting a calculation entity (soft delete).
/// Route: DELETE /calculation-entities/{id}
/// </summary>
public abstract class DeleteCalculationEntityEndpointBase : CrudDeleteEndpointBase<CalculationEntityIdRequest>
{
    /// <summary>Initializes a new instance of the <see cref="DeleteCalculationEntityEndpointBase"/> class.</summary>
    protected DeleteCalculationEntityEndpointBase(ILogger<DeleteCalculationEntityEndpointBase> logger) : base(logger)
    {
    }

    /// <inheritdoc/>
    protected override string ResourceName => "calculation-entities";

    /// <inheritdoc />
    protected override string Route => $"/{ResourceName}/{{Id}}";

    /// <inheritdoc/>
    protected override string GetResourceIdentifier(CalculationEntityIdRequest request) => request.Id.ToString();
}
