using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.Web.RestEndpoints.Crud;

namespace Fdw.Operations.Endpoints.TypeCollections;

/// <summary>
/// Endpoint that lists all TypeOption values for a named TypeCollection.
/// Route: GET /api/v1/type-collections/{collectionName}/values
/// </summary>
public abstract class ListTypeCollectionValuesEndpoint
    : CrudListEndpoint<GetTypeCollectionValuesRequest, TypeCollectionValueSummaryDto>
{
    /// <inheritdoc/>
    protected override string ResourceName => "type-collections";

    /// <inheritdoc/>
    protected override string ReadPolicy => "configurations:read";

    /// <inheritdoc/>
    protected override string Route => "/type-collections/{collectionName}/values";

    /// <inheritdoc/>
    protected override string EndpointSummary => "List TypeCollection values";

    /// <inheritdoc/>
    protected override string EndpointDescription =>
        "Returns all TypeOption names and property metadata for the specified TypeCollection.";

    /// <inheritdoc/>
    protected override Task<IGenericResult<List<TypeCollectionValueSummaryDto>>> LoadItems(
        GetTypeCollectionValuesRequest request, CancellationToken ct)
    {
        var collectionType = TypeCollectionResolver.FindCollectionType(request.CollectionName);

        if (collectionType == null)
            return Task.FromResult(
                GenericResult<List<TypeCollectionValueSummaryDto>>.Success([]));

        var values = TypeCollectionResolver.GetValues(collectionType)
            .Select(TypeCollectionResolver.ToSummary)
            .OrderBy(v => v.Name, System.StringComparer.Ordinal)
            .ToList();

        return Task.FromResult(GenericResult<List<TypeCollectionValueSummaryDto>>.Success(values));
    }
}
