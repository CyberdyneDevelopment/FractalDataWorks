using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Configuration;
using Fdw.Results;
using Fdw.Services.Abstractions;
using Fdw.Services.Authentication.Abstractions.Security;
using Fdw.Services.Connections;
using Fdw.Services.Connections.Abstractions;
using Fdw.Services.Connections.Endpoints.Logging;
using Fdw.Web.RestEndpoints.Crud;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Connections.Endpoints;

/// <summary>
/// Generic base endpoint for updating an existing connection configuration, for any connection type.
/// Reads the composed aggregate through the connection provider, merges the request into it, and saves it
/// back in ONE call.
/// </summary>
/// <remarks>
/// Why one read and one save: <c>Get</c> already composes the header AND its typed body by dispatching on
/// <c>Implementation</c>, and <c>Save</c> writes the aggregate back the same way. Reading the two halves
/// through two providers let them drift apart, and saving them separately meant an update that touched only
/// the header left the body pointing at the previous version of it.
///
/// Which connection type is being updated comes from the EXISTING record's own
/// <see cref="IImplementationConfiguration.Implementation"/>, resolved via
/// <c>ConnectionTypes.ByName(...)</c>, whose <see cref="IConnectionType.EndpointBehavior"/> merges the change —
/// never from client input, since an update always targets a connection that already exists and
/// already knows what it is.
/// </remarks>
public abstract class UpdateConnectionEndpointBase : CrudUpdateEndpointBase<UpdateConnectionRequest, ConnectionDetailDto>
{
    private readonly ConnectionConfigurationProvider _connectionProvider;

    /// <inheritdoc />
    protected UpdateConnectionEndpointBase(ILogger<UpdateConnectionEndpointBase> logger, ConnectionConfigurationProvider connectionProvider) : base(logger)
    {
        _connectionProvider = connectionProvider;
    }

    /// <summary>Gets the resource name used for route and policy generation.</summary>
    protected override string ResourceName => "connections";

    /// <summary>Returns the connection name as the resource identifier.</summary>
    protected override string GetResourceIdentifier(UpdateConnectionRequest request) => request.Name;

    /// <summary>Finds the existing connection to update, returning null if not found.</summary>
    protected override async Task<IGenericResult<ConnectionDetailDto?>> FindForUpdate(UpdateConnectionRequest request, CancellationToken ct)
    {
        var connectionResult = await _connectionProvider.Get(request.Name, ct).ConfigureAwait(false);
        if (!connectionResult.IsSuccess || connectionResult.Value is null)
        {
            return GenericResult<ConnectionDetailDto?>.Success(null);
        }

        var existing = connectionResult.Value;
        var detail = ConnectionDetailMapper.Map(ConnectionTypes.ByName(existing.Implementation), existing, Logger);
        if (detail.IsFailure)
        {
            return detail.ToNewResult<ConnectionDetailDto?>();
        }

        var found = detail.Value!;
        found.SchemaDiscovery = SchemaDiscoveryTypeInfo.Of(existing.SchemaDiscovery);
        return GenericResult<ConnectionDetailDto?>.Success(found);
    }

