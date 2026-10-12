using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.Services.Connections;
using Fdw.Services.Data;
using Fdw.Web.RestEndpoints.Crud;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Data.Endpoints;

/// <summary>
/// Base endpoint for listing all containers across all data stores.
/// </summary>
/// <remarks>
/// Reads every store down to its containers and no further (FDW-951): their fields, keys and path
/// policies are not composed to name them, so the item carries no field count. A container's fields
/// are read on their own route, when it is opened.
/// </remarks>
public abstract class ListDataStoreContainersEndpointBase : CrudListEndpointBase<DataStoreContainerWithPathDto>
{
    // A store's paths are one level under it and their containers two.
    private const int ThroughContainers = 2;

    private readonly DataStoreConfigurationProvider _dataStoreProvider;

    /// <inheritdoc />
    protected ListDataStoreContainersEndpointBase(ILogger<ListDataStoreContainersEndpointBase> logger, DataStoreConfigurationProvider dataStoreProvider) : base(logger)
    {
        _dataStoreProvider = dataStoreProvider;
    }

    /// <summary>Gets the resource name used for route and policy generation.</summary>
    protected override string ResourceName => "datastore-containers";

    /// <summary>Loads every store's containers and maps them to DTOs.</summary>
    protected override async Task<IGenericResult<List<DataStoreContainerWithPathDto>>> LoadItems(CancellationToken ct)
    {
        var stores = await _dataStoreProvider.Get(ThroughContainers, ct).ConfigureAwait(false);
        if (stores.IsFailure)
        {
            return stores.ToNewResult<List<DataStoreContainerWithPathDto>>();
        }

        var containers = stores.Value!
            .Where(config => !string.IsNullOrWhiteSpace(config.Name))
            .SelectMany(config => config.Paths
                .SelectMany(path => path.Containers
                    .Select(container => MapContainerWithPath(config, path, container))))
            .ToList();

        return GenericResult<List<DataStoreContainerWithPathDto>>.Success(containers);
    }

    /// <summary>Maps a container with its parent path and data store information.</summary>
    protected virtual DataStoreContainerWithPathDto MapContainerWithPath(
        IDataStoreImplementationConfiguration dataStore,
        DataPathConfiguration path,
        DataContainerConfiguration container)
    {
        return new DataStoreContainerWithPathDto
        {
            Id = container.Id,
            DataStoreName = dataStore.Name,
            PathName = path.Name,
            ContainerName = container.Name,
            ContainerType = container.TypeId ?? string.Empty,
            ClassificationId = container.ClassificationId
        };
    }
}
