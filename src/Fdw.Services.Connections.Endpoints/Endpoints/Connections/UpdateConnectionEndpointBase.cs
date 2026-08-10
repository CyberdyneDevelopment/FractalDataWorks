using System;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.Services.Abstractions;
using Fdw.Services.Connections.Abstractions;
using Fdw.Services.Connections.Endpoints.Logging;
using Fdw.Web.RestEndpoints.Crud;

namespace Fdw.Services.Connections.Endpoints;

/// <summary>
/// Generic base endpoint for updating an existing connection configuration.
/// Reads the composed aggregate through the connection provider, merges the request into it, and saves it
/// back in ONE call.
/// </summary>
/// <typeparam name="TConfig">The concrete typed body configuration type this endpoint merges.</typeparam>
/// <remarks>
/// Why one read and one save: <c>Get</c> already composes the header AND its typed body by dispatching on
/// <c>ServiceOptionType</c>, and <c>Save</c> writes the aggregate back the same way. Reading the two halves
/// through two providers let them drift apart, and saving them separately meant an update that touched only
/// the header left the body pointing at the previous version of it.
/// </remarks>
public abstract class UpdateConnectionEndpointBase<TConfig> : CrudUpdateEndpoint<UpdateConnectionRequest, ConnectionDetailDto>
    where TConfig : class, IConnectionConfiguration
{
    // Why: the connection provider reads and writes the whole aggregate — header plus dispatched typed body.
    private readonly ConnectionConfigurationProvider _connectionProvider;

    /// <inheritdoc />
    protected UpdateConnectionEndpointBase(ConnectionConfigurationProvider connectionProvider)
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

        // Why: Get composed the typed body already — it is the same dispatch the save uses, so the two
        // halves can never disagree about which type this connection is.
        if (connectionResult.Value.Configuration is not TConfig body)
            return GenericResult<ConnectionDetailDto?>.Success(null);

        return GenericResult<ConnectionDetailDto?>.Success(
            MapExistingToDetail(connectionResult.Value, body));
    }

    /// <summary>Merges the update request with the existing configuration and persists via the configurationGateway.</summary>
    protected override async Task<IGenericResult<ConnectionDetailDto>> Update(UpdateConnectionRequest request, ConnectionDetailDto existing, CancellationToken ct)
    {
        var connectionResult = await _connectionProvider.Get(request.Name, ct).ConfigureAwait(false);
        if (!connectionResult.IsSuccess || connectionResult.Value is null)
        {
            return GenericResult<ConnectionDetailDto>.Failure(
                ConnectionEndpointLog.ConnectionNotFound(Logger, request.Name));
        }

        if (connectionResult.Value.Configuration is not TConfig existingBody)
        {
            return GenericResult<ConnectionDetailDto>.Failure(
                ConnectionEndpointLog.ConnectionNotFound(Logger, request.Name));
        }

        var (updatedConnection, updatedBody) = MergeUpdate(request, connectionResult.Value, existingBody);

        // Why: the check-settings fields live on the shared header row (conn.Connection), so they
        // are merged once here rather than in every per-type MergeUpdate override — every connection
        // type gets the same behavior for free. Null means no change, matching every other nullable
        // field on UpdateConnectionRequest.
        updatedConnection.HealthCheckEnabled = request.HealthCheckEnabled ?? updatedConnection.HealthCheckEnabled;
        updatedConnection.HealthCheckOnStartup = request.HealthCheckOnStartup ?? updatedConnection.HealthCheckOnStartup;
        updatedConnection.HealthCheckIntervalSeconds = request.HealthCheckIntervalSeconds ?? updatedConnection.HealthCheckIntervalSeconds;

        // Why: same dead-config guard as CreateConnectionEndpointBase, evaluated against the fully
        // merged header so a request that only flips HealthCheckEnabled=true (leaving an existing
        // OnStartup/IntervalSeconds untouched) is still validated correctly.
        if (updatedConnection.HealthCheckEnabled && !updatedConnection.HealthCheckOnStartup && updatedConnection.HealthCheckIntervalSeconds is null)
        {
            return GenericResult<ConnectionDetailDto>.Failure(
                ConnectionEndpointLog.HealthCheckEnabledWithoutTrigger(Logger, request.Name));
        }

        // One save for the whole aggregate. The provider versions the header, then dispatches the merged
        // body to its typed provider so both halves land on the SAME new version — which is exactly what
        // saving them through two providers could not guarantee.
        updatedConnection.Configuration = updatedBody;

        var connectionSave = await _connectionProvider.Save(updatedConnection, ct).ConfigureAwait(false);
        if (connectionSave.IsFailure) return connectionSave.ToNewResult<ConnectionDetailDto>();

        return GenericResult<ConnectionDetailDto>.Success(MapUpdatedToDetail(updatedConnection, updatedBody));
    }

    /// <summary>
    /// Maps the existing parent connection and typed body to a detail DTO for the find phase.
    /// Override to include type-specific fields in the response.
    /// </summary>
    protected abstract ConnectionDetailDto MapExistingToDetail(ConnectionConfiguration connection, TConfig body);

    /// <summary>
    /// Merges the update request into the existing parent connection and typed body.
    /// Returns a tuple of the updated records. Override to handle type-specific field merges.
    /// </summary>
    protected abstract (ConnectionConfiguration connection, TConfig body) MergeUpdate(
        UpdateConnectionRequest request,
        ConnectionConfiguration existingConnection,
        TConfig existingBody);

    /// <summary>
    /// Maps the saved updated records to a detail DTO. Override to include type-specific fields.
    /// </summary>
    protected abstract ConnectionDetailDto MapUpdatedToDetail(ConnectionConfiguration connection, TConfig body);
}
