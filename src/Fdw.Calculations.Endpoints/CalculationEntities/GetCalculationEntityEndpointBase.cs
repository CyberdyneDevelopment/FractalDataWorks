using Fdw.Web.RestEndpoints.Crud;
using Microsoft.Extensions.Logging;

namespace Fdw.Calculations.Endpoints.CalculationEntities;

/// <summary>
/// Base endpoint for getting a calculation entity by ID.
/// Route: POST /calculation-entities/get, the id in the body (FDW-978).
/// </summary>
public abstract class GetCalculationEntityEndpointBase : CrudGetEndpointBase<CalculationEntityIdRequest, CalculationEntityDetailDto>
{
    /// <summary>Initializes a new instance of the <see cref="GetCalculationEntityEndpointBase"/> class.</summary>
    protected GetCalculationEntityEndpointBase(ILogger<GetCalculationEntityEndpointBase> logger) : base(logger)
    {
    }

    /// <inheritdoc/>
    protected override string ResourceName => "calculation-entities";

    /// <summary>Gets whether the calculation entity is read by POST with its id in the body: it is.</summary>
    protected override bool ReadByBody => true;

    /// <inheritdoc/>
    protected override string GetResourceIdentifier(CalculationEntityIdRequest request) => request.Id.ToString();
}
