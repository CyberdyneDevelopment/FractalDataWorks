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
/// Base endpoint for listing all configured data stores.
/// </summary>
public abstract class ListDataStoresEndpointBase : CrudListEndpointBase<DataStoreSummaryResponse>
{
    // Why: DataStoreConfigurationProvider (dual-source) merges system (ctrl) and user (cfg) DataStore configs.
    private readonly DataStoreConfigurationProvider _dataStoreProvider;
    // Why: ConnectionConfigurationProvider (dual-source) replaces IConnectionProvider.GetAllConnectionConfigurations()
    // which was removed. Used to resolve connection names for display.
    private readonly ConnectionConfigurationProvider? _configProvider;

    /// <inheritdoc />
    protected ListDataStoresEndpointBase(
        DataStoreConfigurationProvider dataStoreProvider,
        ConnectionConfigurationProvider configProvider)
    {
        _dataStoreProvider = dataStoreProvider;
        _configProvider = configProvider;
    }

    /// <inheritdoc />
    protected ListDataStoresEndpointBase(DataStoreConfigurationProvider dataStoreProvider)
        : this(dataStoreProvider, null!)
    {
    }

    /// <summary>Gets the resource name used for route and policy generation.</summary>
    protected override string ResourceName => "datastores";

    /// <summary>Loads all data store configurations and maps them to summary DTOs.</summary>
    protected override async Task<IGenericResult<List<DataStoreSummaryResponse>>> LoadItems(CancellationToken ct)
    {
        var configsResult = await _dataStoreProvider.Get(ct).ConfigureAwait(false);
        if (!configsResult.IsSuccess)
        {
            return configsResult.ToNewResult<List<DataStoreSummaryResponse>>();
        }

        // Why: Pre-load all connection configs once so MapToSummary can resolve names
        // via dictionary lookup instead of per-item async calls.
        var connectionNameMap = await BuildConnectionNameMap(ct).ConfigureAwait(false);

        var configs = configsResult.Value ?? (IReadOnlyList<DataStoreConfiguration>)[];
        var items = MapConfigurations(configs, connectionNameMap).ToList();
        return GenericResult<List<DataStoreSummaryResponse>>.Success(items);
    }

    /// <summary>Filters and deduplicates configurations, then maps them to summary DTOs.</summary>
    protected virtual IReadOnlyList<DataStoreSummaryResponse> MapConfigurations(
        IReadOnlyList<DataStoreConfiguration> configurations,
        IReadOnlyDictionary<Guid, string> connectionNameMap)
    {
        return configurations
            .Where(config => !string.IsNullOrWhiteSpace(config.Name)
                && !string.IsNullOrWhiteSpace(config.ServiceOptionType)
                && !string.Equals(config.ServiceOptionType, "DataStore", StringComparison.OrdinalIgnoreCase))
            .GroupBy(config => config.Name, StringComparer.OrdinalIgnoreCase)
            .Select(group => group.First())
            .Select(config => MapToSummary(config, connectionNameMap))
            .ToList();
    }

    /// <summary>Maps a single data store configuration to a summary DTO.</summary>
    protected virtual DataStoreSummaryResponse MapToSummary(
        DataStoreConfiguration config,
        IReadOnlyDictionary<Guid, string> connectionNameMap)
    {
        connectionNameMap.TryGetValue(config.ConnectionId, out var connectionName);
        return new DataStoreSummaryResponse
        {
            Name = config.Name,
            StoreType = config.ServiceOptionType,
            ConnectionId = config.ConnectionId,
            ConnectionName = connectionName ?? string.Empty,
            Description = config.Description,
            PathCount = (config.Paths ?? []).Count,
            ContainerCount = (config.Paths ?? []).Sum(p => (p.Containers ?? []).Count),
            CreatedAt = config.CreateDate,
            ModifiedAt = config.ModifyDate,
            CreatedBy = config.CreateBy,
            ModifiedBy = config.ModifyBy,
            CreatedOnBehalfOf = config.CreateOnBehalfOf,
            ModifiedOnBehalfOf = config.ModifyOnBehalfOf
        };
    }

    private async Task<IReadOnlyDictionary<Guid, string>> BuildConnectionNameMap(CancellationToken ct)
    {
        if (_configProvider is null)
        {
            return new Dictionary<Guid, string>();
        }

        var result = await _configProvider.Get(ct).ConfigureAwait(false);
        if (!result.IsSuccess || result.Value is null)
        {
            return new Dictionary<Guid, string>();
        }

        return result.Value
            .Where(c => !string.IsNullOrWhiteSpace(c.Name))
            .ToDictionary(c => c.Id, c => c.Name);
    }
}
