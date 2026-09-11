using System;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.Services.Abstractions;
using Fdw.Services.Connections.Abstractions;
using Fdw.Services.Connections.Endpoints.Logging;
using Fdw.Services.Data;
using Fdw.Web.RestEndpoints.Crud;
using Microsoft.AspNetCore.Http;
using Fdw.Services.Connections.Clients.Models;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Connections.Endpoints;

/// <summary>
/// Generic base endpoint for creating a new connection configuration.
/// Builds the implementation configuration and saves it through the connection provider in ONE call.
/// </summary>
/// <typeparam name="TConfig">The connection implementation configuration this endpoint builds.</typeparam>
/// <remarks>
/// There is no second record to compose. The domain row is four columns the provider writes itself from
/// Save's domain/implementation/name arguments, so the implementation configuration this endpoint builds is
/// the whole of what an endpoint has to supply. The provider reads <c>Implementation</c>, resolves the
/// provider registered for it, and that provider writes the row and everything under it — so the endpoint
/// never holds an implementation provider, and the connection type stays invisible to the machinery.
/// </remarks>
public abstract class CreateConnectionEndpointBase<TConfig> : CrudCreateEndpointBase<CreateConnectionRequest, ConnectionDetailDto>
    where TConfig : class, IConnectionImplementationConfiguration
{
    private readonly ConnectionConfigurationProvider _connectionProvider;
    private readonly ISchemaInformationService? _schemaInformationService;

    /// <inheritdoc />
    protected CreateConnectionEndpointBase(ILogger<CreateConnectionEndpointBase<TConfig>> logger, ConnectionConfigurationProvider connectionProvider,
        ISchemaInformationService? schemaInformationService = null) : base(logger)
    {
        _connectionProvider = connectionProvider;
        _schemaInformationService = schemaInformationService;
    }

    /// <summary>Gets the resource name used for route and policy generation.</summary>
    protected override string ResourceName => "connections";

    /// <summary>Returns the connection name from the create request.</summary>
    protected override string GetResourceName(CreateConnectionRequest request) => request.Name;

    /// <summary>Checks whether a connection with the requested name already exists.</summary>
    protected override async Task<IGenericResult<bool>> CheckExists(CreateConnectionRequest request, CancellationToken ct)
    {
        var existingResult = await _connectionProvider.Get(request.Name, ct).ConfigureAwait(false);
        return GenericResult<bool>.Success(existingResult.IsSuccess && existingResult.Value != null);
    }

    /// <summary>Builds the connection's implementation configuration and saves it through the provider.</summary>
    protected override async Task<IGenericResult<ConnectionDetailDto>> Create(CreateConnectionRequest request, CancellationToken ct)
    {
        var connection = CreateConnectionConfiguration(request, Guid.CreateVersion7());

        if (connection.HealthCheckEnabled && !connection.HealthCheckOnStartup && connection.HealthCheckIntervalSeconds is null)
        {
            return GenericResult<ConnectionDetailDto>.Failure(
                ConnectionEndpointLog.HealthCheckEnabledWithoutTrigger(Logger, request.Name));
        }

        // Name, Domain and Implementation are not set on the record: Save stamps them from its own
        // arguments, and the implementation row does not persist them -- they belong to the domain row,
        // whose Id Save stamps back onto this instance.
        var connectionSave = await _connectionProvider.Save(connection, "Connection", request.ServiceType, request.Name, ct).ConfigureAwait(false);
        if (connectionSave.IsFailure)
        {
            return connectionSave.ToNewResult<ConnectionDetailDto>();
        }

        var detail = MapToDetail(connection, connection.Id);

        // Fire schema discovery if ISchemaInformationService is registered (optional dependency).
        var schemaService = _schemaInformationService;
        if (schemaService != null)
        {
            var schemaResult = await schemaService.GetSchema(request.Name, ct).ConfigureAwait(false);

            if (schemaResult.IsSuccess && schemaResult.Value is { } schemaInfo)
            {
                detail.SetupSummary = new ConnectionSetupSummaryPayload
                {
                    DiscoveryId = schemaInfo.DataStore.Id.ToString(),
                    ConnectionTestPassed = true,
                    DataStoreName = schemaInfo.DataStore.Name
                };
            }
            else
            {
                // Discovery failed or produced no result — connection was still created but schema setup did not complete.
                detail.SetupSummary = null;
            }
        }

        return GenericResult<ConnectionDetailDto>.Success(detail);
    }

    /// <summary>
    /// Builds this connection's implementation configuration from the create request, including the
    /// health-check fields the contract carries.
    /// </summary>
    /// <remarks>
    /// Do not set Name, Domain or Implementation: Save stamps all three from its own arguments and the
    /// implementation row does not persist them. <paramref name="connectionId"/> seeds <c>Id</c> so the
    /// record has one before the write; Save replaces it with the domain row's Id.
    /// </remarks>
    protected abstract TConfig CreateConnectionConfiguration(CreateConnectionRequest request, Guid connectionId);

    /// <summary>
    /// Maps the saved connection configuration to a detail DTO.
    /// Override to add type-specific fields to the response.
    /// </summary>
    protected abstract ConnectionDetailDto MapToDetail(TConfig connection, Guid connectionId);

    /// <summary>Sends a 201 Created response with the connection detail.</summary>
    protected override Task SendCreatedResponse(ConnectionDetailDto detail, CancellationToken ct)
    {
        return Send.ResponseAsync(detail, 201, ct);
    }
}
