using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.Web.RestEndpoints.Crud;
using Microsoft.Extensions.Logging;

namespace Fdw.Operations.Endpoints.TypeCollections;

/// <summary>
/// Endpoint that lists the TypeCollections loaded in this process.
/// Route: GET /api/v1/type-collections
/// </summary>
public abstract class ListTypeCollectionsEndpointBase
    : CrudListEndpointBase<TypeCollectionSummaryDto>
{
    /// <summary>Initializes a new instance of the <see cref="ListTypeCollectionsEndpointBase"/> class.</summary>
    protected ListTypeCollectionsEndpointBase(ILogger<ListTypeCollectionsEndpointBase> logger) : base(logger)
    {
    }

    /// <inheritdoc/>
    protected override string ResourceName => "type-collections";

    /// <inheritdoc/>
    protected override string ReadPolicy => "configurations:read";

    /// <inheritdoc/>
    protected override string Route => "/type-collections";

    /// <inheritdoc/>
    protected override string EndpointSummary => "List TypeCollections";

    /// <inheritdoc/>
    protected override string EndpointDescription =>
        "Returns the name, kind (type or service-type) and option count of each TypeCollection loaded by this host.";

    /// <inheritdoc/>
    protected override Task<IGenericResult<List<TypeCollectionSummaryDto>>> LoadItems(CancellationToken ct)
    {
        var collections = TypeCollectionResolver.FindAll()
            .Select(c => new TypeCollectionSummaryDto
            {
                Name = c.Name,
                Kind = c.Kind,
                ValueCount = TypeCollectionResolver.GetValues(c.Type).Count,
            })
            .ToList();

        OperationsEndpointLog.TypeCollectionsListed(Logger, collections.Count);

        return Task.FromResult(GenericResult<List<TypeCollectionSummaryDto>>.Success(collections));
    }
}
