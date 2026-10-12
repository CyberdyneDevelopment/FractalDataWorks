using Fdw.Web.RestEndpoints.Crud;
using Microsoft.Extensions.Logging;

namespace Fdw.Calculations.Endpoints.CalculationEntities;

/// <summary>
/// Base endpoint for updating a calculation entity.
/// Route: PATCH /calculation-entities/{id}
/// </summary>
public abstract class UpdateCalculationEntityEndpointBase : CrudUpdateEndpointBase<UpdateCalculationEntityRequest, CalculationEntityDetailDto>
{
    /// <summary>Initializes a new instance of the <see cref="UpdateCalculationEntityEndpointBase"/> class.</summary>
    protected UpdateCalculationEntityEndpointBase(ILogger<UpdateCalculationEntityEndpointBase> logger) : base(logger)
    {
    }

    /// <inheritdoc/>
    protected override string ResourceName => "calculation-entities";

    /// <inheritdoc />
    protected override string Route => $"/{ResourceName}/{{Id}}";

    /// <inheritdoc/>
    protected override string GetResourceIdentifier(UpdateCalculationEntityRequest request) => request.Id.ToString();
}
