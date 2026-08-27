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

    // Why optional, and why this exact expression: the connection layer selects its session context
    // from _authenticationContextAccessor?.Current (MsSqlConnection.SetUserSessionContext). Reading
    // the SAME expression here is what makes the cache partition name the same principal the session
    // is actually opened under. A null accessor yields a null context, which every scheme must govern
    // — the reference scheme sends it to Deny — so this is not a fallback for a missing value, it is
    // the identical input producing the identical decision.
    private readonly IAuthenticationContextAccessor? _authenticationContextAccessor;

    // Why lazy and never resolved in the constructor: ConnectionTypes.ByName freezes the collection,
    // and connection kinds register into it from their own assemblies' module initializers. Freezing
    // it at gateway construction would lock out any kind whose assembly had not loaded yet — the same
    // load-order hazard the _dataStores Lazy exists to avoid.
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
        IOptions<DataGatewayOptions>? options = null,
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
        IOptions<DataGatewayOptions>? options = null,
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

        ConfigurationGatewayLog.Initialised(_logger, ConnectionName);
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
        // Why the command kind gates everything below: a cache serves REPEATED READS of the same
        // question. A write is not a question - it is not idempotent, and its result describes what
        // one execution did. Caching it is wrong twice over: the entry is stale the moment a later
        // write lands, and, because ComputeCacheKey only varies by filter/ordering/paging (fields a
        // write does not have), two different writes to the same container compute the SAME key. With
        // caching on, the second write would be answered from the first one's cached result and never
        // execute at all. Reads cache; writes invalidate and are never read from cache.
        bool isQuery = command is IQueryCommand;

        string? cacheKey = null;
        // Why the ceiling is captured here rather than the kind: these are the only two things the
        // write below needs, and taking them together binds both to the one resolution that produced
        // the key. TimeSpan.MaxValue is the identity for the minimum CachePolicy applies, so it is the
        // correct starting value for the paths that never consult a kind at all.
        var cacheCeiling = TimeSpan.MaxValue;
        if (CacheEnabled && isQuery)
        {
            // Why the read fails instead of proceeding uncached: without a partition the gateway
            // cannot tell which callers may share a result. Continuing would either poison the cache
            // for other principals or serve this caller a result from a different visibility scope,
            // so there is no safe degraded mode — and silently skipping the cache would hide a
            // misconfiguration that has to be fixed.
            var connectionTypeResult = _connectionTypeLazy.Value;
            if (!connectionTypeResult.IsSuccess || connectionTypeResult.Value is null)
                return connectionTypeResult.ToNewResult<T>();

            cacheCeiling = connectionTypeResult.Value.MaxCacheDuration(_authenticationContextAccessor?.Current);

            try
            {
                // Why the partition leads the key: results are visible only to callers whose session
                // reads under the same scope, so the scope is part of the identity of a cached entry,
                // not a qualifier on it. The connection kind computes it; this gateway never parses
                // it or learns anything about the kind from it.
                // Why: key prefix "_cfg|" distinguishes configuration-gateway cached results from
                // data-gateway cached results in the shared DataGatewayResultCache store.
                // typeof(T).FullName prevents type mismatches across generic invocations with the same
                // query shape.
                cacheKey = string.Concat(
                    connectionTypeResult.Value.CachePartition(_authenticationContextAccessor?.Current),
                    "|_cfg|", CacheKeyBuilder.ComputeCacheKey(command, target), ":", typeof(T).FullName);
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

    // Why a single property rather than the expression repeated: both the read path and the outcome
    // path must agree on whether caching is on. If they can disagree, a write invalidates a cache
    // that reads never populate, or worse the reverse.
    private bool CacheEnabled => _cache is not null && _options is not null && _options.Value.EnableCache;

    // Why this is its own method: it is the whole of what caching DOES with an execution's outcome,
    // and Execute is already carrying connection resolution, key computation and dispatch. Reading
    // it in one place is also what makes the read/write asymmetry legible - the same result object
    // is either stored under a key or used as the signal to drop keys.
    //
    // Why the write path invalidates here and providers no longer carry an ICacheInvalidator: this
    // gateway is the ONLY thing that persists a change, so it is the only place that can know a
    // change happened. Threading an invalidator through every provider asked 61 call sites to
    // remember to announce a write they did not perform - and the tags they passed were the same
    // "{path}.{container}" this computes from the command it just ran.
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

        // Why every tag and not just the container: GetInvalidationTags honours a command's own
        // CacheInvalidationTags metadata, which is how a write that touches rows another container
        // projects declares the blast radius of what it changed.
        _cache!.InvalidateByTags(CacheKeyBuilder.GetInvalidationTags(command, target));
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
        // child collections + KVP) now lives entirely in ImplementationConfigurationProviderBase.Get, mirroring the
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
    // Why a second schema scan rather than reusing BuildConnection's: BuildConnection is async (it
    // awaits secret resolution) and returns an open connection, while this must run on the
    // synchronous cache-key path and needs only the declared kind. Both read the same one entry, so
    // they cannot disagree about which connection ConfigurationDb is.
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

            // Why fail loud on NotFound rather than partitioning under some placeholder: an
            // unregistered kind means we cannot know what its sessions would show, and a guessed
            // partition would let callers with different visibility share cached rows.
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

        // Why: hand the gateway's own _secretManager (registered alongside it by
        // resolved from the container) to the factory so secret resolution
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
