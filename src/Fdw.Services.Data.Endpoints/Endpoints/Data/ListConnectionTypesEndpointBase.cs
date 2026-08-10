using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.Services.Connections.Abstractions;
using Fdw.Web.RestEndpoints.Crud;

namespace Fdw.Services.Data.Endpoints;

/// <summary>
/// Generic base endpoint for listing available connection types from the source-generated collection.
/// </summary>
public abstract class ListConnectionTypesEndpointBase : CrudListEndpoint<ConnectionTypeSummaryDto>
{
    /// <summary>Gets the resource name used for route and policy generation.</summary>
    protected override string ResourceName => "connection-types";

    /// <summary>Gets the authorization policy for read access.</summary>
    protected override string ReadPolicy => "connections:read";

    /// <summary>Gets the OpenAPI summary for this endpoint.</summary>
    protected override string EndpointSummary => "List available connection types";

    /// <summary>Gets the OpenAPI description for this endpoint.</summary>
    protected override string EndpointDescription =>
        "Returns a list of all available connection types registered via source-generated ServiceTypeCollections.";

    /// <summary>Loads all registered connection types as summary DTOs.</summary>
    protected override Task<IGenericResult<List<ConnectionTypeSummaryDto>>> LoadItems(CancellationToken ct)
    {
        var items = MapConnectionTypes();
        return Task.FromResult(GenericResult<List<ConnectionTypeSummaryDto>>.Success(items.ToList()));
    }

    /// <summary>Maps all registered connection types to summary DTOs.</summary>
    protected virtual IReadOnlyList<ConnectionTypeSummaryDto> MapConnectionTypes()
    {
        return Fdw.Services.Connections.ConnectionTypes.All()
            .Select(kvp => MapToSummary(kvp.Value))
            .ToList();
    }

    /// <summary>Maps a single connection type to a summary DTO.</summary>
    protected virtual ConnectionTypeSummaryDto MapToSummary(IConnectionType connectionType)
    {
        return new ConnectionTypeSummaryDto
        {
            Name = connectionType.Name,
            DisplayName = connectionType.Name,
            Description = $"Connection type: {connectionType.Name}",
            Category = "Connection"
        };
    }
}
