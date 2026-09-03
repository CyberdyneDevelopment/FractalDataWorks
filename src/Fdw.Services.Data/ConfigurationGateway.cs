using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Commands.Data;
using Fdw.Commands.Data.Abstractions;
using Fdw.Commands.Data.Abstractions.Caching;
using Fdw.Configuration;
using Fdw.Data;
using Fdw.Data.Abstractions;
using Fdw.Data.Abstractions.Mappers.PocoMappers;
using Fdw.Data.RowSources.Abstractions;
using Fdw.Results;
using Fdw.Services.Authentication.Abstractions.Security;
using Fdw.Services.Connections;
using Fdw.Services.Connections.Abstractions;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Data.Caching;
using Fdw.Services.Data.Configuration;
using Fdw.Services.Data.DataNodes;
using Fdw.Services.Data.Logging;
using Fdw.Services.SecretManagers.Abstractions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

using Fdw.Abstractions;
namespace Fdw.Services.Data;

/// <summary>
/// IConfigurationGateway implementation. Resolves its own IDataConnection from
/// <see cref="IConnectionFactory"/> and the ConfigurationDb entry in
/// the deserialized <see cref="Configuration.ConfigurationSchema"/>. Configuration reads execute directly
/// on this connection — never through DataGatewayService — so there is no risk of
/// recursion when ConfigurationGatewayDataStoreProvider resolves containers at startup.
/// </summary>
/// <remarks>
/// Cascade orchestration: when a command has Cascade=true, Execute resolves the parent container
/// via the IDataNode tree, fires follow-up queries for each child container whose FK key points to
/// the parent, and populates the matching collection property on each parent POCO via reflection.
/// </remarks>
public sealed class ConfigurationGateway : IConfigurationGateway
{
    /// <inheritdoc />
    public string ConnectionName { get; }
    private const string ConnectionTypeMsSql = "MsSql";
    private const string ConnectionTypePostgreSql = "PostgreSql";

    private readonly ILogger<ConfigurationGateway> _logger;
    private readonly IConnectionFactory _connectionFactory;
    private readonly ISecretManager? _secretManager;
    private readonly ConfigurationSchema _schema;

    private readonly Lazy<Task<IGenericResult<IDataConnection>>> _connectionLazy;

    private readonly Lazy<IReadOnlyList<IDataStore>> _dataStores;

    private readonly DataGatewayResultCache? _cache;
    private readonly MainDataGatewayConfiguration? _options;

    private readonly IAuthenticationContextAccessor? _authenticationContextAccessor;

    private readonly Lazy<IGenericResult<IConnectionType>> _connectionTypeLazy;

    private static readonly TimeSpan DefaultCacheDuration = TimeSpan.FromMinutes(5);

    /// <summary>
    /// Initializes a new instance of <see cref="ConfigurationGateway"/> without a secret manager.
    /// Use when the ConfigurationDb connection uses integrated auth or does not need secret resolution.
    /// </summary>
    /// <param name="connectionName">The configuration connection this gateway reads and writes.</param>
    /// <param name="connectionFactory">Factory used to open a connection to ConfigurationDb.</param>
    /// <param name="schema">
    /// Deserialized <see cref="ConfigurationSchema"/> from <c>configurationSchema.json</c>.
    /// Registered as a singleton via
    /// <see cref="ConfigurationGatewayTypes"/>, one per connection declared in the schema.
    /// </param>
    /// <param name="logger">Logger (optional — falls back to NullLogger).</param>
    /// <param name="cache">Optional process-wide result cache. When null caching is disabled.</param>
    /// <param name="options">Optional gateway options (EnableCache knob). When null caching is disabled.</param>
    /// <param name="authenticationContextAccessor">
    /// Optional accessor for the calling principal, used to partition cached results by the
    /// visibility scope their session reads under.
    /// </param>
    public ConfigurationGateway(
        string connectionName,
        IConnectionFactory connectionFactory,
        ConfigurationSchema schema,
        ILogger<ConfigurationGateway>? logger = null,
        DataGatewayResultCache? cache = null,
        MainDataGatewayConfiguration? options = null,
        IAuthenticationContextAccessor? authenticationContextAccessor = null)
        : this(connectionName, connectionFactory, secretManager: null, schema, logger, cache, options, authenticationContextAccessor)
    {
    }

