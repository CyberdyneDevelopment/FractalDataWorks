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
/// Base endpoint for listing all containers across all data stores.
/// </summary>
public abstract class ListDataStoreContainersEndpointBase : CrudListEndpointBase<DataStoreContainerWithPathDto>
{
    // Why: DataStoreConfigurationProvider (dual-source) merges system (ctrl) and user (cfg) DataStore configs.
    private readonly DataStoreConfigurationProvider _dataStoreProvider;

    /// <inheritdoc />
    protected ListDataStoreContainersEndpointBase(DataStoreConfigurationProvider dataStoreProvider)
    {
        _dataStoreProvider = dataStoreProvider;
    }

    /// <summary>Gets the resource name used for route and policy generation.</summary>
    protected override string ResourceName => "datastore-containers";

    /// <summary>Loads all containers from all data stores and maps them to DTOs.</summary>
    protected override async Task<IGenericResult<List<DataStoreContainerWithPathDto>>> LoadItems(CancellationToken ct)
    {
        var configsResult = await _dataStoreProvider.Get(ct).ConfigureAwait(false);
        if (!configsResult.IsSuccess)
        {
            return configsResult.ToNewResult<List<DataStoreContainerWithPathDto>>();
        }

        var configs = configsResult.Value ?? (IReadOnlyList<DataStoreConfiguration>)[];

        var containers = configs
            .Where(config => !string.IsNullOrWhiteSpace(config.Name))
            .SelectMany(config => (config.Paths ?? [])
                .SelectMany(path => (path.Containers ?? [])
                    .Select(container => MapContainerWithPath(config, path, container))))
            .ToList();

        return GenericResult<List<DataStoreContainerWithPathDto>>.Success(containers);
    }

    /// <summary>Maps a container with its parent path and data store information.</summary>
    protected virtual DataStoreContainerWithPathDto MapContainerWithPath(
        DataStoreConfiguration dataStore,
        DataPathConfiguration path,
        DataContainerConfiguration container)
    {
        return new DataStoreContainerWithPathDto
        {
            Id = container.Id,
            DataStoreName = dataStore.Name,
            PathName = path.Name,
            ContainerName = container.Name,
            // Why: TypeId replaces ContainerType after Wave A5 DDL rename.
            ContainerType = container.TypeId ?? string.Empty,
            FieldCount = (container.Fields ?? []).Count
        };
    }
}
