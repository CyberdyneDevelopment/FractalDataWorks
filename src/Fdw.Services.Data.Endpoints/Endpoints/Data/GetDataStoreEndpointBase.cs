using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.Services.Connections;
using Fdw.Services.Data.Abstractions;
using Fdw.Web.RestEndpoints.Crud;

namespace Fdw.Services.Data.Endpoints;

/// <summary>
/// Base endpoint for retrieving a specific data store by name, including paths and containers.
/// </summary>
public abstract class GetDataStoreEndpointBase : CrudGetEndpointBase<DataStoreNameRequest, DataStoreDetailResponse>
{
    private readonly DataStoreConfigurationProvider _dataStoreProvider;
    private readonly ConnectionConfigurationProvider? _connectionProvider;

    /// <inheritdoc />
    protected GetDataStoreEndpointBase(DataStoreConfigurationProvider dataStoreProvider)
        : this(dataStoreProvider, null)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="GetDataStoreEndpointBase"/> class
    /// with connection resolution support.
    /// </summary>
    /// <param name="dataStoreProvider">The data store configuration provider.</param>
    /// <param name="connectionProvider">The connection configuration provider for resolving ConnectionName.</param>
    protected GetDataStoreEndpointBase(
        DataStoreConfigurationProvider dataStoreProvider,
        ConnectionConfigurationProvider? connectionProvider)
    {
        _dataStoreProvider = dataStoreProvider;
        _connectionProvider = connectionProvider;
    }

    /// <summary>Gets the resource name used for route and policy generation.</summary>
    protected override string ResourceName => "datastores";

    /// <summary>Returns the data store name as the resource identifier.</summary>
    protected override string GetResourceIdentifier(DataStoreNameRequest request) => request.Name;

    /// <summary>Finds a data store by name and maps it to a detail DTO.</summary>
    protected override async Task<IGenericResult<DataStoreDetailResponse?>> FindByIdentifier(DataStoreNameRequest request, CancellationToken ct)
    {
        var configResult = await _dataStoreProvider.Get(request.Name, ct).ConfigureAwait(false);

        if (!configResult.IsSuccess || configResult.Value is null)
        {
            // Not-found is not an error at this level -- return null to trigger 404
            return GenericResult<DataStoreDetailResponse?>.Success(null);
        }

        DataStoreDetailResponse? detail = MapToDetail(configResult.Value);
        return GenericResult<DataStoreDetailResponse?>.Success(detail);
    }

    /// <summary>Maps a data store configuration to a detail DTO. Override for type-specific fields.</summary>
    protected virtual DataStoreDetailResponse MapToDetail(DataStoreConfiguration config)
    {
        return new DataStoreDetailResponse
        {
            Id = config.Id,
            Name = config.Name,
            StoreType = config.ServiceOptionType ?? "Unknown",
            ConnectionId = config.ConnectionId,
            ConnectionName = ResolveConnectionName(config.ConnectionId),
            Description = config.Description,
            WriteMode = config.WriteMode,
            Paths = (config.Paths ?? []).Select(MapPath).ToList(),
            LastDiscoveredAt = config.LastDiscoveredAt,
            CreatedAt = config.CreateDate,
            ModifiedAt = config.ModifyDate,
            CreatedBy = config.CreateBy,
            ModifiedBy = config.ModifyBy,
            CreatedOnBehalfOf = config.CreateOnBehalfOf,
            ModifiedOnBehalfOf = config.ModifyOnBehalfOf
        };
    }

    /// <summary>Maps a path configuration to a path DTO.</summary>
    protected virtual DataStorePathResponse MapPath(DataPathConfiguration path)
    {
        return new DataStorePathResponse
        {
            Id = path.Id,
            Name = path.Name,
            PathType = path.PathType ?? string.Empty,
            PathValue = path.PathValue,
            Description = path.Description,
            SourceDescription = path.SourceDescription,
            Containers = (path.Containers ?? []).Select(MapContainer).ToList()
        };
    }

    /// <summary>Maps a container configuration to a container DTO including fields.</summary>
    protected virtual DataStoreContainerResponse MapContainer(DataContainerConfiguration container)
    {
        return new DataStoreContainerResponse
        {
            Id = container.Id,
            Name = container.Name,
            ContainerType = container.TypeId ?? string.Empty,
            FieldCount = (container.Fields ?? []).Count,
            SourceDescription = null,
            SurrogateKeyFields = [],
            NaturalKeyFields = [],
            Fields = (container.Fields ?? []).Select(MapField).ToList()
        };
    }

    /// <summary>Maps a field configuration to a field DTO.</summary>
    protected virtual DataStoreFieldResponse MapField(DataContainerFieldConfiguration field)
    {
        return new DataStoreFieldResponse
        {
            Id = field.Id,
            Name = field.Name,
            NativeDataType = field.DataType,
            IsNullable = false,
            IsKey = false,
            Ordinal = 0,
            Description = field.Description
        };
    }

#pragma warning disable VSTHRD002
    private string ResolveConnectionName(Guid connectionId)
    {
        if (_connectionProvider is null)
        {
            return string.Empty;
        }

        var connectionResult = _connectionProvider.Get(connectionId).GetAwaiter().GetResult();
        var connection = connectionResult.IsSuccess ? connectionResult.Value : null;
        return connection?.Name ?? string.Empty;
    }
#pragma warning restore VSTHRD002
}