    /// <summary>
    /// Initializes a new instance of <see cref="ConfigurationGateway"/> with an optional secret manager.
    /// </summary>
    /// <param name="connectionName">The configuration connection this gateway reads and writes.</param>
    /// <param name="connectionFactory">Factory used to open a connection to ConfigurationDb.</param>
    /// <param name="secretManager">
    /// Optional secret manager. When non-null and the ConfigurationDb connection references a secret,
    /// the secret is resolved at construction time and attached to the connection configuration
    /// before the factory creates the connection.
    /// </param>
    /// <param name="schema">
    /// Deserialized <see cref="ConfigurationSchema"/> from <c>configurationSchema.json</c>.
    /// </param>
    /// <param name="logger">Logger (optional — falls back to NullLogger).</param>
    /// <param name="cache">Optional process-wide result cache. When null caching is disabled.</param>
    /// <param name="options">Optional gateway options (EnableCache knob). When null caching is disabled.</param>
    /// <param name="authenticationContextAccessor">
    /// Optional accessor for the calling principal, used to partition cached results by the
    /// visibility scope their session reads under. Optional for the same reason the connection
    /// layer's own accessor is: a null accessor yields a null context, which every session-context
    /// scheme must govern, so the partition still names exactly what the session will apply.
    /// </param>
    public ConfigurationGateway(
        string connectionName,
        IConnectionFactory connectionFactory,
        ISecretManager? secretManager,
        ConfigurationSchema schema,
        ILogger<ConfigurationGateway>? logger = null,
        DataGatewayResultCache? cache = null,
        MainDataGatewayConfiguration? options = null,
        IAuthenticationContextAccessor? authenticationContextAccessor = null)
    {
        ConnectionName = string.IsNullOrWhiteSpace(connectionName)
            ? throw new ArgumentNullException(nameof(connectionName))
            : connectionName;
        _connectionFactory = connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));
        _secretManager = secretManager;
        _schema = schema ?? throw new ArgumentNullException(nameof(schema));
        _logger = logger ?? NullLogger<ConfigurationGateway>.Instance;
        _cache = cache;
        _options = options;
        _authenticationContextAccessor = authenticationContextAccessor;

        _connectionTypeLazy = new Lazy<IGenericResult<IConnectionType>>(
            ResolveConnectionType,
            LazyThreadSafetyMode.ExecutionAndPublication);

#pragma warning disable VSTHRD011
        _connectionLazy = new Lazy<Task<IGenericResult<IDataConnection>>>(
            () => BuildConnection(default),
            LazyThreadSafetyMode.ExecutionAndPublication);
