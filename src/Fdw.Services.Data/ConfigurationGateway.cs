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
    private const string ConfigurationDbConnectionName = "ConfigurationDb";
    private const string ConnectionTypeMsSql = "MsSql";
    private const string ConnectionTypePostgreSql = "PostgreSql";

    private readonly ILogger<ConfigurationGateway> _logger;
    private readonly IConnectionFactory _connectionFactory;
    private readonly ISecretManager? _secretManager;
    // Why: ConfigurationSchema is the deserialized configurationSchema.json — a static file
    // loaded once at startup. No IOptionsMonitor needed because the file does not change at
    // runtime. Singleton lifetime ensures the schema is read exactly once.
    private readonly ConfigurationSchema _schema;

    // Why: Connection is built lazily on first Execute call. The factory + resolved config
    // are captured at gateway construction; the actual SqlConnection open occurs on first use.
    // LazyThreadSafetyMode.ExecutionAndPublication ensures exactly one build even under racing threads.
    // Why: Task-typed Lazy because BuildConnection awaits the secret manager (ISecretManager.Execute
    // is async — most secret stores call external systems). The Lazy holds the Task so concurrent
    // first-callers all await the same in-flight build.
    private readonly Lazy<Task<IGenericResult<IDataConnection>>> _connectionLazy;

    // Why: Tree is built lazily on first container resolution rather than at construction time,
    // so the full TypeCollection (DataStoreTypes, etc.) is populated by module initializers
    // before the tree is built.
    private readonly Lazy<IReadOnlyList<IDataStore>> _dataStores;

    // Why: Cache and options are optional (null disables caching) so the gateway works
    // in contexts where DataGatewayResultCache has not been registered (e.g. test hosts or
    // apps that register ConfigurationGateway before DefaultDataGatewayServiceType runs).
    private readonly DataGatewayResultCache? _cache;
    private readonly IOptions<DataGatewayOptions>? _options;

    private static readonly TimeSpan DefaultCacheDuration = TimeSpan.FromMinutes(5);

    /// <summary>
    /// Initializes a new instance of <see cref="ConfigurationGateway"/> without a secret manager.
    /// Use when the ConfigurationDb connection uses integrated auth or does not need secret resolution.
    /// </summary>
    /// <param name="connectionFactory">Factory used to open a connection to ConfigurationDb.</param>
    /// <param name="schema">
    /// Deserialized <see cref="ConfigurationSchema"/> from <c>configurationSchema.json</c>.
    /// Registered as a singleton via
    /// <c>AddConfigurationGateway&lt;TFactory&gt;(services, jsonFilePath)</c>.
    /// </param>
    /// <param name="logger">Logger (optional — falls back to NullLogger).</param>
    /// <param name="cache">Optional process-wide result cache. When null caching is disabled.</param>
    /// <param name="options">Optional gateway options (EnableCache knob). When null caching is disabled.</param>
    public ConfigurationGateway(
        IConnectionFactory connectionFactory,
        ConfigurationSchema schema,
        ILogger<ConfigurationGateway>? logger = null,
        DataGatewayResultCache? cache = null,
        IOptions<DataGatewayOptions>? options = null)
        : this(connectionFactory, secretManager: null, schema, logger, cache, options)
    {
    }

    /// <summary>
    /// Initializes a new instance of <see cref="ConfigurationGateway"/> with an optional secret manager.
    /// </summary>
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
    public ConfigurationGateway(
        IConnectionFactory connectionFactory,
        ISecretManager? secretManager,
        ConfigurationSchema schema,
        ILogger<ConfigurationGateway>? logger = null,
        DataGatewayResultCache? cache = null,
        IOptions<DataGatewayOptions>? options = null)
    {
        _connectionFactory = connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));
        _secretManager = secretManager;
        _schema = schema ?? throw new ArgumentNullException(nameof(schema));
        _logger = logger ?? NullLogger<ConfigurationGateway>.Instance;
        _cache = cache;
        _options = options;

        // Why: ConfigurationDb configuration is resolved once at construction time (not per-Execute)
        // so the schema object is read exactly once. Wrapping in Lazy<> ensures the factory call
        // happens on the first Execute, not eagerly at DI startup. The Lazy holds a Task because
        // the factory body calls ISecretManager.Execute (async).
        // Why suppress VSTHRD011: BuildConnection uses ConfigureAwait(false) throughout and the
        // underlying secret-manager is sync (env-var read). Documented Lazy<Task<T>> deadlocks
        // require captured sync contexts and cross-thread Task transfer — neither applies here.
