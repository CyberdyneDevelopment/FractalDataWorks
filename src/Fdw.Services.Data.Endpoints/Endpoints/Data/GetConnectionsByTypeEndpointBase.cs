using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.Services.Connections;
using Fdw.Web.RestEndpoints.Crud;

namespace Fdw.Services.Data.Endpoints;

/// <summary>
/// Generic base endpoint for retrieving connections of a specific type.
/// </summary>
public abstract class GetConnectionsByTypeEndpointBase : CrudGetEndpoint<GetConnectionsByTypeRequest, List<ConnectionByTypeDto>>
{
    // Why: ConnectionConfigurationProvider replaces IOptionsMonitor<List<ConnectionConfiguration>>
    // with dual-source (ctrl + cfg) provider that merges system and user configurations.
    private readonly ConnectionConfigurationProvider _provider;

    /// <inheritdoc />
    protected GetConnectionsByTypeEndpointBase(ConnectionConfigurationProvider provider)
    {
        _provider = provider;
    }

    /// <summary>Gets the resource name used for route and policy generation.</summary>
    protected override string ResourceName => "connection-types";

    /// <summary>Gets the authorization policy for read access.</summary>
    protected override string ReadPolicy => "connections:read";

    /// <summary>Returns the connection type as the resource identifier.</summary>
    protected override string GetResourceIdentifier(GetConnectionsByTypeRequest request) => request.TypeName;

    /// <summary>Finds all connections of the specified type.</summary>
    protected override async Task<IGenericResult<List<ConnectionByTypeDto>?>> FindByIdentifier(GetConnectionsByTypeRequest request, CancellationToken ct)
    {
        var allResult = await _provider.Get(ct).ConfigureAwait(false);
        if (!allResult.IsSuccess)
            return allResult.ToNewResult<List<ConnectionByTypeDto>?>();
        var allConnections = allResult.Value!;
        var connections = allConnections
            .Where(c => string.Equals(c.ServiceOptionType, request.TypeName, StringComparison.OrdinalIgnoreCase))
            .Select(MapToDto)
            .ToList();

        return GenericResult<List<ConnectionByTypeDto>?>.Success(connections);
    }

    /// <summary>Maps a connection configuration to a connection by type DTO.</summary>
    protected virtual ConnectionByTypeDto MapToDto(ConnectionConfiguration connection)
    {
        return new ConnectionByTypeDto
        {
            Name = connection.Name,
            ConnectionType = connection.ServiceOptionType ?? "Unknown"
        };
    }
}