#pragma warning restore VSTHRD011

        _dataStores = new Lazy<IReadOnlyList<IDataStore>>(
            () => BuildFromSchema(_schema, _logger),
            LazyThreadSafetyMode.ExecutionAndPublication);

        ConfigurationGatewayLog.Initialised(_logger, ConnectionName);
    }

    /// <summary>
    /// Gets the IDataStore tree built from the ConfigurationSchema.
    /// Consumed by <see cref="ConfigurationContainerLookup"/> without a separate registration.
    /// </summary>
    public IReadOnlyList<IDataStore> DataStores => _dataStores.Value;

    /// <inheritdoc/>
    public Task<IGenericResult<T>> Execute<T>(IDataCommand command, DataStoreTarget target, CancellationToken cancellationToken = default)
        => Execute<T>(command, target, useCache: true, cancellationToken);

    /// <inheritdoc/>
    public async Task<IGenericResult<T>> Execute<T>(IDataCommand command, DataStoreTarget target, bool useCache, CancellationToken cancellationToken = default)
    {
        bool isQuery = command is IQueryCommand;

        string? cacheKey = null;
        var cacheCeiling = TimeSpan.MaxValue;
        if (CacheEnabled && isQuery)
        {
            var connectionTypeResult = _connectionTypeLazy.Value;
            if (!connectionTypeResult.IsSuccess || connectionTypeResult.Value is null)
                return connectionTypeResult.ToNewResult<T>();

            cacheCeiling = connectionTypeResult.Value.MaxCacheDuration(_authenticationContextAccessor?.Current);

            try
            {
                cacheKey = string.Concat(
                    connectionTypeResult.Value.CachePartition(_authenticationContextAccessor?.Current),
                    "|_cfg|", CacheKeyBuilder.ComputeCacheKey(command, target), ":", typeof(T).FullName);
            }
            catch (Exception ex)
            {
                return GenericResult<T>.Failure(
                    DataGatewayCacheLog.KeyComputationFailed(_logger, command.CommandType, target.Container, ex.Message));
            }

            if (useCache && _cache!.TryGet<T>(cacheKey, out var cached) && cached is not null)
                return cached;
        }

        var result = await ExecuteCore<T>(command, target, cancellationToken).ConfigureAwait(false);

        ApplyCacheOutcome(command, target, result, isQuery, cacheKey, cacheCeiling);
        return result;
    }



    /// <inheritdoc/>
    public void InvalidateCachedResults(DataStoreTarget target)
    {
        ArgumentNullException.ThrowIfNull(target);

        if (!CacheEnabled)
            return;

        _cache!.InvalidateByTag(CacheKeyBuilder.TagFor(target));
    }

    private bool CacheEnabled => _cache is not null && _options is not null && _options.EnableCache;

    private void ApplyCacheOutcome<T>(
        IDataCommand command,
        DataStoreTarget target,
        IGenericResult<T> result,
        bool isQuery,
        string? cacheKey,
        TimeSpan cacheCeiling)
    {
        if (!CacheEnabled || !result.IsSuccess)
            return;

        if (isQuery)
        {
            if (cacheKey is not null)
            {
                _cache!.Set(
                    cacheKey,
                    result,
                    CacheKeyBuilder.GetInvalidationTags(command, target),
                    CachePolicy.GetDuration(command, DefaultCacheDuration, cacheCeiling));
            }

            return;
        }

        _cache!.InvalidateByTags(CacheKeyBuilder.GetInvalidationTags(command, target));
    }

    private async Task<IGenericResult<T>> ExecuteCore<T>(IDataCommand command, DataStoreTarget target, CancellationToken cancellationToken)
    {
        ConfigurationGatewayLog.ExecuteEntry(_logger, target.DataStore, target.Path, target.Container);

        if (string.IsNullOrWhiteSpace(target.DataStore))
        {
            return GenericResult<T>.Failure(
                ConfigurationGatewayLog.ExecuteFailed(_logger, target.Container,
                    "DataStoreTarget.DataStore is required"));
        }

#pragma warning disable VSTHRD011, VSTHRD003
        var connectionResult = await _connectionLazy.Value.ConfigureAwait(false);
#pragma warning restore VSTHRD011, VSTHRD003
        if (!connectionResult.IsSuccess || connectionResult.Value is null)
            return connectionResult.ToNewResult<T>();

        var connection = connectionResult.Value;

        var containerResult = ResolveContainerResult(target);

        if (!containerResult.IsSuccess)
        {
            ConfigurationGatewayLog.ExecuteFailed(_logger, target.Container,
                containerResult.CurrentMessage ?? "Container could not be resolved from the configuration schema");

            return containerResult.ToNewResult<T>();
        }

        if (containerResult.Value is null)
        {
            return GenericResult<T>.Failure(
                ConfigurationGatewayLog.ExecuteFailed(_logger, target.Container,
                    "Container could not be resolved from the configuration schema"));
        }

        var container = containerResult.Value;

        try
        {
            var mainResult = await connection.Execute<T>(command, container, cancellationToken).ConfigureAwait(false);
            ConfigurationGatewayLog.ExecuteExit(_logger, target.Container, mainResult.IsSuccess);
            return mainResult;
        }
        catch (Exception ex)
        {
            return GenericResult<T>.Failure(
                ConfigurationGatewayLog.ExecuteException(_logger, ex, target.Container));
        }
    }

    /// <inheritdoc/>
    public async Task<IGenericResult<IRecordSource<DataRecord>>> OpenRecordSource(
        IDataCommand command, DataStoreTarget target, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(target.DataStore))
        {
            return GenericResult<IRecordSource<DataRecord>>.Failure(
                ConfigurationGatewayLog.ExecuteFailed(_logger, target.Container, "DataStoreTarget.DataStore is required"));
        }

#pragma warning disable VSTHRD011, VSTHRD003
        var connectionResult = await _connectionLazy.Value.ConfigureAwait(false);
