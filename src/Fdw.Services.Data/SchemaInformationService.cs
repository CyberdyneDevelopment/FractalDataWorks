using Fdw.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Data.Abstractions;
using Fdw.Data.DataStores.Abstractions;
using Fdw.Messages;
using Fdw.Results;
using Fdw.Services.Configuration;
using Fdw.Services.Connections;
using Fdw.Services.Connections.Abstractions;
using Fdw.Services.Connections.Commands;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Data.Commands;
using Fdw.Services.Data.Logging;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Services.Data;

/// <summary>
/// Provides on-demand schema discovery for named connections.
/// Caches results via <see cref="DataStoreConfigurationProvider"/>; persists newly discovered
/// metadata as the DataStore's paths, containers and fields.
/// </summary>
/// <remarks>
/// GetSchema is the cache-first path: if a DataStore already exists for this connection,
/// its persisted metadata is returned immediately. Otherwise discovery runs and results
/// are persisted before returning.
/// RefreshSchema always re-discovers, enabling the UI "Re-discover" action.
/// The paths, containers and fields are the store's own children, so what discovery finds is merged
/// into the store and the store is saved once, through the DataStore domain.
/// </remarks>
public sealed class SchemaInformationService : ISchemaInformationService
{
    private readonly IConnectionProvider _connectionProvider;
    private readonly ConnectionConfigurationProvider _configProvider;
    private readonly DataStoreConfigurationProvider _dataStoreProvider;
    private readonly ILogger<SchemaInformationService> _logger;

    /// <summary>
    /// Initializes a new instance of <see cref="SchemaInformationService"/>.
    /// </summary>
    public SchemaInformationService(
        IConnectionProvider connectionProvider,
        ConnectionConfigurationProvider configProvider,
        DataStoreConfigurationProvider dataStoreProvider,
        ILogger<SchemaInformationService>? logger = null)
    {
        _connectionProvider = connectionProvider;
        _configProvider = configProvider;
        _dataStoreProvider = dataStoreProvider;
        _logger = logger ?? NullLogger<SchemaInformationService>.Instance;
    }

