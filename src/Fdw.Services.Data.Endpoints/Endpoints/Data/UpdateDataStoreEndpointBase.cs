using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Data.Abstractions.Results;
using Fdw.Data.Abstractions;
using Fdw.Services.Connections.Validation;
using Fdw.Results;
using Fdw.Services.Connections;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Data.Endpoints.Logging;
using Fdw.Web.RestEndpoints.Crud;
using Fdw.Web.RestEndpoints.Logging;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Data.Endpoints;

/// <summary>
/// Generic base endpoint for updating an existing data store configuration.
/// </summary>
/// <typeparam name="TConfig">The concrete data store configuration type.</typeparam>
public abstract class UpdateDataStoreEndpointBase<TConfig> : CrudUpdateEndpointBase<UpdateDataStoreRequest, DataStoreDetailResponse>
    where TConfig : class, IDataStoreImplementationConfiguration
{
    private readonly DataStoreConfigurationProvider _dataStoreProvider;
    private readonly ConnectionConfigurationProvider _connectionProvider;

    /// <inheritdoc />
    protected UpdateDataStoreEndpointBase(ILogger<UpdateDataStoreEndpointBase<TConfig>> logger, DataStoreConfigurationProvider dataStoreProvider,
        ConnectionConfigurationProvider connectionProvider) : base(logger)
    {
        _dataStoreProvider = dataStoreProvider;
        _connectionProvider = connectionProvider;
    }

    /// <summary>Gets the resource name used for route and policy generation.</summary>
    protected override string ResourceName => "datastores";

    /// <summary>Returns the data store name from the update request.</summary>
    protected override string GetResourceIdentifier(UpdateDataStoreRequest request) => request.Name;

    /// <summary>Finds the existing data store configuration to update.</summary>
    protected override async Task<IGenericResult<DataStoreDetailResponse?>> FindForUpdate(UpdateDataStoreRequest request, CancellationToken ct)
    {
        var configResult = await _dataStoreProvider.Get(request.Name, ct).ConfigureAwait(false);

        if (!configResult.IsSuccess || configResult.Value == null)
        {
            return GenericResult<DataStoreDetailResponse?>.Success(null);
        }

        var detail = MapToDetail((TConfig)configResult.Value, request);
        return GenericResult<DataStoreDetailResponse?>.Success(detail);
    }

    /// <summary>Updates the data store configuration and persists it via the DataGateway.</summary>
    protected override async Task<IGenericResult<DataStoreDetailResponse>> Update(UpdateDataStoreRequest request, DataStoreDetailResponse existing, CancellationToken ct)
    {
        var classified = DataClassificationValidator.Validate(ClassificationsNamedBy(request), Logger);
        if (classified.IsFailure) return classified.ToNewResult<DataStoreDetailResponse>();

        var existingResult = await _dataStoreProvider.Get(request.Name, ct).ConfigureAwait(false);
        var existingConfig = existingResult.IsSuccess ? existingResult.Value as TConfig : null;

        if (existingConfig == null)
        {
            return GenericResult<DataStoreDetailResponse>.Failure(EndpointLogger.ResourceNotFound(Logger, "DataStore", request.Name));
        }

        Guid? resolvedConnectionId = null;
        if (!string.IsNullOrEmpty(request.ConnectionName))
        {
            resolvedConnectionId = await ResolveConnectionId(request.ConnectionName, ct).ConfigureAwait(false);
            if (resolvedConnectionId is null)
            {
                return GenericResult<DataStoreDetailResponse>.Failure(
                    DataStoreEndpointLog.ConnectionNotFound(Logger, request.ConnectionName, request.Name));
            }
        }

        var updated = UpdateConfiguration(existingConfig, request, resolvedConnectionId);

        // Left out means unchanged, as for the other fields this PATCH carries.
        if (request.ClassificationId is { } classificationId)
        {
            updated.ClassificationId = classificationId;
        }

        // The response names the connection the store is now on. A PATCH that leaves the connection alone
        // carries no name, so the request cannot say; the store can. Read before the save so a connection
        // that cannot be read refuses the request, rather than answering after the change is already stored.
        var connectionName = await NameConnection(updated, ct).ConfigureAwait(false);
        if (connectionName.IsFailure || connectionName.Value is not { } name)
        {
            return connectionName.ToNewResult<DataStoreDetailResponse>();
        }

        ApplyPaths(updated, request);

        var saveResult = await _dataStoreProvider.Save(updated, "DataStore", updated.Implementation, updated.Name, ct).ConfigureAwait(false);
        if (saveResult.IsFailure)
        {
            return saveResult.ToNewResult<DataStoreDetailResponse>();
        }

        var detail = MapToDetail(updated, request);
        detail.ConnectionName = name;
        return GenericResult<DataStoreDetailResponse>.Success(detail);
    }

    /// <summary>Reads the name of the connection the store references.</summary>
    /// <remarks>
    /// The store's ConnectionId is a foreign key to conn.Connection, so a connection that cannot be read is
    /// a fault to report, not an absence to describe as an empty name.
    /// </remarks>
    private async Task<IGenericResult<string>> NameConnection(TConfig store, CancellationToken ct)
    {
        var connection = await _connectionProvider.Get(store.ConnectionId, ct).ConfigureAwait(false);
        if (connection.IsFailure || connection.Value is null)
        {
            return GenericResult<string>.Failure(
                DataStoreEndpointLog.ConnectionNameUnresolved(Logger, store.ConnectionId, store.Name));
        }

        return GenericResult<string>.Success(connection.Value.Name);
    }

    /// <summary>Applies the update request to the existing configuration. Override for type-specific fields.</summary>
    protected abstract TConfig UpdateConfiguration(TConfig existing, UpdateDataStoreRequest request, Guid? resolvedConnectionId);

    // The store's classification and each path's, checked before anything is written.
    private static IEnumerable<int> ClassificationsNamedBy(UpdateDataStoreRequest request)
    {
        // Left out, the store's classification is unchanged, so there is nothing to check.
        if (request.ClassificationId is { } named)
            yield return named;
        if (request.Paths is null)
            yield break;

        foreach (var path in request.Paths)
            yield return path.ClassificationId;
    }

    /// <summary>Replaces the store's paths with the ones the request carries.</summary>
    /// <param name="target">The configuration about to be saved.</param>
    /// <param name="request">The update being applied.</param>
    /// <remarks>
    /// Null means the caller said nothing about paths and the store keeps the ones it has; an empty
    /// list means the caller said it has none. The provider's Save cascades the result either way,
    /// so a path removed from the list is removed from the store.
    /// </remarks>
    protected static void ApplyPaths(TConfig target, UpdateDataStoreRequest request)
    {
        ArgumentNullException.ThrowIfNull(target);
        ArgumentNullException.ThrowIfNull(request);

        if (request.Paths is null)
        {
            return;
        }

        var byName = target.Paths?.ToDictionary(p => p.Name, StringComparer.OrdinalIgnoreCase)
            ?? new Dictionary<string, DataPathConfiguration>(StringComparer.OrdinalIgnoreCase);

        target.Paths = request.Paths
            .Select(p => new DataPathConfiguration
            {
                Id = byName.TryGetValue(p.Name, out var existing) ? existing.Id : default,
                Name = p.Name,
                PathValue = p.PhysicalPath,
                Description = p.Description,
                ClassificationId = p.ClassificationId,
                Containers = byName.TryGetValue(p.Name, out var kept) ? kept.Containers : [],
            })
            .ToList();
    }

    /// <summary>Maps the saved configuration to a detail DTO. Override for type-specific fields.</summary>
    protected abstract DataStoreDetailResponse MapToDetail(TConfig savedConfig, UpdateDataStoreRequest request);

    private async Task<Guid?> ResolveConnectionId(string connectionName, CancellationToken ct)
    {
        var connectionResult = await _connectionProvider.Get(connectionName, ct).ConfigureAwait(false);
        return connectionResult.IsSuccess ? connectionResult.Value?.Id : null;
    }
}