#pragma warning disable VSTHRD011
        _connectionLazy = new Lazy<Task<IGenericResult<IDataConnection>>>(
            () => BuildConnection(default),
            LazyThreadSafetyMode.ExecutionAndPublication);
#pragma warning restore VSTHRD011

        // Why: Tree is built lazily on first container resolution (first Execute call that needs
        // a container). LazyThreadSafetyMode.ExecutionAndPublication ensures only one build runs
        // even if multiple threads race on the first request.
        _dataStores = new Lazy<IReadOnlyList<IDataStore>>(
            () => BuildFromSchema(_schema, _logger),
            LazyThreadSafetyMode.ExecutionAndPublication);

        ConfigurationGatewayLog.Initialised(_logger, ConfigurationDbConnectionName);
    }

    /// <summary>
    /// Gets the IDataStore tree built from the ConfigurationSchema.
    /// Consumed by <see cref="ConfigurationContainerLookup"/> without a separate registration.
    /// </summary>
    public IReadOnlyList<IDataStore> DataStores => _dataStores.Value;

    /// <inheritdoc/>
    public Task<IGenericResult<T>> Execute<T>(IDataCommand command, DataStoreTarget target, CancellationToken cancellationToken = default)
        // Why: Default cacheable-read path — delegates to the useCache overload with cache reads enabled.
        // All existing callers that call the no-useCache overload automatically get caching via this
        // forwarding, so no call-site changes are needed.
        => Execute<T>(command, target, useCache: true, cancellationToken);

    /// <inheritdoc/>
    public async Task<IGenericResult<T>> Execute<T>(IDataCommand command, DataStoreTarget target, bool useCache, CancellationToken cancellationToken = default)
    {
        // Why: enable = both the cache singleton and the options knob are present AND EnableCache=true.
        // When either is null (contexts without wired caching) caching is simply off — no fallback, no NRE.
        bool enable = _cache is not null && _options is not null && _options.Value.EnableCache;

        string? cacheKey = null;
        if (enable)
        {
            try
            {
                // Why: key prefix "_cfg|" distinguishes configuration-gateway cached results from
                // data-gateway cached results in the shared DataGatewayResultCache store.
                // typeof(T).FullName prevents type mismatches across generic invocations with the same
                // query shape.
                cacheKey = string.Concat("_cfg|", CacheKeyBuilder.ComputeCacheKey(command, target), ":", typeof(T).FullName);
            }
            catch (Exception ex)
            {
                return GenericResult<T>.Failure(
                    DataGatewayCacheLog.KeyComputationFailed(_logger, command.CommandType, target.Container, ex.Message));
            }

            // Why: Only read from cache when useCache=true (the default). useCache=false is a
            // force-refresh: skip the cache read so the fresh result replaces the stale entry below.
            if (useCache && _cache!.TryGet<T>(cacheKey, out var cached) && cached is not null)
                return cached;
        }

        var result = await ExecuteCore<T>(command, target, cancellationToken).ConfigureAwait(false);

        // Why: ALWAYS write on success when caching is enabled — even on useCache=false (force-refresh).
        // Writing the fresh result ensures subsequent default reads see the updated value from cache.
        // Tags are "{path}.{container}" (e.g. "conn.Connection") — the same format that providers
        // use when calling ICacheInvalidator.InvalidateByTag(Commands().CacheTag(pathName)).
        if (cacheKey is not null && result.IsSuccess)
        {
            _cache!.Set(
                cacheKey,
                result,
                CacheKeyBuilder.GetInvalidationTags(command, target),
                CachePolicy.GetDuration(command, DefaultCacheDuration));
        }

        return result;
    }

    // Why: ExecuteCore is the raw fresh-execution path — connection resolution → container lookup → execute.
    // It runs on every cache miss and on the useCache=false force-refresh path. No caching here.
    private async Task<IGenericResult<T>> ExecuteCore<T>(IDataCommand command, DataStoreTarget target, CancellationToken cancellationToken)
    {
        ConfigurationGatewayLog.ExecuteEntry(_logger, target.DataStore, target.Path, target.Container);

        // Why: ConfigurationGateway always routes to its single ConfigurationDb connection;
        // target.DataStore is used only for container resolution in the IDataStore tree
        // (same as how command.DataStoreName is used in the command-only Execute).
        // target.DataStore must be non-empty — a missing store name means the caller has no
        // addressing information, so fail loud instead of silently routing to the wrong place.
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

        // Why: resolve container from target fields instead of command fields — the target
        // is the canonical address on this overload. The command carries the query shape only.
        // Why (FDW-583): propagate ResolveContainerResult's own message as the ExecuteFailed reason
        // instead of the generic literal that used to hide which of the four resolution causes fired.
        var containerResult = ResolveContainerResult(target);

        // Why: the Execute seam takes the unified container only — there is no container-less
        // overload. Every configuration read targets a container; a failure here means the schema tree
        // could not resolve the target's container, so fail loud rather than route container-less.
        if (!containerResult.IsSuccess)
        {
            ConfigurationGatewayLog.ExecuteFailed(_logger, target.Container,
                containerResult.CurrentMessage ?? "Container could not be resolved from the configuration schema");

            // Why: return the RESOLUTION result rather than a fresh message-only failure. ToNewResult
            // preserves Code/InnerResult/Details/Messages, so the node layer's typed cause
            // (DataPathNotFound / ContainerNotFoundInPath) survives to the caller instead of being
            // flattened into text that could only be matched on. Callers that must distinguish "this
            // host's schema does not register that container" — a structural, permanent property — from
            // a genuine load failure read Code/CodeChain; the logged line above is unchanged.
            return containerResult.ToNewResult<T>();
        }

        // Why: a successful resolution with a null container would mean the seam handed back nothing to
        // execute against — no fallback container is invented, so fail loud with its own message.
        if (containerResult.Value is null)
        {
            return GenericResult<T>.Failure(
                ConfigurationGatewayLog.ExecuteFailed(_logger, target.Container,
                    "Container could not be resolved from the configuration schema"));
        }

        var container = containerResult.Value;

        // Why: the gateway no longer composes child collections/KVP. Aggregate composition (typed body +
        // child collections + KVP) now lives entirely in DefaultConfigurationProvider.Get, mirroring the
        // write path — the gateway is a single-row/single-query executor again. The provider drives child
        // loads through the by-type Execute overload below.
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

        // Why (FDW-583): propagate ResolveContainerResult's own message — see ExecuteCore's identical fix.
        var containerResult = ResolveContainerResult(target);
        if (!containerResult.IsSuccess || containerResult.Value is null)
        {
            return GenericResult<IRecordSource<DataRecord>>.Failure(
                ConfigurationGatewayLog.ExecuteFailed(_logger, target.Container,
                    containerResult.CurrentMessage ?? "Container could not be resolved from the configuration schema"));
        }

        var container = containerResult.Value;

        // Why: streaming is an optional connection capability (IRecordSourceConnection); ConfigurationDb's
        // MsSql connection supports it. A connection that does not fails loud here rather than silently
        // materializing — symmetric with DataGatewayService.OpenRecordSource.
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

        // Why (FDW-583): propagate ResolveContainerResult's own message — see ExecuteCore's identical fix.
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
            // Why: by-type read — the child row type is known only at runtime (the provider's child
            // composition), so the element type is passed as an argument and the connection resolves the
            // element's generated mapper by name. No MakeGenericMethod, no Activator, no reflection.
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

        // Why (FDW-583): propagate ResolveContainerResult's own message — see ExecuteCore's identical fix.
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
            // Why: no-value Execute on the connection — the save cascade only needs success/failure
            // (the child INSERT returns no materialized rows), so there is no result type to close and
            // no reflection. Symmetric to the generic read overload above.
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
        // Why: ConfigurationGateway is a single-connection gateway to ConfigurationDb.
        // DataSet federation is exclusively a DataGatewayService concern. Any caller that
        // routes a DataSetTarget here has mis-wired its dependencies — fail loud.
        return Task.FromResult(GenericResult<T>.Failure(
            ConfigurationGatewayLog.DataSetTargetNotSupported(_logger, target.DataSet)));
    }

    /// <inheritdoc/>
    public async Task<IGenericResult<IDataGatewayTransaction>> BeginTransaction(
        string connectionName,
        CancellationToken cancellationToken = default)
    {
        // Why: ConfigurationGateway always uses its single ConfigurationDb connection;
        // connectionName is accepted for API conformance but the internal connection is
        // always the one built by _connectionLazy.
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

        // Why: enforceConnectionMatch:false — ConfigurationGateway is single-connection (every
        // command routes to its one ConfigurationDb connection), so the cross-connection guard
        // would wrongly reject commands that target a DataStore with a name different from the
        // connection name.
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

    // Why: Called once from the Lazy<> value factory. Finds "ConfigurationDb" in the schema's
    // Connections list and calls the injected IConnectionFactory to create a connection.
    // If the schema has no such connection, or the factory returns failure, the result is
    // non-success — callers get a failure result from Execute, not a null-reference crash.
    private async Task<IGenericResult<IDataConnection>> BuildConnection(CancellationToken cancellationToken)
    {
        ConfigurationGatewayLog.BuildConnectionEntry(_logger, ConfigurationDbConnectionName);

        ConnectionConfiguration? configDbEntry = null;

        for (var i = 0; i < _schema.Connections.Count; i++)
        {
            if (string.Equals(_schema.Connections[i].Name, ConfigurationDbConnectionName, StringComparison.OrdinalIgnoreCase))
            {
                configDbEntry = _schema.Connections[i];
                break;
            }
        }

        if (configDbEntry is null)
        {
            return GenericResult<IDataConnection>.Failure(
                ConfigurationGatewayLog.ConnectionNotFound(_logger, ConfigurationDbConnectionName));
        }

        // Why: hand the gateway's own _secretManager (registered alongside it by
        // AddConfigurationGateway<TFactory, TSecretManager>) to the factory so secret resolution
        // happens here, in-flight, instead of routing through a separate secret-manager service
        // provider whose config provider would re-enter ConfigurationGateway via gateway.Execute
        // and trigger Lazy reentrancy on _connectionLazy.
        var factoryResult = await _connectionFactory
            .Create(configDbEntry, _secretManager, cancellationToken)
            .ConfigureAwait(false);
        if (!factoryResult.IsSuccess || factoryResult.Value is null)
        {
            var reason = factoryResult.CurrentMessage?.ToString() ?? "factory returned failure";
            return GenericResult<IDataConnection>.Failure(
                ConfigurationGatewayLog.ConnectionCreationFailed(_logger, ConfigurationDbConnectionName, reason));
        }

        if (factoryResult.Value is not IDataConnection dataConnection)
        {
            return GenericResult<IDataConnection>.Failure(
                ConfigurationGatewayLog.ConnectionCreationFailed(
                    _logger,
                    ConfigurationDbConnectionName,
                    $"factory returned {factoryResult.Value.GetType().Name} which does not implement IDataConnection"));
        }

        ConfigurationGatewayLog.BuildConnectionExit(_logger, ConfigurationDbConnectionName, success: true);
        return GenericResult<IDataConnection>.Success(dataConnection);
    }

    // =========================================================================
    // Tree building — per-store builder (replaces BuildFromSchema/BuildTree/BuildStore/
    // BuildPath/BuildContainer/ResolveKeys/BuildKeyFields/MakeReferencingKeysLazy)
    // =========================================================================

    // Why: each store config selects its transport's DataStoreType, which supplies an
    // IDataStoreBuilder; the builder is Configure()d with the nested store config and Build()s the
    // uniform IDataNode tree (store -> paths -> containers -> fields -> keys, FK-direct). This is the
    // single tree-build mechanism shared with ConfigurationGatewayDataStoreProvider.Load — no more
    // duplicated if-MsSql tree builders, no async-fields/materialization.
    // Why: concrete List<T> return per CA1859 — assigned into the IReadOnlyList<IDataStore> lazy.
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

    // Why: resolves the transport's builder, configures it with the store config, and builds the
    // store tree. Returns null (skipped, logged) when the transport type is unknown or the build
    // fails — ResolveContainer then reports the store as not found. No fallback store is invented.
    private static IDataStore? BuildStore(DataStoreConfiguration storeCfg, ILogger logger)
    {
        // Why: TypeId on the store config carries the transport discriminator (e.g. "MsSql").
        // The matching registered DataStoreType supplies the per-transport builder. Use the same
        // All().FirstOrDefault lookup DataStoreTypesBuilderSelector.Select uses (proven path).
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

        // Why: the builder's Build() is async by contract but synchronous in practice (it assembles
        // from in-memory config and returns a completed task). The full-tree lazy is synchronous, so
        // the completed task is unwrapped here — there is no real awaiting and no deadlock risk.
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

    // Why (FDW-583): the previous nullable-returning ResolveContainer silently discarded the reason for
    // every miss (empty container, store not found, path lookup failure, container-in-path failure) —
    // every Execute overload below then logged the same generic literal
    // "Container could not be resolved from the configuration schema" regardless of which of those four
    // distinct causes actually occurred. This overload returns the specific IGenericMessage for each
    // cause so the printed Error names the real reason.
    // Why: target-typed overload reads addressing from DataStoreTarget, eliminating the
    // legacy ConnectionName-as-fallback path that exists in the command overload. All three
    // fields in the target are explicit; DataStore must be non-empty (validated by the caller).
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

        // Why: Path is null — search all paths in the store, preserving the same behaviour
        // as the command overload when PathName is absent. Per-path misses along the way are the
        // shared node's own (Debug-level) navigation misses; only the overall "no path had it"
        // outcome below is the terminal failure for this call.
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

}