    /// <summary>Merges the update request with the existing configuration and persists via the configurationGateway.</summary>
    protected override async Task<IGenericResult<ConnectionDetailDto>> Update(UpdateConnectionRequest request, ConnectionDetailDto existing, CancellationToken ct)
    {
        // A member the connection does not store is refused, never accepted and ignored (FDW-1177).
        var unsettable = UpdateConnectionChanges.CheckUnchangeable(request, Logger);
        if (unsettable.IsFailure) return unsettable.ToNewResult<ConnectionDetailDto>();

        var connectionResult = await _connectionProvider.Get(request.Name, ct).ConfigureAwait(false);
        if (!connectionResult.IsSuccess || connectionResult.Value is null)
        {
            return GenericResult<ConnectionDetailDto>.Failure(
                ConnectionEndpointLog.ConnectionNotFound(Logger, request.Name));
        }

        var existingConnection = connectionResult.Value;
        var connectionType = ConnectionTypes.ByName(existingConnection.Implementation);

        // What the connection names for its credentials before the request touches it: a merge may change the instance
        // in place, so this snapshot is what the request's own references are compared against (FDW-1086).
        var before = connectionType.CredentialSlots(existingConnection);
        if (before.IsFailure) return before.ToNewResult<ConnectionDetailDto>();

        var merged = connectionType.EndpointBehavior.Merge(ConnectionRequestBody.ForUpdate(request), existingConnection, Logger);
        if (merged.IsFailure) return merged.ToNewResult<ConnectionDetailDto>();

        var updatedConnection = merged.Value!;

        var admitted = AdmitChange(connectionType, existingConnection, updatedConnection);
        if (admitted.IsFailure) return admitted.ToNewResult<ConnectionDetailDto>();

        // Identity is restamped here, not left to the behaviour. An update changes fields; it never renames a
        // connection or moves it to another implementation, so these three are the caller's to preserve --
        // whatever a type's merge does, a Save under a different name or implementation is never what an
        // update meant. (The per-type MergeUpdate this replaced dropped all three, and Save then asked for a
        // provider registered under "".)
        updatedConnection.Name = existingConnection.Name;
        updatedConnection.Domain = existingConnection.Domain;
        updatedConnection.Implementation = existingConnection.Implementation;

        ApplyHealthCheckSettings(request, updatedConnection);

        var discovery = ChooseSchemaDiscovery(request, existingConnection);
        if (discovery.IsFailure) return discovery.ToNewResult<ConnectionDetailDto>();
        updatedConnection.SchemaDiscovery = discovery.Value!;

        var trigger = RequireHealthCheckTrigger(request.Name, updatedConnection);
        if (trigger.IsFailure) return trigger.ToNewResult<ConnectionDetailDto>();

        // Credentials last before the save: refuse a request that names where they are stored, store the values it sent,
        // and after the save delete the secrets the connection no longer names (FDW-1086).
        var credentials = await ConnectionCredentialStore.Prepare(
            connectionType, before.Value, updatedConnection, request.Credentials, HttpContext.RequestServices, Logger, ct).ConfigureAwait(false);
        if (credentials.IsFailure) return credentials.ToNewResult<ConnectionDetailDto>();

        // One save, one record: the provider versions the implementation row and writes everything under
        // it. The name identifies the member, so the existing domain row is the one this hangs from.
        var connectionSave = await _connectionProvider.Save(updatedConnection, "Connection", updatedConnection.Implementation, request.Name, ct).ConfigureAwait(false);
        await ConnectionCredentialStore.Retire(
            connectionSave.IsFailure ? credentials.Value!.Written : credentials.Value!.Retiring,
            request.Name, HttpContext.RequestServices, Logger, ct).ConfigureAwait(false);
        if (connectionSave.IsFailure) return connectionSave.ToNewResult<ConnectionDetailDto>();

        var detail = ConnectionDetailMapper.Map(connectionType, updatedConnection, Logger);
        if (detail.IsFailure) return detail;

        var updated = detail.Value!;
        updated.CreatedAt = existing.CreatedAt;
        updated.UpdatedAt = DateTimeOffset.UtcNow;
        updated.SchemaDiscovery = SchemaDiscoveryTypeInfo.Of(updatedConnection.SchemaDiscovery);
        return GenericResult<ConnectionDetailDto>.Success(updated);
    }

    // The health-check settings the request states replace the connection's; the ones it leaves out stay as they were.
    private static void ApplyHealthCheckSettings(UpdateConnectionRequest request, IConnectionImplementationConfiguration updated)
    {
        updated.HealthCheckEnabled = request.HealthCheckEnabled ?? updated.HealthCheckEnabled;
        updated.HealthCheckOnStartup = request.HealthCheckOnStartup ?? updated.HealthCheckOnStartup;
        updated.HealthCheckIntervalSeconds = request.HealthCheckIntervalSeconds ?? updated.HealthCheckIntervalSeconds;
    }

    // A health check that is enabled needs something to run it: a check at startup or an interval.
    private IGenericResult RequireHealthCheckTrigger(string connectionName, IConnectionImplementationConfiguration updated)
        => updated.HealthCheckEnabled && !updated.HealthCheckOnStartup && updated.HealthCheckIntervalSeconds is null
            ? GenericResult.Failure(ConnectionEndpointLog.HealthCheckEnabledWithoutTrigger(Logger, connectionName))
            : GenericResult.Success();

    // Both sides are asked: changing a connection that acts as the host, or turning one into such a connection, is a
    // platform administrator's decision either way (FDW-1068).
    private IGenericResult AdmitChange(
        IConnectionType connectionType,
        IConnectionImplementationConfiguration existing,
        IConnectionImplementationConfiguration updated)
    {
        var callerIsPlatformAdmin = Resolve<ISystemRoleConfiguration>().IsPlatformAdmin(User);
        var before = HostIdentityGate.Admit(connectionType, existing, callerIsPlatformAdmin, Logger);
        return before.IsFailure ? before : HostIdentityGate.Admit(connectionType, updated, callerIsPlatformAdmin, Logger);
    }

    // An update that names no option keeps the one the connection has; one that names an option must name
    // one the connection's type offers.
    private IGenericResult<string> ChooseSchemaDiscovery(
        UpdateConnectionRequest request, IConnectionImplementationConfiguration existing)
    {
        if (request.SchemaDiscovery is not { } chosen)
            return GenericResult<string>.Success(existing.SchemaDiscovery);

        var offered = ConnectionTypes.ByName(existing.Implementation).SupportedDiscoveryTypes;
        if (offered.Any(option => string.Equals(option.Name, chosen, StringComparison.Ordinal)))
            return GenericResult<string>.Success(chosen);

        return SchemaDiscoveryRefusal.NotOffered<string>(
            Logger, request.Name, chosen, existing.Implementation, offered.Select(option => option.Name));
    }
}
