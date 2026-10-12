using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Operations.Abstractions.Results;
using Fdw.Results;
using Fdw.Web.RestEndpoints.Crud;
using Microsoft.Extensions.Logging;

namespace Fdw.Operations.Endpoints.TypeCollections;

/// <summary>
/// Endpoint that lists all TypeOption values for a named TypeCollection.
/// Route: GET /api/v1/type-collections/{collectionName}/values
/// </summary>
public abstract class ListTypeCollectionValuesEndpointBase
    : CrudListEndpointBase<GetTypeCollectionValuesRequest, TypeCollectionValueSummaryDto>
{
    /// <summary>Initializes a new instance of the <see cref="ListTypeCollectionValuesEndpointBase"/> class.</summary>
    protected ListTypeCollectionValuesEndpointBase(ILogger<ListTypeCollectionValuesEndpointBase> logger) : base(logger)
    {
    }

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
                GenericResult<List<TypeCollectionValueSummaryDto>>.Failure(
                    OperationsResultCodes.ByName("TypeCollectionNotFound"),
                    Logger,
                    ResultDetails.Create().With("CollectionName", request.CollectionName)));

        var summaries = TypeCollectionResolver.GetValues(collectionType)
            .Select(TypeCollectionResolver.ToSummary)
            .ToList();

        // Why by name: an option's id is derived from its type name, so ids carry no order. The console
        // renders in the order returned, and name is the one stable order every collection has.
        var values = summaries.OrderBy(v => v.Name, System.StringComparer.Ordinal).ToList();

        return Task.FromResult(GenericResult<List<TypeCollectionValueSummaryDto>>.Success(values));
    }
}
