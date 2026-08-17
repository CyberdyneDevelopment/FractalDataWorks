using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Dynamic;
using System.Linq;
using System.Transactions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Fdw.Commands.Data;
using Fdw.Commands.Data.Abstractions;
using Fdw.Commands.Data.Abstractions.Caching;
using Fdw.Conventions;
using Fdw.Data;
using Fdw.Data.Abstractions;
using Fdw.Data.Abstractions.Mappers.PocoMappers;
using Fdw.Data.DataContainers.Abstractions;
using Fdw.Data.DataSets.Abstractions;
using Fdw.Data.RowSources.Abstractions;
using Fdw.Messages;
using Fdw.Results;
using Fdw.Services.Authentication.Abstractions.Security;
using Fdw.Services.Connections;
using Fdw.Services.Connections.Abstractions;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Data.Caching;
using Fdw.Services.Data.Execution;
using Fdw.Services.Data.Logging;
using Fdw.Services.Data.Results;

namespace Fdw.Services.Data;

/// <summary>
/// Default implementation of the DataGateway service.
/// Routes commands to the appropriate connection based on ConnectionName.
/// Manages container metadata and dataset federation.
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class DataGatewayService : IDataGateway
{
    private readonly ILogger<DataGatewayService> _logger;
    private readonly IDataConnectionProvider _connectionProvider;
    // Why: IDataSetConfigurationProvider (not IDataSetProvider) is used here because ExecuteDataSet
    // needs DataSetConfiguration records for query construction — not the live IDataSet runtime.
    // Lazy<T> breaks the circular DI: DataGatewayService → DataSetProvider → DataGatewayService.
    // Why: field mappings are now composed by DataSetConfigurationProvider.Get — no IDataSetSourceResolver needed.
    private readonly Lazy<IDataSetConfigurationProvider> _dataSetProvider;
    // Why: DataStoreConfigurationProvider (dual-source) merges system (ctrl) and user (cfg) DataStore
    // configs. Routing reads the store's ConnectionId from its config record on demand.
    private readonly DataStoreConfigurationProvider _dataStoreConfigProvider;
    // Why: the eager full-tree singleton is deleted; container routing resolves the unified container
    // on demand via IDataStoreProvider.GetContainer(...) (returns IDataContainer). The INTERFACE (not
    // the concrete ConfigurationGatewayDataStoreProvider) is injected so the gateway depends on the
    // abstraction and stays unit-testable. Nullable for the bootstrap/test paths that never route
    // container commands.
    private readonly IDataStoreProvider? _dataStoreProvider;
    private readonly PredicatePushdownAnalyzer _predicatePushdown;
    // Why: RBAC enforcement is deferred until RequiredPermission is added to the IDataNode tree
    // (follow-up to Phase 7). Optional because DataGateway also runs in non-HTTP contexts.
    private readonly IFrameworkAuthorizationService? _authorizationService;

    // Why this accessor and not IHttpContextAccessor: the connection selects its session context from
    // IAuthenticationContextAccessor.Current, so reading the same source is what makes the cache
    // partition name the principal the session is actually opened under. A principal established off
    // the request thread — background jobs, ETL, boot elevation — has an authentication context and no
    // HttpContext, so an HTTP-sourced key cannot see it and collapses every such caller together.
    private readonly IAuthenticationContextAccessor? _authenticationContextAccessor;

    // Why: the cache partition is declared by the connection kind, and the kind is named by the
    // connection configuration the target's DataStore points at.
    private readonly ConnectionConfigurationProvider? _connectionConfigProvider;
    // Why: cache + options are injected by DI in production; null in test constructors that
    // don't wire caching — a null cache means caching is disabled for that instance.
    private readonly DataGatewayResultCache? _cache;
    private readonly IOptions<DataGatewayOptions>? _options;

    private static readonly TimeSpan DefaultCacheDuration = TimeSpan.FromMinutes(5);

    /// <summary>
    /// Initializes a new instance of the <see cref="DataGatewayService"/> class.
    /// </summary>
    /// <param name="loggerFactory">The logger factory for creating loggers.</param>
    /// <param name="connectionProvider">The data connection provider.</param>
    /// <param name="dataSetProvider">The dataset configuration provider for federated queries (lazy to break circular DI).</param>
    /// <param name="dataStoreConfigProvider">The DataStore configuration provider (dual-source).</param>
    /// <param name="authorizationService">Optional authorization service for permission validation.</param>
    /// <param name="dataStoreProvider">On-demand container resolver. When non-null, container commands
    /// resolve the unified container via <c>GetContainer(...)</c>; when null, container routing fails loud.</param>
    /// <param name="cache">Optional process-wide result cache. When null caching is disabled (test paths).</param>
    /// <param name="options">Optional gateway options (EnableCache knob). When null caching is disabled.</param>
    /// <param name="authenticationContextAccessor">
    /// Optional accessor for the calling principal, used to partition cached results by the visibility
    /// scope their session reads under. Read with the same expression the connection uses to select its
    /// session context, so the partition and the session cannot name different principals.
    /// </param>
    /// <param name="connectionConfigProvider">
    /// Optional provider used to name the connection kind behind a target's DataStore. Required whenever
    /// caching is enabled — without it the partition cannot be resolved and the read fails loud.
    /// </param>
    public DataGatewayService(
        ILoggerFactory? loggerFactory,
        IDataConnectionProvider connectionProvider,
        Lazy<IDataSetConfigurationProvider> dataSetProvider,
        DataStoreConfigurationProvider dataStoreConfigProvider,
        IFrameworkAuthorizationService? authorizationService = null,
        IDataStoreProvider? dataStoreProvider = null,
        DataGatewayResultCache? cache = null,
        IOptions<DataGatewayOptions>? options = null,
        IAuthenticationContextAccessor? authenticationContextAccessor = null,
        ConnectionConfigurationProvider? connectionConfigProvider = null)
    {
        var factory = loggerFactory ?? NullLoggerFactory.Instance;
        _logger = factory.CreateLogger<DataGatewayService>();

        _connectionProvider = connectionProvider;
        _dataSetProvider = dataSetProvider;
        _dataStoreConfigProvider = dataStoreConfigProvider;
        _authorizationService = authorizationService;
        _dataStoreProvider = dataStoreProvider;
        _cache = cache;
        _options = options;
        _authenticationContextAccessor = authenticationContextAccessor;
        _connectionConfigProvider = connectionConfigProvider;

        // Get internal implementation details - these are not injected
        _predicatePushdown = new PredicatePushdownAnalyzer(factory.CreateLogger<PredicatePushdownAnalyzer>());
    }

    /// <inheritdoc/>
    public async Task<IGenericResult<IDataGatewayTransaction>> BeginTransaction(
        string connectionName,
        CancellationToken cancellationToken = default)
    {
        DataGatewayLogger.TransactionScopeOpened(_logger, connectionName);

        var connectionResult = await _connectionProvider.Get<IDataConnection>(connectionName, cancellationToken)
            .ConfigureAwait(false);
        if (!connectionResult.IsSuccess || connectionResult.Value == null)
        {
            return GenericResult<IDataGatewayTransaction>.Failure(
                DataGatewayLogger.ConnectionRetrievalFailed(_logger, connectionName,
                    connectionResult.CurrentMessage ?? "Connection not found"));
        }

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
            ResolveContainerResult,
            _logger);

        return GenericResult<IDataGatewayTransaction>.Success(scope);
    }

    /// <inheritdoc/>
    public Task<IGenericResult<T>> Execute<T>(IDataCommand command, DataStoreTarget target, CancellationToken cancellationToken = default)
        // Why: Default cacheable-read path — delegates to the useCache overload with cache reads enabled.
        // All existing callers (LimitEnforcementDataGateway, tests) that call the no-useCache overload
        // automatically get caching via this forwarding, so no call site changes are needed.
        => Execute<T>(command, target, useCache: true, cancellationToken);

    /// <inheritdoc/>
    public async Task<IGenericResult<T>> Execute<T>(IDataCommand command, DataStoreTarget target, bool useCache, CancellationToken cancellationToken = default)
    {
        // Why: enable = both the cache singleton and the options knob are present AND EnableCache=true.
        // When either is null (test paths without wired caching) caching is simply off — no fallback, no NRE.
        bool enable = _cache is not null && _options is not null && _options.Value.EnableCache;
        bool cacheEnabled = enable && CachePolicy.IsEnabled(command);

        string? cacheKey = null;
        // Why the ceiling is captured here rather than the kind: these are the only two things the
        // write below needs, and taking them together binds both to the one resolution that produced
        // the key. TimeSpan.MaxValue is the identity for the minimum CachePolicy applies, so it is the
        // correct starting value for the paths that never consult a kind at all.
        var cacheCeiling = TimeSpan.MaxValue;
        if (cacheEnabled)
        {
            // Why the read fails instead of proceeding uncached: without a partition the gateway cannot
            // tell which callers may share a result, so continuing would either poison the cache for
            // other principals or serve this caller a result from a different visibility scope.
            var connectionTypeResult = await ResolveConnectionType(target, cancellationToken).ConfigureAwait(false);
            if (!connectionTypeResult.IsSuccess || connectionTypeResult.Value is null)
                return connectionTypeResult.ToNewResult<T>();

            var partition = connectionTypeResult.Value.CachePartition(_authenticationContextAccessor?.Current);
            cacheCeiling = connectionTypeResult.Value.MaxCacheDuration(_authenticationContextAccessor?.Current);

            try
            {
                // Why: key = the caller's visibility scope + query shape (target + command semantics) +
                // result type. A cached row is visible only to callers reading under the same scope, so
                // the scope is part of the entry's identity; typeof(T).FullName prevents type mismatches
                // across generic invocations with the same query shape.
                cacheKey = string.Concat(
                    partition, "|",
                    CacheKeyBuilder.ComputeCacheKey(command, target), ":", typeof(T).FullName);
            }
            catch (Exception ex)
            {
                return GenericResult<T>.Failure(
                    DataGatewayCacheLog.KeyComputationFailed(_logger, command.CommandType, target.Container, ex.Message));
            }

            // Why: Only read from cache when useCache=true (the default). useCache=false is a force-refresh:
            // skip the cache read so the fresh result replaces the stale cache entry when written below.
            if (useCache && _cache!.TryGet<T>(cacheKey, out var cached) && cached is not null)
                return cached;
        }

        var result = await ExecuteCore<T>(command, target, cancellationToken).ConfigureAwait(false);

        // Why: ALWAYS write on success when caching is enabled — even on useCache=false (force-refresh).
        // Writing the fresh result ensures subsequent default reads see the updated value from the cache.
        if (cacheKey is not null && result.IsSuccess)
        {
            _cache!.Set(
                cacheKey,
                result,
                CacheKeyBuilder.GetInvalidationTags(command, target),
                CachePolicy.GetDuration(
                    command,
                    DefaultCacheDuration,
                    cacheCeiling));
        }

        return result;
    }

    // Why: ExecuteCore is the raw fresh-execution path — container resolution → connection lookup → execute.
    // It runs on every cache miss and on the useCache=false force-refresh path. No caching here.
    private async Task<IGenericResult<T>> ExecuteCore<T>(IDataCommand command, DataStoreTarget target, CancellationToken cancellationToken)
    {
        // Why: Every DataGateway command targets a container, and every container is reached
        // via a DataStore. A command without target.DataStore is malformed — fail loud.
        if (string.IsNullOrWhiteSpace(target.DataStore))
            return GenericResult<T>.Failure(DataServiceResultCodes.ByName("DataStoreNameRequired"));

        DataGatewayLogger.ExecuteContainerEntering(_logger, target.DataStore, target.Path, target.Container);

        // Why: container resolution is on demand through ConfigurationGatewayDataStoreProvider.GetContainer
        // — it builds the one unified container (IDataContainer) from ConfigurationDb via the cached,
        // tag-invalidated gateway, replacing the deleted eager full-tree singleton. No IDataStoreProvider
        // = container routing cannot run; fail loud.
        // Why (FDW-583): propagate ResolveContainerResult's own message instead of re-deriving a generic
        // one here — it already names the real cause (missing store/path/container).
        var containerResult = await ResolveContainerResult(target, cancellationToken).ConfigureAwait(false);
        if (!containerResult.IsSuccess)
            return containerResult.ToNewResult<T>();

        var dataContainer = containerResult.Value;
        if (dataContainer is null)
            return GenericResult<T>.Failure(DataGatewayLogger.ContainerNotFound(_logger, target.Container));

        // Why: Connection is resolved from the DataStore's ConnectionId, not from command.ConnectionName.
        // "Address by DataStore, connection invisible" — the caller sets DataStoreName (via target); the
        // gateway reads the DataStore config record on demand (dual-source, gateway-cached) to find the
        // physical connection.
        var storeResult = await _dataStoreConfigProvider.Get(target.DataStore, cancellationToken).ConfigureAwait(false);
        if (!storeResult.IsSuccess || storeResult.Value is null)
        {
            return GenericResult<T>.Failure(
                DataGatewayLogger.DataStoreNotFoundForSource(_logger, target.DataStore));
        }

        var store = storeResult.Value;
        if (store.ConnectionId == Guid.Empty)
        {
            return GenericResult<T>.Failure(
                DataGatewayLogger.DataStoreHasNoConnectionId(_logger, target.DataStore));
        }

        var connectionResult = await _connectionProvider.Get(store.ConnectionId, cancellationToken).ConfigureAwait(false);
        if (!connectionResult.IsSuccess || connectionResult.Value == null)
        {
            var reason = connectionResult.CurrentMessage ?? "Unknown error";
            return GenericResult<T>.Failure(
                DataGatewayLogger.DataStoreConnectionNotResolved(_logger, target.DataStore, store.ConnectionId.ToString(), reason));
        }

        var connection = connectionResult.Value;

        DataGatewayLogger.ExecutingContainerCommand(_logger, command.GetType().Name, target.Container, target.DataStore);

        var stopwatch = Stopwatch.StartNew();
        var result = await connection.Execute<T>(command, dataContainer, cancellationToken).ConfigureAwait(false);
        stopwatch.Stop();

        if (result.IsSuccess)
            DataGatewayLogger.ContainerCommandCompleted(_logger, target.Container, stopwatch.Elapsed.TotalMilliseconds);
        else
            DataGatewayLogger.ContainerCommandFailed(_logger, target.Container, result.CurrentMessage ?? "Unknown error");

        return result;
    }

    // Why the connection kind decides this and not the gateway: what a caller may see is settled at the
    // connection, by the session context its scheme applies. The gateway holds the token and compares it;
    // it never parses it and learns nothing about the kind from it. A kind that declares no session-context
    // concept returns a constant, so its results keep caching globally.
    private async Task<IGenericResult<IConnectionType>> ResolveConnectionType(
        DataStoreTarget target,
        CancellationToken cancellationToken)
    {
        if (_connectionConfigProvider is null)
        {
            return GenericResult<IConnectionType>.Failure(
                DataGatewayCacheLog.CachePartitionUnavailable(
                    _logger, target.DataStore, "no connection configuration provider is registered"));
        }

        var storeResult = await _dataStoreConfigProvider.Get(target.DataStore, cancellationToken).ConfigureAwait(false);
        if (!storeResult.IsSuccess || storeResult.Value is null)
        {
            return GenericResult<IConnectionType>.Failure(
                DataGatewayCacheLog.CachePartitionUnavailable(
                    _logger, target.DataStore, "the DataStore could not be resolved"));
        }

        var connectionResult = await _connectionConfigProvider
            .Get(storeResult.Value.ConnectionId, cancellationToken).ConfigureAwait(false);
        if (!connectionResult.IsSuccess || connectionResult.Value is null)
        {
            return GenericResult<IConnectionType>.Failure(
                DataGatewayCacheLog.CachePartitionUnavailable(
                    _logger, target.DataStore, "the DataStore's connection configuration could not be resolved"));
        }

        if (string.IsNullOrWhiteSpace(connectionResult.Value.ServiceOptionType))
        {
            return GenericResult<IConnectionType>.Failure(
                DataGatewayCacheLog.CachePartitionUnavailable(
                    _logger, target.DataStore, "the connection declares no ServiceOptionType"));
        }

        // Why fail loud on NotFound: an unregistered kind means we cannot know what its sessions would
        // show, and a guessed partition would let callers with different visibility share cached rows.
        if (ReferenceEquals(ConnectionTypes.ByName(connectionResult.Value.ServiceOptionType), ConnectionTypes.NotFound))
        {
            return GenericResult<IConnectionType>.Failure(
                DataGatewayCacheLog.CachePartitionUnavailable(
                    _logger,
                    target.DataStore,
                    $"connection type '{connectionResult.Value.ServiceOptionType}' is not registered"));
        }

        return GenericResult<IConnectionType>.Success(
            ConnectionTypes.ByName(connectionResult.Value.ServiceOptionType));
    }

    /// <inheritdoc/>
    public async Task<IGenericResult<IRecordSource<DataRecord>>> OpenRecordSource(
        IDataCommand command, DataStoreTarget target, CancellationToken cancellationToken = default)
    {
        // Why: every record-source cursor targets a container reached via a DataStore — a missing
        // DataStore is a malformed request, fail loud (mirrors the Execute<T> DataStoreTarget guard).
        if (string.IsNullOrWhiteSpace(target.DataStore))
            return GenericResult<IRecordSource<DataRecord>>.Failure(DataServiceResultCodes.ByName("DataStoreNameRequired"));

        DataGatewayLogger.OpeningRecordSource(_logger, target.DataStore, target.Container);

        // Why (FDW-583): propagate ResolveContainerResult's own message instead of re-deriving a
        // generic one here — see ExecuteCore's identical fix above.
        var containerResult = await ResolveContainerResult(target, cancellationToken).ConfigureAwait(false);
        if (!containerResult.IsSuccess)
            return containerResult.ToNewResult<IRecordSource<DataRecord>>();

        var dataContainer = containerResult.Value;
        if (dataContainer is null)
            return GenericResult<IRecordSource<DataRecord>>.Failure(DataGatewayLogger.ContainerNotFound(_logger, target.Container));

        var storeResult = await _dataStoreConfigProvider.Get(target.DataStore, cancellationToken).ConfigureAwait(false);
        if (!storeResult.IsSuccess || storeResult.Value is null)
            return GenericResult<IRecordSource<DataRecord>>.Failure(DataGatewayLogger.DataStoreNotFoundForSource(_logger, target.DataStore));

        var store = storeResult.Value;
        if (store.ConnectionId == Guid.Empty)
            return GenericResult<IRecordSource<DataRecord>>.Failure(DataGatewayLogger.DataStoreHasNoConnectionId(_logger, target.DataStore));

        var connectionResult = await _connectionProvider.Get(store.ConnectionId, cancellationToken).ConfigureAwait(false);
        if (!connectionResult.IsSuccess || connectionResult.Value == null)
            return GenericResult<IRecordSource<DataRecord>>.Failure(
                DataGatewayLogger.DataStoreConnectionNotResolved(_logger, target.DataStore, store.ConnectionId.ToString(), connectionResult.CurrentMessage ?? "Unknown error"));

        // Why: streaming is an OPTIONAL connection capability — netstandard2.0 has no default interface
        // methods, so it lives on IRecordSourceConnection, not IDataConnection. Feature-detect it; a
        // connection that cannot stream fails loud here so the caller can fall back to materializing Execute.
        if (connectionResult.Value is not IRecordSourceConnection recordSourceConnection)
            return GenericResult<IRecordSource<DataRecord>>.Failure(
                DataGatewayLogger.RecordSourceNotSupported(_logger, target.DataStore));

        var result = await recordSourceConnection.OpenRecordSource(command, dataContainer, cancellationToken).ConfigureAwait(false);
        if (result.IsSuccess)
            DataGatewayLogger.RecordSourceOpened(_logger, target.DataStore, target.Container);

        return result;
    }

    /// <inheritdoc/>
    public async Task<IGenericResult<T>> Execute<T>(IDataCommand command, DataSetTarget target, CancellationToken cancellationToken = default)
    {
        // Why: a DataSet NAME is an INSTANCE, resolved live from the configuration provider — not a
        // member of DataSetTypes (which holds the strategy KINDS: Simple/Compound/Federated). Resolve
        // the composed instance config (its Sources are populated by the provider's Get(name)), then
        // dispatch on the AUTHORED type discriminator (config.ServiceOptionType) to the matching
        // strategy. Fail loud at every missing input — no fallback.
        var cfgResult = await _dataSetProvider.Value.Get(target.DataSet, cancellationToken).ConfigureAwait(false);
        if (!cfgResult.IsSuccess)
            // Why: the provider already logged the specific failure; propagate its messages rather
            // than re-logging with a ?? fallback literal (NO FALLBACKS rule).
            return cfgResult.ToNewResult<T>();
        if (cfgResult.Value is null)
            return GenericResult<T>.Failure(
                DataGatewayLogger.DataSetNotFound(_logger, target.DataSet, "configuration provider returned a null value for a successful result"));

        var config = cfgResult.Value;

        // Why: the strategies resolve containers/connections through IDataStoreProvider; in the
        // bootstrap/test paths it is null. Fail loud rather than NRE — no silent skip.
        if (_dataStoreProvider is null)
            return GenericResult<T>.Failure(
                DataGatewayLogger.DataSetNotFound(_logger, target.DataSet, "IDataStoreProvider is not available; cannot execute a DataSet"));

        if (string.IsNullOrWhiteSpace(config.ServiceOptionType))
            return GenericResult<T>.Failure(
                DataGatewayLogger.DataSetNotFound(_logger, target.DataSet, "DataSet configuration has no ServiceOptionType (strategy kind)"));

        var strategy = DataSetTypes.ByName(config.ServiceOptionType);
        if (ReferenceEquals(strategy, DataSetTypes.NotFound))
            return GenericResult<T>.Failure(
                DataGatewayLogger.DataSetNotFound(_logger, target.DataSet, $"DataSet strategy type '{config.ServiceOptionType}' is not a registered DataSetTypes member"));

        DataGatewayLogger.RoutingToDataSet(_logger, target.DataSet);
        var context = new DataSetExecutionContext(
            config, _connectionProvider, _dataStoreProvider, _predicatePushdown, _logger);
        return await strategy.Execute<T>(context, command, cancellationToken).ConfigureAwait(false);
    }

    // Why: ExecuteCascadeSave was removed in Wave C5. ParentTableName was deleted from
    // IConfigurationType (which is also deleted) in FDW-395 Phase 6. All ConfigurationSaveCommands
    // follow the flat single-table path. IDataNode owns parent-child structure for future cascade work.


    // Why: container resolution is on demand through ConfigurationGatewayDataStoreProvider.GetContainer —
    // it builds the one unified container (IDataContainer) from ConfigurationDb via the cached,
    // tag-invalidated gateway, replacing the deleted eager full-tree singleton. The provider's 3-arg
    // overload itself falls back to the path-agnostic scan when target.Path is empty, so addressing
    // stays explicit and complete.
    // Why (FDW-583): the previous nullable-returning ResolveContainer discarded containerResult's
    // IGenericMessage on failure (returning null), so both call sites below re-derived a generic
    // "Container not found in configuration" that never named the real cause. This overload propagates
    // the provider's own result — which, after the addressed-lookup logging added to
    // ConfigurationGatewayDataStoreProvider.Get(store, path, container, ct), already names the missing
    // store/path/container — straight to the caller.
    private async Task<IGenericResult<IDataContainer>> ResolveContainerResult(DataStoreTarget target, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(target.Container))
            return GenericResult<IDataContainer>.Failure(DataGatewayLogger.ContainerNotFound(_logger, target.Container));

        if (string.IsNullOrWhiteSpace(target.DataStore))
            return GenericResult<IDataContainer>.Failure(DataServiceResultCodes.ByName("DataStoreNameRequired"));

        if (_dataStoreProvider is null)
            return GenericResult<IDataContainer>.Failure(DataGatewayLogger.DataStoreProviderUnavailable(_logger));

        var containerResult = await _dataStoreProvider
            .Get(target.DataStore, target.Path ?? string.Empty, target.Container, ct)
            .ConfigureAwait(false);

        if (!containerResult.IsSuccess)
            return containerResult.ToNewResult<IDataContainer>();
        if (containerResult.Value is null)
            return GenericResult<IDataContainer>.Failure(DataGatewayLogger.ContainerNotFound(_logger, target.Container));

        return GenericResult<IDataContainer>.Success(containerResult.Value);
    }

}
