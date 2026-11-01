using System;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.Services.Connections.Abstractions;
using Fdw.Web.RestEndpoints.Crud;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Connections.Endpoints;

/// <summary>
/// Base endpoint for retrieving a specific connection configuration by name. Type-agnostic: a read
/// through <see cref="ConnectionConfigurationProvider"/> is already dispatched, so what comes back is the
/// implementation itself, and <see cref="ConnectionDetailMapper"/> maps it through the connection type's own
/// <see cref="IConnectionType.EndpointBehavior"/> -- so one GET-by-name endpoint renders every connection
/// type, including one with no endpoint code of its own, rather than being locked to one provider.
/// </summary>
public abstract class GetConnectionEndpointBase : CrudGetEndpointBase<ConnectionNameRequest, ConnectionDetailDto>
{
    private readonly ConnectionConfigurationProvider _configProvider;

    /// <inheritdoc />
    protected GetConnectionEndpointBase(ILogger<GetConnectionEndpointBase> logger, ConnectionConfigurationProvider configProvider) : base(logger)
    {
        _configProvider = configProvider;
    }

    /// <summary>Gets the resource name used for route and policy generation.</summary>
    protected override string ResourceName => "connections";

    /// <summary>Returns the connection name as the resource identifier.</summary>
    protected override string GetResourceIdentifier(ConnectionNameRequest request) => request.Name;

    /// <summary>
    /// Finds a connection by name (or Guid id) and maps it to a detail DTO. A read through the domain is
    /// already dispatched, so what comes back is the implementation itself.
    /// </summary>
    protected override async Task<IGenericResult<ConnectionDetailDto?>> FindByIdentifier(ConnectionNameRequest request, CancellationToken ct)
    {
        // Accept either Guid (connection Id) or string name as identifier.
        var domainResult = Guid.TryParse(request.Name, out var id)
            ? await _configProvider.Get(id, ct).ConfigureAwait(false)
            : await _configProvider.Get(request.Name, ct).ConfigureAwait(false);
        if (!domainResult.IsSuccess) return domainResult.ToNewResult<ConnectionDetailDto?>();

        var connection = domainResult.Value;
        if (connection is null) return GenericResult<ConnectionDetailDto?>.Success(null);

        var detail = ConnectionDetailMapper.Map(ConnectionTypes.ByName(connection.Implementation), connection, Logger);
        if (detail.IsFailure) return detail.ToNewResult<ConnectionDetailDto?>();

        var found = detail.Value!;
        found.SchemaDiscovery = SchemaDiscoveryTypeInfo.Of(connection.SchemaDiscovery);
        return GenericResult<ConnectionDetailDto?>.Success(found);
    }
}
