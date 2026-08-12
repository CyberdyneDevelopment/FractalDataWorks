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
using Microsoft.Extensions.Options;

namespace Fdw.Services.Data;

/// <summary>
/// Provides on-demand schema discovery for named connections.
/// Caches results via <see cref="DataStoreConfigurationProvider"/>; persists newly discovered
/// metadata as DataStore/DataPath/DataContainer/DataContainerField rows.
/// </summary>
/// <remarks>
/// GetSchema is the cache-first path: if a DataStore already exists for this connection,
/// its persisted metadata is returned immediately. Otherwise discovery runs and results
/// are persisted before returning.
/// RefreshSchema always re-discovers, enabling the UI "Re-discover" action.
/// Discovery scope (included schemas, excluded schemas) is read directly from the
/// the DataStore/DataPath/DataContainer hierarchy (db/schema/table) and RBAC.
/// </remarks>
public sealed class SchemaInformationService : ISchemaInformationService
{
    private readonly IConnectionProvider _connectionProvider;
    // Why: ConnectionConfigurationProvider (dual-source) replaces IConnectionProvider.GetAllConnectionConfigurations()
    // which was removed. Used for resolving connection configs by name.
    private readonly ConnectionConfigurationProvider _configProvider;
    // Why: DataStoreConfigurationProvider (dual-source) merges system (ctrl) and user (cfg) DataStore configs.
    private readonly DataStoreConfigurationProvider _dataStoreProvider;
    private readonly DefaultConfigurationProvider<DataPathConfiguration, DataPathConfigurationCommand> _dataPathProvider;
    private readonly DefaultConfigurationProvider<DataContainerConfiguration, DataContainerConfigurationCommand> _containerProvider;
    private readonly DefaultConfigurationProvider<DataContainerFieldConfiguration, DataContainerFieldConfigurationCommand> _fieldProvider;
    // Why: IOptionsMonitor holds the in-memory config cache so upsert checks do not
    // require extra database round-trips on every field write.
    private readonly IOptionsMonitor<List<DataPathConfiguration>> _dataPathOptions;
    private readonly IOptionsMonitor<List<DataContainerConfiguration>> _containerOptions;
    private readonly IOptionsMonitor<List<DataContainerFieldConfiguration>> _fieldOptions;
    private readonly ILogger<SchemaInformationService> _logger;

    /// <summary>
    /// Initializes a new instance of <see cref="SchemaInformationService"/>.
    /// </summary>
    public SchemaInformationService(
        IConnectionProvider connectionProvider,
        ConnectionConfigurationProvider configProvider,
        DataStoreConfigurationProvider dataStoreProvider,
        DefaultConfigurationProvider<DataPathConfiguration, DataPathConfigurationCommand> dataPathProvider,
        DefaultConfigurationProvider<DataContainerConfiguration, DataContainerConfigurationCommand> containerProvider,
        DefaultConfigurationProvider<DataContainerFieldConfiguration, DataContainerFieldConfigurationCommand> fieldProvider,
        IOptionsMonitor<List<DataPathConfiguration>> dataPathOptions,
        IOptionsMonitor<List<DataContainerConfiguration>> containerOptions,
        IOptionsMonitor<List<DataContainerFieldConfiguration>> fieldOptions,
        ILogger<SchemaInformationService>? logger = null)
    {
        _connectionProvider = connectionProvider;
        _configProvider = configProvider;
        _dataStoreProvider = dataStoreProvider;
        _dataPathProvider = dataPathProvider;
        _containerProvider = containerProvider;
        _fieldProvider = fieldProvider;
        _dataPathOptions = dataPathOptions;
        _containerOptions = containerOptions;
        _fieldOptions = fieldOptions;
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
            // Why (FDW-583): a single emission — DiscoveryDisabled was previously logged bare here AND
            // again inside the Failure(...) call, printing the same record twice.
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
            // Why (FDW-583): a single emission — DiscoveryDisabled was previously logged bare here AND
            // again inside the Failure(...) call, printing the same record twice.
            return GenericResult<SchemaInformation>.Failure(
                SchemaInformationLog.DiscoveryDisabled(_logger, connectionName));
        }

