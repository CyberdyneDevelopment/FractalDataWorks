using System;
using System.Threading;
using System.Threading.Tasks;
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

        var saveResult = await _dataStoreProvider.Save(updated, ct).ConfigureAwait(false);
        if (saveResult.IsFailure)
        {
            return saveResult.ToNewResult<DataStoreDetailResponse>();
        }

        return GenericResult<DataStoreDetailResponse>.Success(MapToDetail(updated, request));
    }

    /// <summary>Applies the update request to the existing configuration. Override for type-specific fields.</summary>
    protected abstract TConfig UpdateConfiguration(TConfig existing, UpdateDataStoreRequest request, Guid? resolvedConnectionId);

    /// <summary>Maps the saved configuration to a detail DTO. Override for type-specific fields.</summary>
    protected abstract DataStoreDetailResponse MapToDetail(TConfig savedConfig, UpdateDataStoreRequest request);

    // Why: Resolves connection name to ID via ConnectionConfigurationProvider instead of IOptionsMonitor.
    private async Task<Guid?> ResolveConnectionId(string connectionName, CancellationToken ct)
    {
        var connectionResult = await _connectionProvider.Get(connectionName, ct).ConfigureAwait(false);
        return connectionResult.IsSuccess ? connectionResult.Value?.Id : null;
    }
}
