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
/// Base endpoint for retrieving a specific container by its identifier.
/// </summary>
public abstract class GetContainerByIdEndpointBase : CrudGetEndpoint<ContainerIdRequest, DataStoreContainerDetailDto>
{
    // Why: DataStoreConfigurationProvider (dual-source) merges system (ctrl) and user (cfg) DataStore configs.
    private readonly DataStoreConfigurationProvider _dataStoreProvider;

    /// <inheritdoc />
    protected GetContainerByIdEndpointBase(DataStoreConfigurationProvider dataStoreProvider)
    {
        _dataStoreProvider = dataStoreProvider;
    }

    /// <summary>Gets the resource name used for route and policy generation.</summary>
    protected override string ResourceName => "datastore-containers";

    /// <summary>Returns the container identifier.</summary>
    protected override string GetResourceIdentifier(ContainerIdRequest request) => request.Id.ToString();

    /// <summary>Finds a container by its identifier.</summary>
    protected override async Task<IGenericResult<DataStoreContainerDetailDto?>> FindByIdentifier(ContainerIdRequest request, CancellationToken ct)
    {
        var allResult = await _dataStoreProvider.Get(ct).ConfigureAwait(false);
        if (!allResult.IsSuccess)
        {
            return GenericResult<DataStoreContainerDetailDto?>.Success(null);
        }

        var allConfigs = allResult.Value ?? (IReadOnlyList<DataStoreConfiguration>)[];

        DataContainerConfiguration? container = null;

        foreach (var dataStore in allConfigs)
        {
            foreach (var path in dataStore.Paths ?? [])
            {
                container = (path.Containers ?? []).FirstOrDefault(c => c.Id == request.Id);
                if (container != null)
                    break;
            }

            if (container != null)
                break;
        }

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
            // Why: TypeId replaces ContainerType after Wave A5 DDL rename.
            ContainerType = container.TypeId ?? string.Empty,
            Description = container.Description,
            Fields = (container.Fields ?? []).Select(MapField).ToList()
        };
    }

    /// <summary>Maps a field configuration to a field DTO.</summary>
    protected virtual DataStoreFieldResponse MapField(DataContainerFieldConfiguration field)
    {
        // Why: IsNullable/Ordinal moved to data.MsSqlDataContainerField typed body after Wave A5.
        // NativeDataType, IsNullable, and Ordinal will be sourced from the typed body in Wave B2.
        return new DataStoreFieldResponse
        {
            Id = field.Id,
            Name = field.Name,
            NativeDataType = field.DataType,
            FrameworkDataType = field.DataType,
            IsNullable = false,
            // Why: IsPrimaryKey removed from DataContainerFieldConfiguration — key role expressed via Keys collection.
            IsKey = false,
            Ordinal = 0,
            Description = field.Description
        };
    }
}