#pragma warning restore VSTHRD011, VSTHRD003
        if (!connectionResult.IsSuccess || connectionResult.Value is null)
            return connectionResult.ToNewResult<IRecordSource<DataRecord>>();

        var containerResult = ResolveContainerResult(target);
        if (!containerResult.IsSuccess || containerResult.Value is null)
        {
            return GenericResult<IRecordSource<DataRecord>>.Failure(
                ConfigurationGatewayLog.ExecuteFailed(_logger, target.Container,
                    containerResult.CurrentMessage ?? "Container could not be resolved from the configuration schema"));
        }

        var container = containerResult.Value;

        if (connectionResult.Value is not IRecordSourceConnection recordSourceConnection)
        {
            return GenericResult<IRecordSource<DataRecord>>.Failure(
                ConfigurationGatewayLog.ExecuteFailed(_logger, target.Container,
                    "Connection does not support streaming record sources (IRecordSourceConnection)"));
        }

        try
        {
            return await recordSourceConnection.OpenRecordSource(command, container, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            return GenericResult<IRecordSource<DataRecord>>.Failure(
                ConfigurationGatewayLog.ExecuteException(_logger, ex, target.Container));
        }
    }

    /// <inheritdoc/>
    public async Task<IGenericResult<IEnumerable<object>>> Execute(
        IDataCommand command, DataStoreTarget target, Type rowType, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(target.DataStore))
        {
            return GenericResult<IEnumerable<object>>.Failure(
                ConfigurationGatewayLog.ExecuteFailed(_logger, target.Container, "DataStoreTarget.DataStore is required"));
        }

#pragma warning disable VSTHRD011, VSTHRD003
        var connectionResult = await _connectionLazy.Value.ConfigureAwait(false);
#pragma warning restore VSTHRD011, VSTHRD003
        if (!connectionResult.IsSuccess || connectionResult.Value is null)
            return connectionResult.ToNewResult<IEnumerable<object>>();

        var containerResult = ResolveContainerResult(target);
        if (!containerResult.IsSuccess || containerResult.Value is null)
        {
            return GenericResult<IEnumerable<object>>.Failure(
                ConfigurationGatewayLog.ExecuteFailed(_logger, target.Container,
                    containerResult.CurrentMessage ?? "Container could not be resolved from the configuration schema"));
        }

        var container = containerResult.Value;

        try
        {
            return await connectionResult.Value.Execute(command, container, rowType, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            return GenericResult<IEnumerable<object>>.Failure(
                ConfigurationGatewayLog.ExecuteException(_logger, ex, target.Container));
        }
    }

    /// <inheritdoc/>
    public async Task<IGenericResult> Execute(IDataCommand command, DataStoreTarget target, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(target.DataStore))
        {
            return GenericResult.Failure(
                ConfigurationGatewayLog.ExecuteFailed(_logger, target.Container, "DataStoreTarget.DataStore is required"));
        }

#pragma warning disable VSTHRD011, VSTHRD003
        var connectionResult = await _connectionLazy.Value.ConfigureAwait(false);
