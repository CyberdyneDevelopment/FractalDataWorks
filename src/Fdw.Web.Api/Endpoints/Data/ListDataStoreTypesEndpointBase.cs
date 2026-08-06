using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Data.Clients.Models;
using Fdw.Web.RestEndpoints.Crud;

namespace Fdw.Services.Data.Endpoints;

/// <summary>
/// Generic base endpoint for listing available DataStore types from the source-generated TypeCollection.
/// Enumerates all registered DataStoreTypes and maps each to a summary DTO.
/// </summary>
public abstract class ListDataStoreTypesEndpointBase : CrudListEndpoint<DataStoreTypeSummaryPayload>
{
    /// <summary>Gets the resource name used for route and policy generation.</summary>
    protected override string ResourceName => "datastores/types";

    /// <summary>Gets the authorization policy for read access.</summary>
    protected override string ReadPolicy => "datastores:read";

    /// <summary>Gets the OpenAPI summary for this endpoint.</summary>
    protected override string EndpointSummary => "List available DataStore types";

    /// <summary>Gets the OpenAPI description for this endpoint.</summary>
    protected override string EndpointDescription =>
        "Returns a list of all available DataStore types registered via source-generated TypeCollections.";

    /// <summary>Loads all registered DataStore types as summary DTOs.</summary>
    protected override Task<IGenericResult<List<DataStoreTypeSummaryPayload>>> LoadItems(CancellationToken ct)
    {
        // Why: DataStoreTypes is a MutableTypeCollection; .All() returns IReadOnlyCollection<IDataStoreType>
        // (direct elements, not dictionary KVPs). Contrast with ServiceTypeCollections (e.g. ConnectionTypes)
        // which return ImmutableDictionary<Guid, IConnectionType> and require kvp.Value.
        // Why: IDataStoreType exposes Name (from ITypeOption) + SectionName; it has no DisplayName/
        // Description/Category members, so we mirror the connection-types pattern (Name doubles as the
        // display label) and surface SectionName as the category. No fabricated values.
        var items = DataStoreTypes.All()
            .Select(t => new DataStoreTypeSummaryPayload
            {
                TypeName = t.Name,
                DisplayName = t.Name,
                Description = $"DataStore type: {t.Name}",
                Category = "DataStore"
            })
            .ToList();

        return Task.FromResult(GenericResult<List<DataStoreTypeSummaryPayload>>.Success(items));
    }
}
