using Fdw.Web.RestEndpoints.Crud;
using Microsoft.Extensions.Logging;

namespace Fdw.Calculations.Endpoints.CalculationEntities;

/// <summary>
/// Base endpoint for getting a calculation entity by ID.
/// Route: GET /calculation-entities/{id}
/// </summary>
public abstract class GetCalculationEntityEndpointBase : CrudGetEndpointBase<CalculationEntityIdRequest, CalculationEntityDetailDto>
{
    /// <summary>Initializes a new instance of the <see cref="GetCalculationEntityEndpointBase"/> class.</summary>
    protected GetCalculationEntityEndpointBase(ILogger<GetCalculationEntityEndpointBase> logger) : base(logger)
    {
    }

    /// <inheritdoc/>
    protected override string ResourceName => "calculation-entities";

    /// <inheritdoc />
    protected override string Route => $"/{ResourceName}/{{Id}}";

    /// <inheritdoc/>
    protected override string GetResourceIdentifier(CalculationEntityIdRequest request) => request.Id.ToString();
}
