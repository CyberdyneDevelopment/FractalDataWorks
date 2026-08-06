using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Data.DataSets.Abstractions;
using Fdw.Results;
using Fdw.Services.Data.Clients.Models;
using Fdw.Web.RestEndpoints.Crud;

namespace Fdw.Services.Data.Endpoints;

/// <summary>
/// Generic base endpoint for listing available DataSet types from the source-generated TypeCollection.
/// Enumerates all registered DataSetTypes and maps each to a summary DTO.
/// </summary>
public abstract class ListDataSetTypesEndpointBase : CrudListEndpoint<DataSetTypeSummaryPayload>
{
    /// <summary>Gets the resource name used for route and policy generation.</summary>
    protected override string ResourceName => "datasets/types";

    /// <summary>Gets the authorization policy for read access.</summary>
    protected override string ReadPolicy => "datasets:read";

    /// <summary>Gets the OpenAPI summary for this endpoint.</summary>
    protected override string EndpointSummary => "List available DataSet types";

    /// <summary>Gets the OpenAPI description for this endpoint.</summary>
    protected override string EndpointDescription =>
        "Returns a list of all available DataSet types registered via source-generated TypeCollections.";

    /// <summary>Loads all registered DataSet types as summary DTOs.</summary>
    protected override Task<IGenericResult<List<DataSetTypeSummaryPayload>>> LoadItems(CancellationToken ct)
    {
        // Why: DataSetTypes is a MutableTypeCollection; .All() returns IReadOnlyCollection<IDataSetType>
        // (direct elements, not dictionary KVPs). Contrast with ServiceTypeCollections (e.g. ConnectionTypes)
        // which return ImmutableDictionary<Guid, IConnectionType> and require kvp.Value.
        // Why: IDataSetType exposes Name (from ITypeOption) + Description; it has no DisplayName/Category
        // members, so Name doubles as the display label (connection-types pattern) and the category is the
        // fixed domain name. No fabricated values.
        var items = DataSetTypes.All()
            .Select(t => new DataSetTypeSummaryPayload
            {
                TypeName = t.Name,
                DisplayName = t.Name,
                Description = t.Description,
                Category = "DataSet"
            })
            .ToList();

        return Task.FromResult(GenericResult<List<DataSetTypeSummaryPayload>>.Success(items));
    }
}
