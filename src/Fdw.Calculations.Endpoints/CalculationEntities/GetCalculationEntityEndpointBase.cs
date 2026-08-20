using Fdw.Web.RestEndpoints.Crud;

namespace Fdw.Calculations.Endpoints.CalculationEntities;

/// <summary>
/// Base endpoint for getting a calculation entity by ID.
/// Route: GET /calculation-entities/{id}
/// </summary>
public abstract class GetCalculationEntityEndpointBase : CrudGetEndpoint<CalculationEntityIdRequest, CalculationEntityDetailDto>
{
    /// <inheritdoc/>
    protected override string ResourceName => "calculation-entities";

    // Why the route is declared rather than inherited: the base builds "/{ResourceName}/{Name}",
    // and every endpoint here identifies a calculation entity by its id. The request this read
    // takes has no Name property at all, so the inherited route bound nothing from the URL and the
    // id it looks up stayed empty.
    /// <inheritdoc />
    protected override string Route => $"/{ResourceName}/{{Id}}";

    /// <inheritdoc/>
    protected override string GetResourceIdentifier(CalculationEntityIdRequest request) => request.Id.ToString();
}
