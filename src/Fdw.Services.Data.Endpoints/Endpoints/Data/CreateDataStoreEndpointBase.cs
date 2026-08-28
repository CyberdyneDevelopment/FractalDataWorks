using System;
using System.Linq;
using System.Collections.Generic;
using Fdw.Services.Connections.Validation;
using Fdw.Services.Data.Clients.Models;
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
public abstract class CreateDataStoreEndpointBase<TConfig> : CrudCreateEndpointBase<CreateDataStoreRequest, DataStoreDetailResponse>
    where TConfig : DataStoreConfiguration
{
    private readonly DataStoreConfigurationProvider _dataStoreProvider;
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

        ApplyPaths(config, request.Paths);

        config.ServiceOptionType = connection.ServiceOptionType;

        var saveResult = await _dataStoreProvider.Save(config, ct).ConfigureAwait(false);
        if (saveResult.IsFailure)
        {
            return saveResult.ToNewResult<DataStoreDetailResponse>();
        }

        var loadResult = await _dataStoreProvider.Get(config.Name, ct).ConfigureAwait(false);
        if (loadResult.IsSuccess && loadResult.Value != null)
        {
            config.Paths = loadResult.Value.Paths;
        }
        else
        {
            DataStoreEndpointLog.CacheReloadFailed(Logger, config.Name);
        }

        return GenericResult<DataStoreDetailResponse>.Success(MapToDetail(config, request));
    }

    /// <summary>Builds a concrete data store configuration from the create request. Override for type-specific fields.</summary>
    protected abstract TConfig CreateConfiguration(CreateDataStoreRequest request, Guid connectionId);

    /// <summary>Puts the requested paths on the configuration before it is saved.</summary>
    /// <param name="target">The configuration about to be saved.</param>
    /// <param name="paths">The paths the caller asked for, if any.</param>
    protected static void ApplyPaths(TConfig target, IList<DataPathRequest>? paths)
    {
        ArgumentNullException.ThrowIfNull(target);

        if (paths is null || paths.Count == 0)
        {
            return;
        }

        target.Paths = paths
            .Select(p => new DataPathConfiguration
            {
                Name = p.Name,
                PathValue = p.PhysicalPath,
                Description = p.Description,
                DataStoreId = target.Id,
            })
            .ToList();
    }

    /// <summary>Maps the saved configuration to a detail DTO. Override for type-specific fields.</summary>
    protected abstract DataStoreDetailResponse MapToDetail(TConfig savedConfig, CreateDataStoreRequest request);

    /// <summary>Sends a 201 Created response with the data store detail.</summary>
    protected override Task SendCreatedResponse(DataStoreDetailResponse detail, CancellationToken ct)
    {
        return Send.ResponseAsync(detail, 201, ct);
    }

}
