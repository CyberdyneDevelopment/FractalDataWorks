using System;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.Services.Abstractions;
using Fdw.Services.Connections.Abstractions;
using Fdw.Services.Connections.Endpoints.Logging;
using Fdw.Web.RestEndpoints.Crud;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Connections.Endpoints;

/// <summary>
/// Generic base endpoint for updating an existing connection configuration.
/// Reads the composed aggregate through the connection provider, merges the request into it, and saves it
/// back in ONE call.
/// </summary>
/// <typeparam name="TConfig">The concrete typed body configuration type this endpoint merges.</typeparam>
/// <remarks>
/// Why one read and one save: <c>Get</c> already composes the header AND its typed body by dispatching on
/// <c>Implementation</c>, and <c>Save</c> writes the aggregate back the same way. Reading the two halves
/// through two providers let them drift apart, and saving them separately meant an update that touched only
/// the header left the body pointing at the previous version of it.
/// </remarks>
public abstract class UpdateConnectionEndpointBase<TConfig> : CrudUpdateEndpointBase<UpdateConnectionRequest, ConnectionDetailDto>
    where TConfig : class, IConnectionImplementationConfiguration
{
    private readonly ConnectionConfigurationProvider _connectionProvider;

    /// <inheritdoc />
    protected UpdateConnectionEndpointBase(ILogger<UpdateConnectionEndpointBase<TConfig>> logger, ConnectionConfigurationProvider connectionProvider) : base(logger)
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

        if (connectionResult.Value is not TConfig existing)
            return GenericResult<ConnectionDetailDto?>.Success(null);

        return GenericResult<ConnectionDetailDto?>.Success(MapExistingToDetail(existing));
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

        if (connectionResult.Value is not TConfig existingConnection)
        {
            return GenericResult<ConnectionDetailDto>.Failure(
                ConnectionEndpointLog.ConnectionNotFound(Logger, request.Name));
        }

        var updatedConnection = MergeUpdate(request, existingConnection);

        updatedConnection.HealthCheckEnabled = request.HealthCheckEnabled ?? updatedConnection.HealthCheckEnabled;
        updatedConnection.HealthCheckOnStartup = request.HealthCheckOnStartup ?? updatedConnection.HealthCheckOnStartup;
        updatedConnection.HealthCheckIntervalSeconds = request.HealthCheckIntervalSeconds ?? updatedConnection.HealthCheckIntervalSeconds;

        if (updatedConnection.HealthCheckEnabled && !updatedConnection.HealthCheckOnStartup && updatedConnection.HealthCheckIntervalSeconds is null)
        {
            return GenericResult<ConnectionDetailDto>.Failure(
                ConnectionEndpointLog.HealthCheckEnabledWithoutTrigger(Logger, request.Name));
        }

        // One save, one record: the provider versions the implementation row and writes everything under
        // it. The name identifies the member, so the existing domain row is the one this hangs from.
        var connectionSave = await _connectionProvider.Save(updatedConnection, "Connection", updatedConnection.Implementation, request.Name, ct).ConfigureAwait(false);
        if (connectionSave.IsFailure) return connectionSave.ToNewResult<ConnectionDetailDto>();

        return GenericResult<ConnectionDetailDto>.Success(MapUpdatedToDetail(updatedConnection));
    }

    /// <summary>
    /// Maps the existing connection configuration to a detail DTO for the find phase.
    /// Override to include type-specific fields in the response.
    /// </summary>
    protected abstract ConnectionDetailDto MapExistingToDetail(TConfig connection);

    /// <summary>
    /// Merges the update request into the existing connection configuration and returns the updated record.
    /// Override to handle type-specific field merges.
    /// </summary>
    protected abstract TConfig MergeUpdate(UpdateConnectionRequest request, TConfig existingConnection);

    /// <summary>
    /// Maps the saved record to a detail DTO. Override to include type-specific fields.
    /// </summary>
    protected abstract ConnectionDetailDto MapUpdatedToDetail(TConfig connection);
}