        // Why: RefreshSchema always re-discovers — cache check is intentionally skipped.
        return await DiscoverAndReturn(config, cancellationToken).ConfigureAwait(false);
    }

    private async Task<IGenericResult<ConnectionConfiguration>> ResolveConnectionConfig(
        string connectionName,
        CancellationToken cancellationToken)
    {
        var result = await _configProvider.Get(connectionName, cancellationToken).ConfigureAwait(false);
        if (!result.IsSuccess || result.Value is null)
        {
            return GenericResult<ConnectionConfiguration>.Failure(
                SchemaInformationLog.ConnectionConfigNotFound(_logger, connectionName));
        }

        return GenericResult<ConnectionConfiguration>.Success(result.Value);
    }

    private async Task<SchemaInformation?> TryGetFromCache(
        ConnectionConfiguration config,
        CancellationToken cancellationToken)
    {
        var allConfigsResult = await _dataStoreProvider.Get(cancellationToken).ConfigureAwait(false);
        var allConfigs = allConfigsResult.IsSuccess ? allConfigsResult.Value! : (IReadOnlyList<DataStoreConfiguration>)[];
        var dataStore = allConfigs.FirstOrDefault(ds => ds.ConnectionId == config.Id);
        return dataStore != null ? new SchemaInformation(dataStore) : null;
    }

    private async Task<IGenericResult<SchemaInformation>> DiscoverAndReturn(
        ConnectionConfiguration config,
        CancellationToken cancellationToken)
    {
        var connectionName = config.Name;
        var connectionType = config.ServiceOptionType;

        if (string.IsNullOrEmpty(connectionType))
        {
            return GenericResult<SchemaInformation>.Failure(
                SchemaInformationLog.ConnectionTypeMissing(_logger, connectionName));
        }

        // Why: ISchemaDiscovery is the marker interface on connection types that support discovery.
        // Checking it here prevents attempting discovery on REST, HTTP, or other non-SQL types.
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

        var containers = discoverResult.Value;

        var persistResult = await PersistConfiguration(
            config.Name, connectionType, config.Id, containers, cancellationToken).ConfigureAwait(false);

        if (!persistResult.IsSuccess)
            return persistResult.ToNewResult<SchemaInformation>();

        // Why: DataGateway manages its own cache — no manual eviction needed.
        // Reload from provider now that persistence is complete.
        var reloadedConfigResult = await _dataStoreProvider.Get(config.Name, cancellationToken).ConfigureAwait(false);
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

    // Why: Schema scope is expressed by DataStore/DataPath/DataContainer (db/schema/table),
    // and access is gated by RBAC. Connection itself carries no include/exclude schema lists.
    private static DataStoreDiscoveryOptions BuildDiscoveryOptions(ConnectionConfiguration config)
        => DataStoreDiscoveryOptions.Default;

    /// <summary>
    /// Persists the discovered schema hierarchy (DataStore → DataPath → DataContainer → DataContainerField).
    /// Uses upsert logic: existing rows are updated, new rows are inserted.
    /// </summary>
    private async Task<IGenericResult> PersistConfiguration(
        string dataStoreName,
        string connectionType,
        Guid connectionId,
        IReadOnlyList<IStorageContainer> containers,
        CancellationToken ct)
    {
        SchemaDiscoveryLog.ResolvingConfigurationWriters(_logger);
        var writersResult = ResolveWriters();
        if (!writersResult.IsSuccess || writersResult.Value == null)
            return writersResult;

        var writers = writersResult.Value;
        var pathGroups = containers.GroupBy(c => c.Path.PathValue, StringComparer.Ordinal).ToList();

        SchemaDiscoveryLog.PersistStarted(_logger, dataStoreName, containers.Count, pathGroups.Count);

        SchemaDiscoveryLog.PersistingDataStore(_logger, dataStoreName, connectionId);
        var dataStoreResult = await ResolveOrCreateDataStore(
            dataStoreName, connectionType, connectionId, writers.DataStore, ct).ConfigureAwait(false);
        if (!dataStoreResult.IsSuccess || dataStoreResult.Value == null)
            return dataStoreResult;

        var dataStoreConfig = dataStoreResult.Value;
        var savedDataStoreId = dataStoreConfig.Id;

        var persistResult = await PersistPathGroups(
            dataStoreName, savedDataStoreId, pathGroups, writers, ct).ConfigureAwait(false);
        if (!persistResult.IsSuccess)
            return persistResult;

        SchemaDiscoveryLog.UpdatingLastDiscoveredAt(_logger, dataStoreName);
        await UpdateLastDiscoveredAt(dataStoreConfig, dataStoreName, writers.DataStore, ct).ConfigureAwait(false);
        return GenericResult.Success();
    }

    private async Task<IGenericResult<DataStoreConfiguration>> ResolveOrCreateDataStore(
        string dataStoreName,
        string connectionType,
        Guid connectionId,
        DefaultConfigurationProvider<DataStoreConfiguration, DataStoreConfigurationCommand> writer,
        CancellationToken ct)
    {
        var allDataStoresResult = await _dataStoreProvider.Get(ct).ConfigureAwait(false);
        var allDataStores = allDataStoresResult.IsSuccess ? allDataStoresResult.Value! : (IReadOnlyList<DataStoreConfiguration>)[];
        var existingDataStore = allDataStores.FirstOrDefault(ds => ds.ConnectionId == connectionId);

        if (existingDataStore != null)
        {
            SchemaDiscoveryLog.ExistingDataStoreFound(_logger, dataStoreName, existingDataStore.Id);
            return GenericResult<DataStoreConfiguration>.Success(existingDataStore);
        }

        var dataStoreConfig = new DataStoreConfiguration
        {
            Name = dataStoreName,
            ConnectionId = connectionId,
            ServiceOptionType = connectionType
        };
        var savedResult = await writer.Save(dataStoreConfig, ct).ConfigureAwait(false);
        if (!savedResult.IsSuccess || savedResult.Value == null)
        {
            var upstreamError = savedResult.CurrentMessage;
            if (upstreamError is not null)
                SchemaDiscoveryLog.PersistFailed(_logger, dataStoreName, upstreamError);
            else
                SchemaDiscoveryLog.DataStoreSaveFailed(_logger, dataStoreName);
            return savedResult.ToNewResult<DataStoreConfiguration>();
        }

        return GenericResult<DataStoreConfiguration>.Success(savedResult.Value);
    }

    private async Task<IGenericResult> PersistPathGroups(
        string dataStoreName,
        Guid savedDataStoreId,
        List<System.Linq.IGrouping<string, IStorageContainer>> pathGroups,
        ConfigurationWriters writers,
        CancellationToken ct)
    {
        var existingPaths = _dataPathOptions.CurrentValue
            .Where(p => p.DataStoreId == savedDataStoreId)
            .ToDictionary(p => p.PathName, StringComparer.OrdinalIgnoreCase);

        var pathsWritten = 0;
        var containersWritten = 0;
        var fieldsWritten = 0;

        foreach (var pathGroup in pathGroups)
        {
            SchemaDiscoveryLog.PersistingDataPath(_logger, pathGroup.Key, dataStoreName);
            var pathIdResult = await ResolveOrCreatePath(
                dataStoreName, savedDataStoreId, pathGroup.Key, existingPaths, writers.Path, ct).ConfigureAwait(false);
            if (!pathIdResult.IsSuccess)
                return pathIdResult;

            var savedPathId = pathIdResult.Value;
            if (!existingPaths.ContainsKey(pathGroup.Key))
                pathsWritten++;

            var persistResult = await PersistContainersForPath(
                pathGroup, savedPathId, writers.Container, writers.Field, ct).ConfigureAwait(false);
            if (!persistResult.IsSuccess)
            {
                var upstreamError = persistResult.CurrentMessage;
                if (upstreamError is not null)
                    SchemaDiscoveryLog.PersistFailed(_logger, dataStoreName, upstreamError);
                else
                    SchemaDiscoveryLog.ContainerPersistFailed(_logger, dataStoreName);
                return persistResult;
            }

            containersWritten += pathGroup.Count();
            fieldsWritten += pathGroup.Sum(c => c.Schema.Fields.Count);
        }

        SchemaDiscoveryLog.PersistCompleted(_logger, dataStoreName, pathsWritten, containersWritten, fieldsWritten);
        return GenericResult.Success();
    }

    private async Task<IGenericResult<Guid>> ResolveOrCreatePath(
        string dataStoreName,
        Guid dataStoreId,
        string pathKey,
        Dictionary<string, DataPathConfiguration> existingPaths,
        DefaultConfigurationProvider<DataPathConfiguration, DataPathConfigurationCommand> writer,
        CancellationToken ct)
    {
        if (existingPaths.TryGetValue(pathKey, out var existingPath))
            return GenericResult<Guid>.Success(existingPath.Id);

        var pathConfig = new DataPathConfiguration
        {
            Name = pathKey,
            DataStoreId = dataStoreId,
            PathName = pathKey
        };
        var savedPathResult = await writer.Save(pathConfig, ct).ConfigureAwait(false);
        if (!savedPathResult.IsSuccess || savedPathResult.Value == null)
        {
            var upstreamError = savedPathResult.CurrentMessage;
            if (upstreamError is not null)
                SchemaDiscoveryLog.PersistFailed(_logger, dataStoreName, upstreamError);
            else
                SchemaDiscoveryLog.DataPathSaveFailed(_logger, pathKey, dataStoreName);
            return savedPathResult.ToNewResult<Guid>();
        }

        // Why: no tree invalidation needed — the eager full-tree singleton is deleted; container
        // lookups go through ConfigurationGatewayDataStoreProvider.GetContainer over DataGatewayService
        // (caching built in, tag-invalidated by the config-write path), so the new DataPath surfaces on
        // next read.
        return GenericResult<Guid>.Success(savedPathResult.Value.Id);
    }

    private async Task<IGenericResult> PersistContainersForPath(
        System.Linq.IGrouping<string, IStorageContainer> pathGroup,
        Guid savedPathId,
        DefaultConfigurationProvider<DataContainerConfiguration, DataContainerConfigurationCommand> containerWriter,
        DefaultConfigurationProvider<DataContainerFieldConfiguration, DataContainerFieldConfigurationCommand> fieldWriter,
        CancellationToken ct)
    {
        var existingContainers = _containerOptions.CurrentValue
            .Where(c => c.DataPathId == savedPathId)
            .ToDictionary(c => c.Name, StringComparer.OrdinalIgnoreCase);

        foreach (var container in pathGroup)
        {
            Guid savedContainerId;
            IReadOnlyList<DataContainerFieldConfiguration> currentFields;
            SchemaDiscoveryLog.PersistingContainer(_logger, container.Name, pathGroup.Key);

            if (existingContainers.TryGetValue(container.Name, out var existingContainerRef))
            {
                // Why: re-read the COMPOSED container (Fields/Keys populated from the DB cascade)
                // before mutating — _containerOptions is a bare IOptionsMonitor snapshot with empty
                // Fields/Keys, and saving that directly would re-point an EMPTY child set at the new
                // RowId version, stranding the container's real fields.
                var composedResult = await containerWriter.Get(existingContainerRef.Id, ct).ConfigureAwait(false);
                if (!composedResult.IsSuccess || composedResult.Value == null)
                    return composedResult;

                var existingContainer = composedResult.Value;

                // Why: ContainerType renamed to TypeId after Wave A5 DDL restructure.
                if (!string.Equals(existingContainer.TypeId, container.ContainerType.Name, StringComparison.Ordinal))
                {
                    existingContainer.TypeId = container.ContainerType.Name;
                    var containerUpdateResult = await containerWriter.Save(existingContainer, ct).ConfigureAwait(false);
                    if (!containerUpdateResult.IsSuccess)
                        return containerUpdateResult;
                }
                savedContainerId = existingContainer.Id;
                currentFields = existingContainer.Fields;
            }
            else
            {
                var containerConfig = new DataContainerConfiguration
                {
                    Name = container.Name,
                    DataPathId = savedPathId,
                    // Why: TypeId replaces ContainerType after Wave A5 DDL rename.
                    TypeId = container.ContainerType.Name
                };
                var savedContainerResult = await containerWriter.Save(containerConfig, ct).ConfigureAwait(false);
                if (!savedContainerResult.IsSuccess || savedContainerResult.Value == null)
                    return savedContainerResult;

                savedContainerId = savedContainerResult.Value.Id;
                currentFields = [];
                // Why: no tree invalidation needed — the eager full-tree singleton is deleted;
                // ConfigurationGatewayDataStoreProvider.GetContainer reads through DataGatewayService
                // (caching built in, tag-invalidated on write), so the new container is found on the
                // next lookup.
            }

            SchemaDiscoveryLog.PersistingFields(_logger, container.Schema.Fields.Count, container.Name);

            // Why: derived from the just re-read composed container (or empty for a brand-new one),
            // NOT from _fieldOptions — the same stale-snapshot risk applies to the field diff source.
            var existingFields = currentFields
                .ToDictionary(f => f.Name, StringComparer.OrdinalIgnoreCase);

            var ordinal = 0;
            foreach (var field in container.Schema.Fields)
            {
                if (existingFields.TryGetValue(field.Name, out var existingField))
                {
                    var dataType = field.FieldType.TypeName;
                    // Why: IsNullable and Ordinal moved to data.MsSqlDataContainerField typed body (Wave A5).
                    // Structural change detection on the base record now uses DataType only;
                    // IsNullable/Ordinal sync will be handled by the typed-body writer in Wave B2.
                    if (!string.Equals(existingField.DataType, dataType, StringComparison.Ordinal))
                    {
                        existingField.DataType = dataType;
                        var fieldUpdateResult = await fieldWriter.Save(existingField, ct).ConfigureAwait(false);
                        if (!fieldUpdateResult.IsSuccess)
                            return fieldUpdateResult;
                    }
                }
                else
                {
                    var fieldConfig = new DataContainerFieldConfiguration
                    {
                        Name = field.Name,
                        DataContainerId = savedContainerId,
                        // Why: IsNullable/Ordinal now live on data.MsSqlDataContainerField (typed body).
                        DataType = field.FieldType.TypeName
                    };
                    var savedFieldResult = await fieldWriter.Save(fieldConfig, ct).ConfigureAwait(false);
                    if (!savedFieldResult.IsSuccess)
                        return savedFieldResult;
                }

                ordinal++;
            }
        }

        return GenericResult.Success();
    }

    private async Task UpdateLastDiscoveredAt(
        DataStoreConfiguration dataStoreConfig,
        string dataStoreName,
        DefaultConfigurationProvider<DataStoreConfiguration, DataStoreConfigurationCommand> writer,
        CancellationToken ct)
    {
        var now = DateTimeOffset.UtcNow;
        dataStoreConfig.LastDiscoveredAt = now;
        var updateResult = await writer.Save(dataStoreConfig, ct).ConfigureAwait(false);
        if (updateResult.IsSuccess)
        {
            SchemaDiscoveryLog.LastDiscoveredAtUpdated(_logger, dataStoreName, now);
        }
        else
        {
            var upstreamError = updateResult.CurrentMessage;
            if (upstreamError is not null)
                SchemaDiscoveryLog.PersistFailed(_logger, dataStoreName, upstreamError);
            else
                SchemaDiscoveryLog.LastDiscoveredAtUpdateFailed(_logger, dataStoreName);
        }
    }

    private IGenericResult<ConfigurationWriters> ResolveWriters()
    {
        return GenericResult<ConfigurationWriters>.Success(new ConfigurationWriters(
            _dataStoreProvider,
            _dataPathProvider,
            _containerProvider,
            _fieldProvider));
    }

    // Why: pure data holder, no logic beyond trivial construction/assignment
    [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
    private sealed class ConfigurationWriters
    {
        public DefaultConfigurationProvider<DataStoreConfiguration, DataStoreConfigurationCommand> DataStore { get; }
        public DefaultConfigurationProvider<DataPathConfiguration, DataPathConfigurationCommand> Path { get; }
        public DefaultConfigurationProvider<DataContainerConfiguration, DataContainerConfigurationCommand> Container { get; }
        public DefaultConfigurationProvider<DataContainerFieldConfiguration, DataContainerFieldConfigurationCommand> Field { get; }

        public ConfigurationWriters(
            DefaultConfigurationProvider<DataStoreConfiguration, DataStoreConfigurationCommand> dataStore,
            DefaultConfigurationProvider<DataPathConfiguration, DataPathConfigurationCommand> path,
            DefaultConfigurationProvider<DataContainerConfiguration, DataContainerConfigurationCommand> container,
            DefaultConfigurationProvider<DataContainerFieldConfiguration, DataContainerFieldConfigurationCommand> field)
        {
            DataStore = dataStore;
            Path = path;
            Container = container;
            Field = field;
        }
    }
}
