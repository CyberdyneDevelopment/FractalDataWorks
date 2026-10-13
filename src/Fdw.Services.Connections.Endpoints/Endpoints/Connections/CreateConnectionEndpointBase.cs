using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.Services.Abstractions;
using Fdw.Services.Authentication.Abstractions.Security;
using Fdw.Services.Connections;
using Fdw.Services.Connections.Abstractions;
using Fdw.Services.Connections.Endpoints.Logging;
using Fdw.Web.RestEndpoints.Crud;
using Microsoft.AspNetCore.Http;
using Fdw.Services.Connections.Clients.Models;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Connections.Endpoints;

/// <summary>
/// Generic base endpoint for creating a new connection configuration, for any connection type.
/// Builds the implementation configuration and saves it through the connection provider in ONE call.
/// </summary>
/// <remarks>
/// There is no second record to compose. The domain row is four columns the provider writes itself from
/// Save's domain/implementation/name arguments, so the implementation configuration this endpoint builds is
/// the whole of what an endpoint has to supply. The provider reads <c>Implementation</c>, resolves the
/// provider registered for it, and that provider writes the row and everything under it — so the endpoint
/// never holds an implementation provider, and the connection type stays invisible to the machinery.
///
/// Which connection type is being created comes from <see cref="CreateConnectionRequest.ServiceType"/>,
/// resolved via <c>ConnectionTypes.ByName(...)</c> — the same registry every other connection
/// capability comes from — whose <see cref="IConnectionType.EndpointBehavior"/> binds the body into the
/// type's own configuration. Never a cast and never a switch on the type name, so a type with no endpoint
/// code of its own can still be created. One concrete subclass per host is enough; it exists only to give FastEndpoints a
/// route via <c>ConfigureEndpoint</c>, since nothing else here varies by connection type any more.
/// </remarks>
public abstract class CreateConnectionEndpointBase : CrudCreateEndpointBase<CreateConnectionRequest, ConnectionDetailDto>
{
    private readonly ConnectionConfigurationProvider _connectionProvider;

    /// <inheritdoc />
    /// <remarks>
    /// Creating a connection discovers nothing. It used to run the connection's schema discovery straight
    /// away when a schema service was supplied (no reference host supplied one) and dropped any failure;
    /// discovery is now something a person asks for, when they create or refresh a data store.
    /// </remarks>
    protected CreateConnectionEndpointBase(ILogger<CreateConnectionEndpointBase> logger, ConnectionConfigurationProvider connectionProvider) : base(logger)
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

    /// <summary>Builds the connection's implementation configuration and saves it through the provider.</summary>
    protected override async Task<IGenericResult<ConnectionDetailDto>> Create(CreateConnectionRequest request, CancellationToken ct)
    {
        // Whoever creates the connection states its discovery option, and it must be one the connection
        // type offers; nothing defaults it. Checked here rather than in a validator so every host that
        // uses this endpoint enforces it.
        var connectionType = ConnectionTypes.ByName(request.ServiceType);
        if (ReferenceEquals(connectionType, ConnectionTypes.NotFound))
        {
            return GenericResult<ConnectionDetailDto>.Failure(ConnectionEndpointLog.ConnectionTypeNotFound(Logger, request.ServiceType));
        }

        var offered = connectionType.SupportedDiscoveryTypes;
        if (!offered.Any(option => string.Equals(option.Name, request.SchemaDiscovery, StringComparison.Ordinal)))
        {
            return SchemaDiscoveryRefusal.NotOffered<ConnectionDetailDto>(
                Logger, request.Name, request.SchemaDiscovery, request.ServiceType, offered.Select(option => option.Name));
        }

        var bound = connectionType.EndpointBehavior.Bind(ConnectionRequestBody.ForCreate(request), Logger);
        if (bound.IsFailure)
        {
            return bound.ToNewResult<ConnectionDetailDto>();
        }

        var connection = bound.Value!;
        connection.SchemaDiscovery = request.SchemaDiscovery;

        // A connection that acts as the host reaches what the host reaches; only a platform administrator may make one.
        var admitted = HostIdentityGate.Admit(
            connectionType, connection, Resolve<ISystemRoleConfiguration>().IsPlatformAdmin(User), Logger);
        if (admitted.IsFailure)
        {
            return admitted.ToNewResult<ConnectionDetailDto>();
        }

        // Health checking is common to every connection type, so it is set here rather than in each
        // type's CreateConfiguration -- the same place the update endpoint sets it. No type mapped it,
        // so every created connection saved as unchecked and the trigger validation below never fired.
        connection.HealthCheckEnabled = request.HealthCheckEnabled;
        connection.HealthCheckOnStartup = request.HealthCheckOnStartup;
        connection.HealthCheckIntervalSeconds = request.HealthCheckIntervalSeconds;

        if (connection.HealthCheckEnabled && !connection.HealthCheckOnStartup && connection.HealthCheckIntervalSeconds is null)
        {
            return GenericResult<ConnectionDetailDto>.Failure(
                ConnectionEndpointLog.HealthCheckEnabledWithoutTrigger(Logger, request.Name));
        }

        // Where a connection's credentials are stored is the server's choice, and their values arrive write-only: refuse
        // a request that names a secret, then store the values it sent and point the connection at them (FDW-1086). A
        // platform administrator alone may instead name a secret manager and secrets that something else keeps (FDW-1327).
        // Last before the save, so no refusal after it can leave stored secrets behind.
        var credentials = await ConnectionCredentialStore.Prepare(
            connectionType, before: null, connection, request.Credentials, Resolve<ISystemRoleConfiguration>().IsPlatformAdmin(User),
            HttpContext.RequestServices, Logger, ct).ConfigureAwait(false);
        if (credentials.IsFailure)
        {
            return credentials.ToNewResult<ConnectionDetailDto>();
        }

        // Name, Domain and Implementation are not set on the record: Save stamps them from its own
        // arguments, and the implementation row does not persist them -- they belong to the domain row,
        // whose Id Save stamps back onto this instance.
        var connectionSave = await _connectionProvider.Save(connection, "Connection", request.ServiceType, request.Name, ct).ConfigureAwait(false);
        if (connectionSave.IsFailure)
        {
            // The connection was not saved, so nothing names the secrets just written.
            await ConnectionCredentialStore.Retire(
                credentials.Value!.Written, request.Name, HttpContext.RequestServices, Logger, ct).ConfigureAwait(false);
            return connectionSave.ToNewResult<ConnectionDetailDto>();
        }

        var detail = ConnectionDetailMapper.Map(connectionType, connection, Logger);
        if (detail.IsFailure)
        {
            return detail;
        }

        var created = detail.Value!;
        created.CreatedAt = DateTimeOffset.UtcNow;
        created.UpdatedAt = DateTimeOffset.UtcNow;
        created.SchemaDiscovery = SchemaDiscoveryTypeInfo.Of(connection.SchemaDiscovery);
        return GenericResult<ConnectionDetailDto>.Success(created);
    }

    /// <summary>Sends a 201 Created response with the connection detail.</summary>
    protected override Task SendCreatedResponse(ConnectionDetailDto detail, CancellationToken ct)
    {
        return Send.ResponseAsync(detail, 201, ct);
    }
}
