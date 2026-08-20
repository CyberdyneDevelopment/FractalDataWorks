using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Services.Connections.Validation;
using Fdw.Results;
using Fdw.Services.Connections;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Data.Endpoints.Logging;
using Fdw.Web.RestEndpoints.Crud;
using Fdw.Web.RestEndpoints.Logging;

namespace Fdw.Services.Data.Endpoints;

/// <summary>
/// Generic base endpoint for updating an existing data store configuration.
/// </summary>
/// <typeparam name="TConfig">The concrete data store configuration type.</typeparam>
public abstract class UpdateDataStoreEndpointBase<TConfig> : CrudUpdateEndpoint<UpdateDataStoreRequest, DataStoreDetailResponse>
    where TConfig : DataStoreConfiguration
{
    // Why: DataStoreConfigurationProvider provides dual-source (ctrl + cfg) merging
    // with full hierarchy assembly.
    private readonly DataStoreConfigurationProvider _dataStoreProvider;
    // Why: ConnectionConfigurationProvider replaces IOptionsMonitor<List<ConnectionConfiguration>>
    // for resolving ConnectionName -> ConnectionId.
    private readonly ConnectionConfigurationProvider _connectionProvider;

    /// <inheritdoc />
    protected UpdateDataStoreEndpointBase(
        DataStoreConfigurationProvider dataStoreProvider,
        ConnectionConfigurationProvider connectionProvider)
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
        var existingResult = await _dataStoreProvider.Get(request.Name, ct).ConfigureAwait(false);
        var existingConfig = existingResult.IsSuccess ? existingResult.Value as TConfig : null;

        if (existingConfig == null)
        {
            return GenericResult<DataStoreDetailResponse>.Failure(EndpointLogger.ResourceNotFound(Logger, "DataStore", request.Name));
        }

        // Why: Only resolve ConnectionId if the request provides a new ConnectionName.
        // Null/empty means "keep the existing connection" -- same partial-update semantics as other fields.
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

        // Why the paths are applied here and not left to UpdateConfiguration: every store type
        // implements that method for its own typed body, and the paths a store exposes are the same
        // shape whichever type it is. Applying them once keeps a new store type from having to
        // remember, which is how they came to be dropped in the first place.
        ApplyPaths(updated, request);

        var saveResult = await _dataStoreProvider.Save(updated, ct).ConfigureAwait(false);
        if (saveResult.IsFailure)
        {
            return saveResult.ToNewResult<DataStoreDetailResponse>();
        }

        return GenericResult<DataStoreDetailResponse>.Success(MapToDetail(updated, request));
    }

    /// <summary>Applies the update request to the existing configuration. Override for type-specific fields.</summary>
    protected abstract TConfig UpdateConfiguration(TConfig existing, UpdateDataStoreRequest request, Guid? resolvedConnectionId);

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

        // Why the existing id is carried over: the gateway reads Id = default as "insert", and a
        // path the store already has under that name is still current — so rebuilding every path as
        // a new record makes the cascade insert a duplicate and the unique index on
        // (DataStoreId, Name, IsCurrent) refuses it. Matching by name is what makes this an update
        // of the paths that stayed and an insert of only the ones that are new.
        var byName = target.Paths?.ToDictionary(p => p.Name, StringComparer.OrdinalIgnoreCase)
            ?? new Dictionary<string, DataPathConfiguration>(StringComparer.OrdinalIgnoreCase);

        target.Paths = request.Paths
            .Select(p => new DataPathConfiguration
            {
                Id = byName.TryGetValue(p.Name, out var existing) ? existing.Id : default,
                Name = p.Name,
                PathValue = p.PhysicalPath,
                Description = p.Description,
                DataStoreId = target.Id,
                Containers = byName.TryGetValue(p.Name, out var kept) ? kept.Containers : [],
            })
            .ToList();
    }

    /// <summary>Maps the saved configuration to a detail DTO. Override for type-specific fields.</summary>
    protected abstract DataStoreDetailResponse MapToDetail(TConfig savedConfig, UpdateDataStoreRequest request);

    // Why: Resolves connection name to ID via ConnectionConfigurationProvider instead of IOptionsMonitor.
    private async Task<Guid?> ResolveConnectionId(string connectionName, CancellationToken ct)
    {
        var connectionResult = await _connectionProvider.Get(connectionName, ct).ConfigureAwait(false);
        return connectionResult.IsSuccess ? connectionResult.Value?.Id : null;
    }
}
