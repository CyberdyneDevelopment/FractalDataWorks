using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.Services.Abstractions;
using Fdw.Services.Connections;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Data.Endpoints.Logging;
using Fdw.Web.RestEndpoints.Crud;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Data.Endpoints;

/// <summary>
/// Base endpoint for retrieving a specific data store by name, including paths and containers:
/// <c>POST /datastores/get</c>, its name in the body (FDW-978).
/// </summary>
public abstract class GetDataStoreEndpointBase : CrudGetEndpointBase<DataStoreNameRequest, DataStoreDetailResponse>
{
    private readonly DataStoreConfigurationProvider _dataStoreProvider;
    private readonly ConnectionNames _connectionNames;

    /// <summary>Initializes a new instance of the <see cref="GetDataStoreEndpointBase"/> class.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="dataStoreProvider">The data store configuration provider.</param>
    /// <param name="connectionProvider">The connection configuration provider for resolving ConnectionName.</param>
    /// <param name="authorization">Asks whether the caller may read connections, before the response names one.</param>
    protected GetDataStoreEndpointBase(
        ILogger<GetDataStoreEndpointBase> logger,
        DataStoreConfigurationProvider dataStoreProvider,
        ConnectionConfigurationProvider connectionProvider,
        IAuthorizationService authorization)
        : base(logger)
    {
        _dataStoreProvider = dataStoreProvider ?? throw new ArgumentNullException(nameof(dataStoreProvider));
        _connectionNames = new ConnectionNames(connectionProvider, authorization, logger);
    }

    /// <summary>Gets the resource name used for route and policy generation.</summary>
    protected override string ResourceName => "datastores";

    /// <summary>Gets whether the data store is read by POST with its name in the body: it is.</summary>
    protected override bool ReadByBody => true;

    /// <summary>Returns the data store name as the resource identifier.</summary>
    protected override string GetResourceIdentifier(DataStoreNameRequest request) => request.Name;

    /// <summary>Finds a data store by name and maps it to a detail DTO.</summary>
    protected override async Task<IGenericResult<DataStoreDetailResponse?>> FindByIdentifier(DataStoreNameRequest request, CancellationToken ct)
    {
        // The store and its paths, each path a record only: its containers are read when that
        // path is opened (/datastores/{name}/paths), not with the store (FDW-951).
        var configResult = await _dataStoreProvider.Get(request.Name, ConfigurationLevels.Children, ct).ConfigureAwait(false);
        if (configResult.IsFailure)
            return configResult.ToNewResult<DataStoreDetailResponse?>();

        // Not configured answers 404; a read that failed says why instead of answering 404.
        if (configResult.Value is null)
            return GenericResult<DataStoreDetailResponse?>.Success(null);

        if (string.IsNullOrEmpty(configResult.Value.Implementation))
        {
            return GenericResult<DataStoreDetailResponse?>.Failure(
                DataStoreEndpointLog.ImplementationMissing(Logger, request.Name));
        }

        // Connections are for administrators to see: the name is there only when this caller may read
        // connections and the row is visible to them. A connection that could not be READ is a fault to
        // report, not an absence, so it still fails.
        var connectionNames = await _connectionNames.Read(
            [configResult.Value.ConnectionId], HttpContext.User, ct).ConfigureAwait(false);
        if (connectionNames.IsFailure)
        {
            return GenericResult<DataStoreDetailResponse?>.Failure(
                DataStoreEndpointLog.ConnectionNameUnresolved(Logger, configResult.Value.ConnectionId, configResult.Value.Name));
        }

        var detail = MapToDetail(configResult.Value);
        detail.ConnectionName = connectionNames.Value!.Of(configResult.Value.ConnectionId, configResult.Value.Name);
        return GenericResult<DataStoreDetailResponse?>.Success(detail);
    }

    /// <summary>Maps a data store configuration to a detail DTO. Override for type-specific fields.</summary>
    protected virtual DataStoreDetailResponse MapToDetail(IDataStoreImplementationConfiguration config)
    {
        return new DataStoreDetailResponse
        {
            Id = config.Id,
            Name = config.Name,
            IsActive = config.IsActive,
            StoreType = config.Implementation,
            ConnectionId = config.ConnectionId,
            Description = config.Description,
            ClassificationId = config.ClassificationId,
            WriteMode = config.WriteMode,
            Paths = (config.Paths ?? []).Select(MapPath).ToList(),
            LastDiscoveredAt = config.LastDiscoveredAt,
            CreatedAt = config.CreatedAt,
            ModifiedAt = config.ModifyDate,
            CreatedBy = config.CreateBy,
            ModifiedBy = config.ModifyBy,
            CreatedOnBehalfOf = config.CreateOnBehalfOf,
            ModifiedOnBehalfOf = config.ModifyOnBehalfOf
        };
    }

    /// <summary>
    /// Maps a path configuration to a path DTO -- shallow: no nested containers or fields. A
    /// store's detail is the store plus its list of paths; a path's containers are fetched
    /// separately (GetDataStorePathsEndpointBase, /datastores/{name}/paths) only when that
    /// store's Containers view is actually opened, and a container's fields only when that
    /// container is (RUI perf fix: lazy-load hierarchies, not everything in one response).
    /// </summary>
    protected virtual DataStorePathResponse MapPath(DataPathConfiguration path)
    {
        return new DataStorePathResponse
        {
            Id = path.Id,
            Name = path.Name,
            PathType = path.PathType ?? string.Empty,
            PathValue = path.PathValue,
            Description = path.Description,
            ClassificationId = path.ClassificationId,
            SourceDescription = path.SourceDescription,
            Containers = []
        };
    }
}
