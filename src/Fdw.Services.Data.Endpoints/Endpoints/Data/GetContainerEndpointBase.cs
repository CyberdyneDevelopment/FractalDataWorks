using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.Services.Connections;
using Fdw.Services.Data;
using Fdw.Web.RestEndpoints.Crud;

namespace Fdw.Services.Data.Endpoints;

/// <summary>
/// Base endpoint for retrieving a specific container by data store, path, and container name.
/// </summary>
public abstract class GetContainerEndpointBase : CrudGetEndpointBase<GetContainerRequest, DataStoreContainerDetailDto>
{
    private readonly DataStoreConfigurationProvider _dataStoreProvider;

    /// <inheritdoc />
    protected GetContainerEndpointBase(DataStoreConfigurationProvider dataStoreProvider)
    {
        _dataStoreProvider = dataStoreProvider;
    }

    /// <summary>Gets the resource name used for route and policy generation.</summary>
    protected override string ResourceName => "datastore-containers";

    /// <summary>Returns the container identifier as a composite key.</summary>
    protected override string GetResourceIdentifier(GetContainerRequest request) =>
        $"{request.Name}/{request.PathName}/{request.ContainerName}";

    /// <summary>Finds a container by data store, path, and container name.</summary>
    protected override async Task<IGenericResult<DataStoreContainerDetailDto?>> FindByIdentifier(GetContainerRequest request, CancellationToken ct)
    {
        var storeResult = await _dataStoreProvider.Get(request.Name, ct).ConfigureAwait(false);

        if (!storeResult.IsSuccess || storeResult.Value == null)
        {
            return GenericResult<DataStoreContainerDetailDto?>.Success(null);
        }

        var storeConfig = storeResult.Value;
        var path = (storeConfig.Paths ?? [])
            .FirstOrDefault(p => string.Equals(p.Name, request.PathName, StringComparison.OrdinalIgnoreCase));

        if (path == null)
        {
            return GenericResult<DataStoreContainerDetailDto?>.Success(null);
        }

        var container = (path.Containers ?? [])
            .FirstOrDefault(c => string.Equals(c.Name, request.ContainerName, StringComparison.OrdinalIgnoreCase));

        DataStoreContainerDetailDto? detail = container != null ? MapToDetail(container) : null;
        return GenericResult<DataStoreContainerDetailDto?>.Success(detail);
    }

    /// <summary>Maps a container configuration to a detail DTO with fields.</summary>
    protected virtual DataStoreContainerDetailDto MapToDetail(DataContainerConfiguration container)
    {
        return new DataStoreContainerDetailDto
        {
            Id = container.Id,
            Name = container.Name,
            ContainerType = container.TypeId ?? string.Empty,
            Description = container.Description,
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
            FrameworkDataType = field.DataType,
            IsNullable = false,
            IsKey = false,
            Ordinal = 0,
            Description = field.Description
        };
    }
}