    /// <inheritdoc/>
    public async Task<IGenericResult<SchemaInformation>> GetSchema(
        string connectionName,
        CancellationToken cancellationToken = default)
    {
        SchemaInformationLog.GetSchemaStarted(_logger, connectionName);

        var configResult = await ResolveConnectionConfig(connectionName, cancellationToken).ConfigureAwait(false);
        if (!configResult.IsSuccess || configResult.Value == null)
            return configResult.ToNewResult<SchemaInformation>();

        var config = configResult.Value;

        if (!config.DiscoveryEnabled)
        {
            return GenericResult<SchemaInformation>.Failure(
                SchemaInformationLog.DiscoveryDisabled(_logger, connectionName));
        }

        // Check cache: if a DataStore already exists for this connection, return it immediately
        var cachedResult = await TryGetFromCache(config, cancellationToken).ConfigureAwait(false);
        if (cachedResult != null)
        {
            SchemaInformationLog.CacheHit(_logger, connectionName, cachedResult.DataStore.Name);
            return GenericResult<SchemaInformation>.Success(cachedResult);
        }

        SchemaInformationLog.CacheMiss(_logger, connectionName);
        return await DiscoverAndReturn(config, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async Task<IGenericResult<SchemaInformation>> RefreshSchema(
        string connectionName,
        CancellationToken cancellationToken = default)
    {
        SchemaInformationLog.RefreshStarted(_logger, connectionName);

        var configResult = await ResolveConnectionConfig(connectionName, cancellationToken).ConfigureAwait(false);
        if (!configResult.IsSuccess || configResult.Value == null)
            return configResult.ToNewResult<SchemaInformation>();

        var config = configResult.Value;

        if (!config.DiscoveryEnabled)
        {
            return GenericResult<SchemaInformation>.Failure(
                SchemaInformationLog.DiscoveryDisabled(_logger, connectionName));
        }

        return await DiscoverAndReturn(config, cancellationToken).ConfigureAwait(false);
    }

    private async Task<IGenericResult<IConnectionImplementationConfiguration>> ResolveConnectionConfig(
        string connectionName,
        CancellationToken cancellationToken)
    {
        var result = await _configProvider.Get(connectionName, cancellationToken).ConfigureAwait(false);
        if (!result.IsSuccess || result.Value is null)
        {
            return GenericResult<IConnectionImplementationConfiguration>.Failure(
                SchemaInformationLog.ConnectionConfigNotFound(_logger, connectionName));
        }

        return GenericResult<IConnectionImplementationConfiguration>.Success(result.Value);
    }

    private async Task<SchemaInformation?> TryGetFromCache(
        IConnectionImplementationConfiguration config,
        CancellationToken cancellationToken)
    {
        var allConfigsResult = await _dataStoreProvider.Get(cancellationToken).ConfigureAwait(false);
        var allConfigs = allConfigsResult.IsSuccess ? allConfigsResult.Value! : (IReadOnlyList<IDataStoreImplementationConfiguration>)[];
        var dataStore = allConfigs.FirstOrDefault(ds => ds.ConnectionId == config.Id);
        return dataStore != null ? new SchemaInformation(dataStore) : null;
    }

    private async Task<IGenericResult<SchemaInformation>> DiscoverAndReturn(
        IConnectionImplementationConfiguration config,
        CancellationToken cancellationToken)
    {
        var connectionName = config.Name;
        var connectionType = config.Implementation;

        if (string.IsNullOrEmpty(connectionType))
        {
            return GenericResult<SchemaInformation>.Failure(
                SchemaInformationLog.ConnectionTypeMissing(_logger, connectionName));
        }

        var connType = ConnectionTypes.ByName(connectionType);
        if (connType == ConnectionTypes.NotFound || connType is not ISchemaDiscovery schemaDiscovery)
        {
            return GenericResult<SchemaInformation>.Failure(
                SchemaInformationLog.ConnectionTypeNotDiscoverable(_logger, connectionName, connectionType));
        }

        // Build a live connection for direct discovery (no pre-existing DataStore row required)
        var connectionResult = await _connectionProvider.Get(connectionName, cancellationToken).ConfigureAwait(false);
        if (!connectionResult.IsSuccess || connectionResult.Value == null)
        {
            IGenericMessage msg;
            var upstreamError = connectionResult.CurrentMessage;
            if (upstreamError is not null)
                msg = SchemaInformationLog.ConnectionBuildFailed(_logger, connectionName, upstreamError);
            else
                msg = SchemaInformationLog.ConnectionBuildFailedNoDetails(_logger, connectionName);
            return GenericResult<SchemaInformation>.Failure(msg);
        }

        SchemaInformationLog.DiscoveryStarting(_logger, connectionName, connectionType);

        var discoveryOptions = BuildDiscoveryOptions(config);
        var discoverResult = await schemaDiscovery
            .DiscoverSchema(connectionResult.Value, discoveryOptions, cancellationToken)
            .ConfigureAwait(false);

        if (!discoverResult.IsSuccess || discoverResult.Value == null)
        {
            IGenericMessage msg;
            var upstreamError = discoverResult.CurrentMessage;
            if (upstreamError is not null)
                msg = SchemaInformationLog.DiscoveryFailed(_logger, connectionName, upstreamError);
            else
                msg = SchemaInformationLog.DiscoveryFailedNoDetails(_logger, connectionName);
            return GenericResult<SchemaInformation>.Failure(msg);
        }

        var persistResult = await PersistConfiguration(
            config.Name, connectionType, config.Id, discoverResult.Value, cancellationToken).ConfigureAwait(false);

        if (!persistResult.IsSuccess || persistResult.Value is null)
            return persistResult.ToNewResult<SchemaInformation>();

        var reloadedConfigResult = await _dataStoreProvider.Get(persistResult.Value, cancellationToken).ConfigureAwait(false);
        var reloadedConfig = reloadedConfigResult.IsSuccess ? reloadedConfigResult.Value : null;
        if (reloadedConfig == null)
        {
            return GenericResult<SchemaInformation>.Failure(
                SchemaInformationLog.DataStoreNotFoundAfterDiscovery(_logger, connectionName));
        }

        var info = new SchemaInformation(reloadedConfig);
        SchemaInformationLog.DiscoverySucceeded(_logger, connectionName, reloadedConfig.Name);
        return GenericResult<SchemaInformation>.Success(info);
    }

    private static DataStoreDiscoveryOptions BuildDiscoveryOptions(IConnectionImplementationConfiguration config)
        => DataStoreDiscoveryOptions.Default;

    /// <summary>
    /// Merges the discovered schema (DataStore → DataPath → DataContainer → DataContainerField) into the
    /// connection's store -- a new one when the connection has none -- and saves the store once.
    /// </summary>
    /// <returns>The name the store is saved under.</returns>
    private async Task<IGenericResult<string>> PersistConfiguration(
        string dataStoreName,
        string connectionType,
        Guid connectionId,
        IReadOnlyList<IStorageContainer> containers,
        CancellationToken ct)
    {
        var pathGroups = containers.GroupBy(c => c.Path.PathValue, StringComparer.Ordinal).ToList();
        SchemaDiscoveryLog.PersistStarted(_logger, dataStoreName, containers.Count, pathGroups.Count);
        SchemaDiscoveryLog.PersistingDataStore(_logger, dataStoreName, connectionId);

        var storeResult = await ResolveOrCreateDataStore(
            dataStoreName, connectionType, connectionId, ct).ConfigureAwait(false);
        if (!storeResult.IsSuccess || storeResult.Value is null)
            return storeResult.ToNewResult<string>();

        var store = storeResult.Value;
        var pathsWritten = 0;
        foreach (var pathGroup in pathGroups)
        {
            SchemaDiscoveryLog.PersistingDataPath(_logger, pathGroup.Key, store.Name);
            var path = store.Paths.FirstOrDefault(
                p => string.Equals(p.PathValue, pathGroup.Key, StringComparison.OrdinalIgnoreCase));
            if (path is null)
            {
                path = new DataPathConfiguration { Name = pathGroup.Key, PathValue = pathGroup.Key };
                store.Paths.Add(path);
                pathsWritten++;
            }

            foreach (var container in pathGroup)
                MergeContainer(path, pathGroup.Key, container);
        }

        SchemaDiscoveryLog.UpdatingLastDiscoveredAt(_logger, store.Name);
        var now = DateTimeOffset.UtcNow;
        store.LastDiscoveredAt = now;

        var saved = await _dataStoreProvider.Save(
            store, store.Domain, store.Implementation, store.Name, ct).ConfigureAwait(false);
        if (!saved.IsSuccess)
        {
            var upstreamError = saved.CurrentMessage;
            if (upstreamError is not null)
                SchemaDiscoveryLog.PersistFailed(_logger, store.Name, upstreamError);
            else
                SchemaDiscoveryLog.DataStoreSaveFailed(_logger, store.Name);
            return saved.ToNewResult<string>();
        }

        SchemaDiscoveryLog.LastDiscoveredAtUpdated(_logger, store.Name, now);
        SchemaDiscoveryLog.PersistCompleted(
            _logger, store.Name, pathsWritten, containers.Count, containers.Sum(c => c.Schema.Fields.Count));
        return GenericResult<string>.Success(store.Name);
    }

    private async Task<IGenericResult<IDataStoreImplementationConfiguration>> ResolveOrCreateDataStore(
        string dataStoreName,
        string connectionType,
        Guid connectionId,
        CancellationToken ct)
    {
        var allDataStores = await _dataStoreProvider.Get(ct).ConfigureAwait(false);
        if (!allDataStores.IsSuccess)
            return allDataStores.ToNewResult<IDataStoreImplementationConfiguration>();

        var existingDataStore = allDataStores.Value!.FirstOrDefault(ds => ds.ConnectionId == connectionId);
        if (existingDataStore != null)
        {
            SchemaDiscoveryLog.ExistingDataStoreFound(_logger, existingDataStore.Name, existingDataStore.Id);
            return GenericResult<IDataStoreImplementationConfiguration>.Success(existingDataStore);
        }

        // Why these: the first save writes the domain row from them. The store is named for its
        // connection and is the kind the connection is.
        return GenericResult<IDataStoreImplementationConfiguration>.Success(new DataStoreImplementationConfiguration
        {
            Name = dataStoreName,
            Domain = "DataStore",
            Implementation = connectionType,
            ConnectionId = connectionId,
        });
    }

    private void MergeContainer(DataPathConfiguration path, string pathName, IStorageContainer discovered)
    {
        SchemaDiscoveryLog.PersistingContainer(_logger, discovered.Name, pathName);
        var container = path.Containers.FirstOrDefault(
            c => string.Equals(c.Name, discovered.Name, StringComparison.OrdinalIgnoreCase));
        if (container is null)
        {
            container = new DataContainerConfiguration { Id = Guid.CreateVersion7(), Name = discovered.Name };
            path.Containers.Add(container);
        }

        container.TypeId = discovered.ContainerType.Name;

        SchemaDiscoveryLog.PersistingFields(_logger, discovered.Schema.Fields.Count, discovered.Name);
        foreach (var field in discovered.Schema.Fields)
        {
            var existingField = container.Fields.FirstOrDefault(
                f => string.Equals(f.Name, field.Name, StringComparison.OrdinalIgnoreCase));
            if (existingField is not null)
            {
                existingField.DataType = field.FieldType.TypeName;
                continue;
            }

            container.Fields.Add(new DataContainerFieldConfiguration
            {
                Id = Guid.CreateVersion7(),
                Name = field.Name,
                DataType = field.FieldType.TypeName,
                VisibilityId = field.Visibility.Name,
            });
        }
    }
}