#pragma warning restore VSTHRD011, VSTHRD003
        if (!connectionResult.IsSuccess || connectionResult.Value is null)
            return connectionResult;

        var connection = connectionResult.Value;

        var containerResult = ResolveContainerResult(target);
        if (!containerResult.IsSuccess || containerResult.Value is null)
        {
            return GenericResult.Failure(
                ConfigurationGatewayLog.ExecuteFailed(_logger, target.Container,
                    containerResult.CurrentMessage ?? "Container could not be resolved from the configuration schema"));
        }

        var container = containerResult.Value;

        try
        {
            return await connection.Execute(command, container, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            return GenericResult.Failure(
                ConfigurationGatewayLog.ExecuteException(_logger, ex, target.Container));
        }
    }

    /// <inheritdoc/>
    public Task<IGenericResult<T>> Execute<T>(IDataCommand command, DataSetTarget target, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(GenericResult<T>.Failure(
            ConfigurationGatewayLog.DataSetTargetNotSupported(_logger, target.DataSet)));
    }

    /// <inheritdoc/>
    public async Task<IGenericResult<IDataGatewayTransaction>> BeginTransaction(
        string connectionName,
        CancellationToken cancellationToken = default)
    {
#pragma warning disable VSTHRD011, VSTHRD003
        var connectionResult = await _connectionLazy.Value.ConfigureAwait(false);
#pragma warning restore VSTHRD011, VSTHRD003
        if (!connectionResult.IsSuccess || connectionResult.Value is null)
            return connectionResult.ToNewResult<IDataGatewayTransaction>();

        if (connectionResult.Value is not ITransactionalDataConnection transactional)
        {
            return GenericResult<IDataGatewayTransaction>.Failure(
                DataGatewayLogger.TransactionNotSupported(_logger, connectionName));
        }

        var txnResult = await transactional.BeginTransaction(cancellationToken).ConfigureAwait(false);
        if (!txnResult.IsSuccess || txnResult.Value == null)
        {
            return GenericResult<IDataGatewayTransaction>.Failure(
                DataGatewayLogger.BeginTransactionFailed(_logger, connectionName,
                    txnResult.CurrentMessage ?? "Transaction start failed"));
        }

        IDataGatewayTransaction scope = new DataGatewayTransaction(
            txnResult.Value,
            connectionName,
            (target, ct) => Task.FromResult(ResolveContainerResult(target)),
            _logger,
            enforceConnectionMatch: false);

        return GenericResult<IDataGatewayTransaction>.Success(scope);
    }

    // =========================================================================
    // Connection building
    // =========================================================================

    private IGenericResult<IConnectionType> ResolveConnectionType()
    {
        for (var i = 0; i < _schema.Connections.Count; i++)
        {
            if (!string.Equals(_schema.Connections[i].Name, ConnectionName, StringComparison.OrdinalIgnoreCase))
                continue;

            if (string.IsNullOrWhiteSpace(_schema.Connections[i].ServiceOptionType))
            {
                return GenericResult<IConnectionType>.Failure(
                    DataGatewayCacheLog.CachePartitionUnavailable(
                        _logger, ConnectionName, "the connection declares no ServiceOptionType"));
            }

            if (ReferenceEquals(ConnectionTypes.ByName(_schema.Connections[i].ServiceOptionType), ConnectionTypes.NotFound))
            {
                return GenericResult<IConnectionType>.Failure(
                    DataGatewayCacheLog.CachePartitionUnavailable(
                        _logger,
                        ConnectionName,
                        $"connection type '{_schema.Connections[i].ServiceOptionType}' is not registered"));
            }

            return GenericResult<IConnectionType>.Success(
                ConnectionTypes.ByName(_schema.Connections[i].ServiceOptionType));
        }

        return GenericResult<IConnectionType>.Failure(
            DataGatewayCacheLog.CachePartitionUnavailable(
                _logger, ConnectionName, "the connection is not declared in configurationSchema.json"));
    }

    private async Task<IGenericResult<IDataConnection>> BuildConnection(CancellationToken cancellationToken)
    {
        ConfigurationGatewayLog.BuildConnectionEntry(_logger, ConnectionName);

        ConnectionConfiguration? configDbEntry = null;

        for (var i = 0; i < _schema.Connections.Count; i++)
        {
            if (string.Equals(_schema.Connections[i].Name, ConnectionName, StringComparison.OrdinalIgnoreCase))
            {
                configDbEntry = _schema.Connections[i];
                break;
            }
        }

        if (configDbEntry is null)
        {
            return GenericResult<IDataConnection>.Failure(
                ConfigurationGatewayLog.ConnectionNotFound(_logger, ConnectionName));
        }

        // A connection factory builds from the implementation configuration — the declared record
        // names the connection and says which kind it is, the body carries what the kind needs.
        if (configDbEntry.Configuration is null)
        {
            return GenericResult<IDataConnection>.Failure(
                ConfigurationGatewayLog.ConnectionNotFound(_logger, ConnectionName));
        }

        var factoryResult = await _connectionFactory
            .Create(configDbEntry.Configuration, _secretManager, cancellationToken)
            .ConfigureAwait(false);
        if (!factoryResult.IsSuccess || factoryResult.Value is null)
        {
            var reason = factoryResult.CurrentMessage?.ToString() ?? "factory returned failure";
            return GenericResult<IDataConnection>.Failure(
                ConfigurationGatewayLog.ConnectionCreationFailed(_logger, ConnectionName, reason));
        }

        if (factoryResult.Value is not IDataConnection dataConnection)
        {
            return GenericResult<IDataConnection>.Failure(
                ConfigurationGatewayLog.ConnectionCreationFailed(
                    _logger,
                    ConnectionName,
                    $"factory returned {factoryResult.Value.GetType().Name} which does not implement IDataConnection"));
        }

        ConfigurationGatewayLog.BuildConnectionExit(_logger, ConnectionName, success: true);
        return GenericResult<IDataConnection>.Success(dataConnection);
    }

    // =========================================================================
    // Tree building — per-store builder (replaces BuildFromSchema/BuildTree/BuildStore/
    // BuildPath/BuildContainer/ResolveKeys/BuildKeyFields/MakeReferencingKeysLazy)
    // =========================================================================

    private static List<IDataStore> BuildFromSchema(
        ConfigurationSchema schema,
        ILogger logger)
    {
        if (schema.DataStores.Count == 0)
        {
            ConfigurationGatewayLog.SchemaEmpty(logger);
            return [];
        }

        var builtStores = new List<IDataStore>(schema.DataStores.Count);
        var totalContainers = 0;

        foreach (var storeCfg in schema.DataStores)
        {
            var store = BuildStore(storeCfg, logger);
            if (store is null)
                continue;

            builtStores.Add(store);
            for (var i = 0; i < store.Paths.Count; i++)
                totalContainers += store.Paths[i].Containers.Count;
        }

        ConfigurationGatewayLog.TreeBuiltFromSchema(logger, builtStores.Count, totalContainers);
        return builtStores;
    }

    private static IDataStore? BuildStore(DataStoreConfiguration storeCfg, ILogger logger)
    {
        var storeType = string.IsNullOrEmpty(storeCfg.TypeId)
            ? null
            : DataStoreTypes.All().FirstOrDefault(t =>
                string.Equals(t.Name, storeCfg.TypeId, StringComparison.OrdinalIgnoreCase));

        if (storeType is null)
        {
            ConfigurationGatewayLog.ExecuteFailed(logger, storeCfg.Name,
                $"DataStore transport '{storeCfg.TypeId}' is not a registered DataStoreType");
            return null;
        }

        var builder = storeType.SupplyBuilder(logger);

        var configureResult = builder.Configure(storeCfg);
        if (!configureResult.IsSuccess)
            return null;

#pragma warning disable VSTHRD002
        var buildResult = builder.Build().GetAwaiter().GetResult();
#pragma warning restore VSTHRD002
        if (!buildResult.IsSuccess || buildResult.Value is null)
        {
            ConfigurationGatewayLog.ExecuteFailed(logger, storeCfg.Name,
                buildResult.CurrentMessage?.ToString() ?? "store build failed");
            return null;
        }

        return buildResult.Value;
    }

    // =========================================================================
    // Container resolution
    // =========================================================================

    private IGenericResult<IDataContainer> ResolveContainerResult(DataStoreTarget target)
    {
        if (string.IsNullOrWhiteSpace(target.Container))
            return GenericResult<IDataContainer>.Failure(ConfigurationGatewayLog.ResolveContainerEmpty(_logger));

        var stores = _dataStores.Value;
        var dataStoreName = target.DataStore;

        IDataStore? store = null;
        for (var i = 0; i < stores.Count; i++)
        {
            if (string.Equals(stores[i].Name, dataStoreName, StringComparison.OrdinalIgnoreCase))
            {
                store = stores[i];
                break;
            }
        }

        if (store is null)
        {
            return GenericResult<IDataContainer>.Failure(ConfigurationGatewayLog.ResolveContainerStoreNotFound(
                _logger, dataStoreName, stores.Count, string.Join(", ", stores.Select(s => $"'{s.Name}'"))));
        }

        if (!string.IsNullOrWhiteSpace(target.Path))
        {
            var pathResult = store.Path(target.Path);
            if (!pathResult.IsSuccess)
                return pathResult.ToNewResult<IDataContainer>();

            return pathResult.Value!.Container(target.Container);
        }

        var paths = store.Paths;
        for (var i = 0; i < paths.Count; i++)
        {
            var candidateResult = paths[i].Container(target.Container);
            if (candidateResult.IsSuccess)
                return candidateResult;
        }

        return GenericResult<IDataContainer>.Failure(
            ConfigurationGatewayLog.ResolveContainerNotFoundInAnyPath(_logger, target.Container, dataStoreName));
    }

    // Why these are explicit and refuse: a data gateway routes a command to a connection using an
    // address the caller supplies alongside it. IGenericService's command surface carries no address,
    // so there is no honest answer -- it fails loud rather than guessing a store.
    string IPlatformService.Id => "ConfigurationGateway";

    string IPlatformService.ServiceType => "DataGateway";

    bool IPlatformService.IsAvailable => true;

    Task<IGenericResult<T>> IGenericService.Execute<T>(IGenericCommand command, CancellationToken cancellationToken)
        => Task.FromResult(GenericResult<T>.Failure(
            DataGatewayProviderLog.CommandCarriesNoAddress(_logger)));

    Task<IGenericResult> IGenericService.Execute(IGenericCommand command, CancellationToken cancellationToken)
        => Task.FromResult<IGenericResult>(GenericResult.Failure(
            DataGatewayProviderLog.CommandCarriesNoAddress(_logger)));

}
