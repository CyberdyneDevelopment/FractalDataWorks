using System;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.Services.Connections;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Data.Endpoints.Logging;
using Fdw.Web.RestEndpoints.Crud;
using Microsoft.AspNetCore.Http;

namespace Fdw.Services.Data.Endpoints;

/// <summary>
/// Generic base endpoint for creating a new data store configuration.
/// </summary>
/// <typeparam name="TConfig">The concrete data store configuration type.</typeparam>
public abstract class CreateDataStoreEndpointBase<TConfig> : CrudCreateEndpoint<CreateDataStoreRequest, DataStoreDetailResponse>
    where TConfig : DataStoreConfiguration
{
    // Why: DataStoreConfigurationProvider provides dual-source (ctrl + cfg) merging
    // with full hierarchy assembly (DataStore -> DataPath -> DataContainer -> Field).
    private readonly DataStoreConfigurationProvider _dataStoreProvider;
    // Why: ConnectionConfigurationProvider replaces IOptionsMonitor<List<ConnectionConfiguration>>
    // for resolving ConnectionName -> ConnectionId.
    private readonly ConnectionConfigurationProvider _connectionProvider;

    /// <inheritdoc />
    protected CreateDataStoreEndpointBase(
        DataStoreConfigurationProvider dataStoreProvider,
        ConnectionConfigurationProvider connectionProvider)
    {
        _dataStoreProvider = dataStoreProvider;
        _connectionProvider = connectionProvider;
    }

    /// <summary>Gets the resource name used for route and policy generation.</summary>
    protected override string ResourceName => "datastores";

    /// <summary>Returns the data store name from the create request.</summary>
    protected override string GetResourceName(CreateDataStoreRequest request) => request.Name;

    /// <summary>Checks whether a data store with the requested name already exists.</summary>
    protected override async Task<IGenericResult<bool>> CheckExists(CreateDataStoreRequest request, CancellationToken ct)
    {
        var existResult = await _dataStoreProvider.Get(request.Name, ct).ConfigureAwait(false);
        return GenericResult<bool>.Success(existResult.IsSuccess && existResult.Value != null);
    }

    /// <summary>Creates the data store configuration and persists it via IConfigurationWriter.</summary>
    protected override async Task<IGenericResult<DataStoreDetailResponse>> Create(CreateDataStoreRequest request, CancellationToken ct)
    {
        var connectionResult = await _connectionProvider.Get(request.ConnectionName, ct).ConfigureAwait(false);
        if (!connectionResult.IsSuccess || connectionResult.Value is null)
        {
            return GenericResult<DataStoreDetailResponse>.Failure(
                DataStoreEndpointLog.ConnectionNotFound(Logger, request.ConnectionName, request.Name));
        }

        var connection = connectionResult.Value;
        var config = CreateConfiguration(request, connection.Id);

        // Why: a DataStore's transport IS its connection's transport — never a client-supplied StoreType.
        // The connection already carries ServiceOptionType (e.g. "MsSql", "Http", "FileSystem") which
        // is the authoritative transport identifier. Copying it here ensures ServiceOptionType is always
        // set to the connection's transport regardless of what (if anything) the subclass puts there.
        config.ServiceOptionType = connection.ServiceOptionType;

        var saveResult = await _dataStoreProvider.Save(config, ct).ConfigureAwait(false);
        if (saveResult.IsFailure)
        {
            return saveResult.ToNewResult<DataStoreDetailResponse>();
        }

        // Why: Reload from provider to get the full hierarchy (DataPaths, DataContainers, DataContainerFields).
        // The in-memory config object has Paths=[] because hierarchy is only populated by the cache.
        var loadResult = await _dataStoreProvider.Get(config.Name, ct).ConfigureAwait(false);
        if (loadResult.IsSuccess && loadResult.Value != null)
        {
            config.Paths = loadResult.Value.Paths;
        }
        else
        {
            // Why: Cache reload failed -- DataStore was saved but we can't populate Paths.
            // Return success anyway since the create succeeded; Paths will be empty in the response.
            DataStoreEndpointLog.CacheReloadFailed(Logger, config.Name);
        }

        return GenericResult<DataStoreDetailResponse>.Success(MapToDetail(config, request));
    }

    /// <summary>Builds a concrete data store configuration from the create request. Override for type-specific fields.</summary>
    protected abstract TConfig CreateConfiguration(CreateDataStoreRequest request, Guid connectionId);

    /// <summary>Maps the saved configuration to a detail DTO. Override for type-specific fields.</summary>
    protected abstract DataStoreDetailResponse MapToDetail(TConfig savedConfig, CreateDataStoreRequest request);

    /// <summary>Sends a 201 Created response with the data store detail.</summary>
    protected override Task SendCreatedResponse(DataStoreDetailResponse detail, CancellationToken ct)
    {
        return Send.ResponseAsync(detail, 201, ct);
    }

}
