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

namespace Fdw.Services.Connections.Endpoints;

/// <summary>
/// Generic base endpoint for creating a new connection configuration.
/// Composes the whole aggregate — the <see cref="ConnectionConfiguration"/> header plus its typed body —
/// and saves it through the connection provider in ONE call.
/// </summary>
/// <typeparam name="TConfig">The concrete typed body configuration type this endpoint builds.</typeparam>
/// <remarks>
/// Why one save: the header provider owns the dispatch. It reads <c>ServiceOptionType</c>, resolves the
/// registered typed provider for it, and hands that provider the body to write along with the body's own
/// subtree. The endpoint therefore never holds a typed provider — the connection type stays invisible to
/// the machinery, which is the point of the composed-header pattern. The previous two-save shape let the
/// header and the body disagree: a request with ServiceType "Http" wrote an MsSql body under an Http header,
/// because only the endpoint's own generic argument decided what got written.
/// </remarks>
public abstract class CreateConnectionEndpointBase<TConfig> : CrudCreateEndpoint<CreateConnectionRequest, ConnectionDetailDto>
    where TConfig : class, IConnectionConfiguration
{
    // Why: the connection provider writes the whole aggregate — the conn.Connection header row AND, by
    // dispatch, the per-type body row (conn.MsSqlConnection etc.) with its authentication/limits children.
    private readonly ConnectionConfigurationProvider _connectionProvider;

    /// <inheritdoc />
    protected CreateConnectionEndpointBase(ConnectionConfigurationProvider connectionProvider)
    {
        _connectionProvider = connectionProvider;
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

    /// <summary>Creates both the parent connection and typed body, then persists each to its own provider.</summary>
    protected override async Task<IGenericResult<ConnectionDetailDto>> Create(CreateConnectionRequest request, CancellationToken ct)
    {
        // Why: connectionId is minted here — before the save — so the typed body can carry its ConnectionId
        // FK. The typed body's own Id is left as Guid.Empty; the cascade mints it via Guid.CreateVersion7()
        // before INSERT.
        var connectionId = Guid.CreateVersion7();

        var connection = CreateConnectionRecord(request, connectionId);

        // Why: HealthCheckEnabled=true with no trigger (neither an on-startup probe nor a periodic
        // interval) is a dead config — ConnectionHealthMonitorWorker would never check it. Fail loud
        // here, before either Save, rather than silently persisting an inert setting.
        if (connection.HealthCheckEnabled && !connection.HealthCheckOnStartup && connection.HealthCheckIntervalSeconds is null)
        {
            return GenericResult<ConnectionDetailDto>.Failure(
                ConnectionEndpointLog.HealthCheckEnabledWithoutTrigger(Logger, request.Name));
        }

        var typedBody = CreateTypedBody(request, connectionId);
        connection.Configuration = typedBody;

        // One save for the whole aggregate: the provider writes the header, then dispatches on
        // ServiceOptionType so the registered typed provider writes the body and everything under it.
        var connectionSave = await _connectionProvider.Save(connection, ct).ConfigureAwait(false);
        if (connectionSave.IsFailure)
        {
            return connectionSave.ToNewResult<ConnectionDetailDto>();
        }

        var detail = MapToDetail(connection, typedBody, connectionId);

        // Fire schema discovery if ISchemaInformationService is registered (optional dependency).
        // Why: ISchemaInformationService is the demand-driven replacement for ISchemaDiscoveryOrchestrator.
        // GetSchema discovers and persists schema immediately after connection creation so the UI
        // has metadata without requiring a separate "Re-discover" action.
        var schemaService = TryResolve<ISchemaInformationService>();
        if (schemaService != null)
        {
            var schemaResult = await schemaService.GetSchema(request.Name, ct).ConfigureAwait(false);

            if (schemaResult.IsSuccess && schemaResult.Value is { } schemaInfo)
            {
                detail.SetupSummary = new ConnectionSetupSummaryPayload
                {
                    // Why: No SignalR correlation ID in the new service — the DataStore name
                    // serves as the stable identifier the client can use for follow-up queries.
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
    /// Builds the parent <see cref="ConnectionConfiguration"/> from the create request.
    /// The default implementation sets Name, ServiceOptionType from <see cref="CreateConnectionRequest.ServiceType"/>,
    /// and Id from <paramref name="connectionId"/>.
    /// Override to customize header fields (Description, Environment, etc.).
    /// </summary>
    protected virtual ConnectionConfiguration CreateConnectionRecord(CreateConnectionRequest request, Guid connectionId)
    {
        return new ConnectionConfiguration
        {
            Id = connectionId,
            Name = request.Name,
            ServiceOptionType = request.ServiceType,
            HealthCheckEnabled = request.HealthCheckEnabled,
            HealthCheckOnStartup = request.HealthCheckOnStartup,
            HealthCheckIntervalSeconds = request.HealthCheckIntervalSeconds,
        };
    }

    /// <summary>
    /// Builds the typed body configuration from the create request.
    /// The typed body's own <c>Id</c> should be left as <see cref="Guid.Empty"/> — the provider
    /// mints it via <see cref="Guid.CreateVersion7()"/> before INSERT. The typed body's
    /// <c>ConnectionId</c> must be set to <paramref name="connectionId"/>.
    /// </summary>
    protected abstract TConfig CreateTypedBody(CreateConnectionRequest request, Guid connectionId);

    /// <summary>
    /// Maps the saved parent connection and typed body to a detail DTO.
    /// Override to add type-specific fields to the response.
    /// </summary>
    protected abstract ConnectionDetailDto MapToDetail(ConnectionConfiguration connection, TConfig typedBody, Guid connectionId);

    /// <summary>Sends a 201 Created response with the connection detail.</summary>
    protected override Task SendCreatedResponse(ConnectionDetailDto detail, CancellationToken ct)
    {
        return Send.ResponseAsync(detail, 201, ct);
    }
}
