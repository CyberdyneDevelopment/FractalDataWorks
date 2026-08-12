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
/// Base endpoint for retrieving paths for a specific data store.
/// </summary>
public abstract class GetDataStorePathsEndpointBase : CrudGetEndpoint<DataStoreNameRequest, List<DataStorePathResponse>>
{
    // Why: DataStoreConfigurationProvider (dual-source) merges system (ctrl) and user (cfg) DataStore configs.
    private readonly DataStoreConfigurationProvider _dataStoreProvider;

    /// <inheritdoc />
    protected GetDataStorePathsEndpointBase(DataStoreConfigurationProvider dataStoreProvider)
    {
        _dataStoreProvider = dataStoreProvider;
    }

    /// <summary>Gets the resource name used for route and policy generation.</summary>
    protected override string ResourceName => "datastores";

    /// <summary>Returns the data store name as the resource identifier.</summary>
    protected override string GetResourceIdentifier(DataStoreNameRequest request) => request.Name;

    /// <summary>Finds a data store by name and returns its paths.</summary>
    protected override async Task<IGenericResult<List<DataStorePathResponse>?>> FindByIdentifier(DataStoreNameRequest request, CancellationToken ct)
    {
        var configResult = await _dataStoreProvider.Get(request.Name, ct).ConfigureAwait(false);

        if (!configResult.IsSuccess || configResult.Value == null)
        {
            return GenericResult<List<DataStorePathResponse>?>.Success(null);
        }

        var paths = (configResult.Value.Paths ?? []).Select(MapPath).ToList();
        return GenericResult<List<DataStorePathResponse>?>.Success(paths);
    }

    /// <summary>Maps a path configuration to a path DTO.</summary>
    protected virtual DataStorePathResponse MapPath(DataPathConfiguration path)
    {
        return new DataStorePathResponse
        {
            Id = path.Id,
            Name = path.Name,
            PathType = path.PathType ?? string.Empty,
            PathName = path.PathName,
            Description = path.Description,
            Containers = (path.Containers ?? []).Select(MapContainer).ToList()
        };
    }

    /// <summary>Maps a container configuration to a container summary DTO.</summary>
    protected virtual DataStoreContainerResponse MapContainer(DataContainerConfiguration container)
    {
        return new DataStoreContainerResponse
        {
            Id = container.Id,
            Name = container.Name,
            // Why: TypeId replaces ContainerType after Wave A5 DDL rename.
            ContainerType = container.TypeId ?? string.Empty,
            FieldCount = (container.Fields ?? []).Count
        };
    }
}
