using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.Services.Connections.Abstractions;
using Fdw.Services.Connections.Endpoints.Logging;
using Fdw.Web.RestEndpoints.Crud;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Services.Connections.Endpoints;

/// <summary>
/// Base endpoint that returns the capability metadata declared by a specific connection type.
/// Route: GET /connection-types/{Name}/capabilities
/// </summary>
/// <remarks>
/// Capabilities are populated from opt-in interfaces:
/// <see cref="ISupportsContainerTypes"/>, <see cref="ISupportsFieldTypes"/>,
/// <see cref="ISupportsWriteModes"/>, and <see cref="ISupportsDataPathFormats"/>.
/// Connection types that do not implement an interface return an empty list for that capability.
/// </remarks>
public abstract class GetConnectionTypeCapabilitiesEndpointBase
    : CrudGetEndpointBase<ConnectionTypeNameRequest, ConnectionTypeCapabilitiesResponse>
{
    private readonly ILogger<GetConnectionTypeCapabilitiesEndpointBase> _logger;
    private readonly ConnectionConfigurationProvider _configProvider;

    /// <inheritdoc />
    protected GetConnectionTypeCapabilitiesEndpointBase(
        ConnectionConfigurationProvider configProvider,
        ILogger<GetConnectionTypeCapabilitiesEndpointBase> logger)
    {
        _configProvider = configProvider;
        _logger = logger ?? NullLogger<GetConnectionTypeCapabilitiesEndpointBase>.Instance;
    }

    /// <inheritdoc />
    protected override string ResourceName => "connection-types";

    /// <inheritdoc />
    protected override string ReadPolicy => "connections:read";

    /// <inheritdoc />
    protected override string Route => "/connection-types/{Name}/capabilities";

    /// <inheritdoc />
    protected override string EndpointSummary => "Get connection type capabilities";

    /// <inheritdoc />
    protected override string EndpointDescription =>
        "Returns the capability metadata declared by the specified connection type, " +
        "including supported container types, field types, write modes, and path formats. " +
        "Empty lists are returned for capabilities not declared by the type.";

    /// <inheritdoc />
    protected override string GetResourceIdentifier(ConnectionTypeNameRequest request) => request.Name;

    /// <inheritdoc />
    protected override async Task<IGenericResult<ConnectionTypeCapabilitiesResponse?>> FindByIdentifier(
        ConnectionTypeNameRequest request, CancellationToken ct)
    {
        // Why: {Name} accepts either a connection-type name (e.g., "MsSql") or a connection Id
        // (Guid). If a Guid, resolve the connection and use its ServiceOptionType.
        var typeName = request.Name;
        if (Guid.TryParse(request.Name, out var connectionId))
        {
            var connResult = await _configProvider.Get(connectionId, ct).ConfigureAwait(false);
            if (!connResult.IsSuccess || connResult.Value is null)
            {
                ConnectionEndpointLog.ConnectionTypeNotFound(_logger, request.Name);
                return GenericResult<ConnectionTypeCapabilitiesResponse?>.Success(null);
            }
            typeName = connResult.Value.ServiceOptionType ?? string.Empty;
        }

        var connectionType = Fdw.Services.Connections.ConnectionTypes.ByName(typeName);
        if (connectionType is null)
        {
            ConnectionEndpointLog.ConnectionTypeNotFound(_logger, typeName);
            return GenericResult<ConnectionTypeCapabilitiesResponse?>.Success(null);
        }

        var containerTypes = connectionType is ISupportsContainerTypes containerCapable
            ? containerCapable.SupportedContainerTypes
            : (IReadOnlyList<string>)[];

        var fieldTypes = connectionType is ISupportsFieldTypes fieldCapable
            ? fieldCapable.SupportedFieldTypes.Select(f => new FieldTypeInfo(f.Name, f.DbTypeName, f.DisplayName)).ToList()
            : (IReadOnlyList<FieldTypeInfo>)[];

        var writeModes = connectionType is ISupportsWriteModes writeCapable
            ? writeCapable.SupportedWriteModes
            : (IReadOnlyList<string>)[];

        var pathFormats = connectionType is ISupportsDataPathFormats pathCapable
            ? pathCapable.SupportedPathFormats
            : (IReadOnlyList<string>)[];

        var response = new ConnectionTypeCapabilitiesResponse
        {
            ContainerTypes = containerTypes,
            FieldTypes = fieldTypes,
            WriteModes = writeModes,
            PathFormats = pathFormats,
        };

        ConnectionEndpointLog.CapabilitiesResolved(_logger, request.Name);
        return GenericResult<ConnectionTypeCapabilitiesResponse?>.Success(response);
    }
}
