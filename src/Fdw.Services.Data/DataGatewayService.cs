using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Dynamic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Transactions;
using Fdw.Abstractions;
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
using Fdw.Services.Abstractions;
using Fdw.Services.Authentication.Abstractions.Security;
using Fdw.Services.Connections;
using Fdw.Services.Connections.Abstractions;
using Fdw.Services.Connections.Abstractions.Logging;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Data.Caching;
using Fdw.Services.Data.Configuration;
using Fdw.Services.Data.Execution;
using Fdw.Services.Data.Limits;
using Fdw.Services.Data.Logging;
using Fdw.Services.Data.Results;
using Fdw.Services.Data.Transformations;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Fdw.Services.Data;

/// <summary>
/// Default implementation of the DataGateway service. Routes commands to the appropriate
/// connection based on ConnectionName, enforces per-connection outbound limits before
/// dispatching, and manages container metadata and dataset federation.
/// </summary>
/// <remarks>
/// Limit kinds checked, in order, before a <see cref="DataStoreTarget"/> command dispatches:
///   1. RateLimit (token bucket, in-memory)
///   2. ConcurrencyLimit (SemaphoreSlim per connection; a slot is taken without blocking and held
///      until the call finishes, or the cursor is disposed, so a full connection refuses at once)
///   3. MaxResultSize (cap on Paging.Take if command is IQueryCommand)
///   4. QueryTimeout (linked CancellationTokenSource wrapping the caller's)
///   5. DailyBudget (in-memory counter, checked before dispatch, incremented after success)
/// On exceeded: returns GenericResult.Failure with the matching MessageLogging result. Never
/// throws, never blocks (non-blocking semaphore TryEnter).
/// <see cref="OpenRecordSource(IDataCommand, DataStoreTarget, CancellationToken)"/> is held to the same
/// limits (FDW-947): they are checked before the cursor opens, the daily counters advance when it
/// opens, and the concurrency slot and the query timeout stay in force until the caller disposes the
/// cursor. A limit that cannot be resolved refuses the call rather than lifting the limits (FDW-949).
/// </remarks>
[ExcludeFromCodeCoverage]
public sealed partial class DataGatewayService : IDataGateway
{
    private readonly ILogger<DataGatewayService> _logger;
    private readonly IDataConnectionProvider _connectionProvider;
    private readonly IDataSetConfigurationProvider _dataSetProvider;
    private readonly IDomainConfigurationProvider<IDataStoreImplementationConfiguration> _dataStoreConfigProvider;
    private readonly IDataStoreProvider? _dataStoreProvider;
    private readonly PredicatePushdownAnalyzer _predicatePushdown;
    private readonly IFrameworkAuthorizationService? _authorizationService;

    private readonly IAuthenticationContextAccessor? _authenticationContextAccessor;
    private readonly IConfigurationGatewayProvider? _configurationGateways;

    private readonly IConnectionConfigurationProvider? _connectionConfigProvider;
    private readonly FieldMappingTransformationResolver _fieldTransformations;
    private readonly DataGatewayResultCache? _cache;
    private readonly MainDataGatewayConfiguration? _options;

    private readonly IConnectionLimitResolver _limitResolver;
    private readonly ConnectionLimitCounterStore _counters;

    private readonly ConcurrentDictionary<string, TokenBucket> _tokenBuckets =
        new(StringComparer.OrdinalIgnoreCase);

    private readonly ConcurrentDictionary<string, SemaphoreSlim> _concurrencyGates =
        new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Initializes a new instance of the <see cref="DataGatewayService"/> class.
    /// </summary>
    /// <param name="loggerFactory">The logger factory for creating loggers.</param>
    /// <param name="connectionProvider">The data connection provider.</param>
    /// <param name="dataSetProvider">The dataset configuration provider for federated queries (lazy to break circular DI).</param>
    /// <param name="dataStoreConfigProvider">The DataStore configuration provider (dual-source).</param>
    /// <param name="limitResolver">Resolves the effective outbound limits for a connection.</param>
    /// <param name="counters">Backing store for the daily query counter limits are checked against.</param>
    /// <param name="fieldTransformations">Resolves the transformation chains attached to a data-set source's field mappings (FDW-877).</param>
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
    /// <param name="configurationGateways">
    /// The configuration gateways, through which the system context reaches a store the host declared
    /// (<see cref="IDataStore.DeclaredByHost"/>). Without it such a request fails loud; a non-system request over one
    /// is refused either way (FDW-1068).
    /// </param>
    public DataGatewayService(
        ILoggerFactory? loggerFactory,
        IDataConnectionProvider connectionProvider,
        IDataSetConfigurationProvider dataSetProvider,
        IDomainConfigurationProvider<IDataStoreImplementationConfiguration> dataStoreConfigProvider,
        IConnectionLimitResolver limitResolver,
        ConnectionLimitCounterStore counters,
        FieldMappingTransformationResolver fieldTransformations,
        IFrameworkAuthorizationService? authorizationService = null,
        IDataStoreProvider? dataStoreProvider = null,
        DataGatewayResultCache? cache = null,
        MainDataGatewayConfiguration? options = null,
        IAuthenticationContextAccessor? authenticationContextAccessor = null,
        IConnectionConfigurationProvider? connectionConfigProvider = null,
        IConfigurationGatewayProvider? configurationGateways = null)
    {
        var factory = loggerFactory ?? NullLoggerFactory.Instance;
        _logger = factory.CreateLogger<DataGatewayService>();

        _connectionProvider = connectionProvider;
        _dataSetProvider = dataSetProvider;
        _dataStoreConfigProvider = dataStoreConfigProvider;
        _limitResolver = limitResolver;
        _counters = counters;
        _authorizationService = authorizationService;
        _dataStoreProvider = dataStoreProvider;
        _cache = cache;
        _options = options;
        _authenticationContextAccessor = authenticationContextAccessor;
        _connectionConfigProvider = connectionConfigProvider;
        _configurationGateways = configurationGateways;
        _fieldTransformations = fieldTransformations ?? throw new ArgumentNullException(nameof(fieldTransformations));

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
        => Execute<T>(command, target, useCache: true, cancellationToken);

    /// <inheritdoc/>
    /// <remarks>
    /// A target in a store the host declared (<see cref="IDataStore.DeclaredByHost"/>) is not run here: a non-system
    /// caller is refused, and the system context is sent to the store's configuration gateway (FDW-1068).
    /// </remarks>
    public async Task<IGenericResult<T>> Execute<T>(IDataCommand command, DataStoreTarget target, bool useCache, CancellationToken cancellationToken = default)
    {
        if (await IsHostDeclared(target.DataStore, cancellationToken).ConfigureAwait(false))
            return await ExecuteOnHostStore<T>(command, target, cancellationToken).ConfigureAwait(false);

        return await ExecuteAdmitted(
            command, target.DataStore, ct => ExecuteWithCache<T>(command, target, useCache, ct), cancellationToken).ConfigureAwait(false);
    }

    // Runs one command's work under the limits of the store it targets: a data command and a vectorized one alike, so a
    // connection's limits hold whatever is sent to it.
    private async Task<IGenericResult<T>> ExecuteAdmitted<T>(
        IGenericCommand command, string dataStore, Func<CancellationToken, Task<IGenericResult<T>>> run, CancellationToken cancellationToken)
    {
        if (cancellationToken.IsCancellationRequested)
            return GenericResult<T>.Failure(ConnectionLimitLog.OperationCancelled(_logger, Guid.Empty));

        var admitted = Admit(dataStore, command, cancellationToken);
        if (!admitted.IsSuccess)
            return admitted.ToNewResult<T>();

        // Held until this call returns: a concurrency slot only limits anything while work occupies it.
        using var lease = admitted.Value!;
        var limits = lease.Limits;
        if (limits.Count == 0)
            return await run(cancellationToken).ConfigureAwait(false);

        var effectiveCt = ApplyQueryTimeout(limits, cancellationToken, out var timeoutCts);

        try
        {
            var result = await run(effectiveCt).ConfigureAwait(false);

            if (result.IsSuccess)
                IncrementDailyCounters(limits);

            return result;
        }
        catch (OperationCanceledException ex) when (timeoutCts?.IsCancellationRequested == true && ex is not null)
        {
            int timeoutSecs = FindQueryTimeoutSeconds(limits)!.Value;
            return GenericResult<T>.Failure(ConnectionLimitLog.QueryTimeoutExceeded(_logger, Guid.Empty, timeoutSecs));
        }
        catch (OperationCanceledException ex) when (ex is not null)
        {
            return GenericResult<T>.Failure(ConnectionLimitLog.OperationCancelled(_logger, Guid.Empty));
        }
        finally
        {
            timeoutCts?.Dispose();
        }
    }

    private async Task<IGenericResult<T>> ExecuteWithCache<T>(IDataCommand command, DataStoreTarget target, bool useCache, CancellationToken cancellationToken)
    {
        bool isQuery = command is IQueryCommand;

        bool cacheEnabled = CacheEnabled && CachePolicy.IsEnabled(command) && isQuery;

        string? cacheKey = null;
        var cacheCeiling = TimeSpan.MaxValue;
        if (cacheEnabled)
        {
            var connectionTypeResult = await ResolveConnectionType(target, cancellationToken).ConfigureAwait(false);
            if (!connectionTypeResult.IsSuccess || connectionTypeResult.Value is null)
                return connectionTypeResult.ToNewResult<T>();

            var partition = connectionTypeResult.Value.CachePartition(_authenticationContextAccessor?.Current);
            cacheCeiling = connectionTypeResult.Value.MaxCacheDuration(_authenticationContextAccessor?.Current);

            try
            {
                cacheKey = string.Concat(
                    partition, "|",
                    CacheKeyBuilder.ComputeCacheKey(command, target), ":", typeof(T).FullName);
            }
            catch (Exception ex)
            {
                return GenericResult<T>.Failure(
                    DataGatewayCacheLog.KeyComputationFailed(_logger, command.CommandType, target.Container, ex.Message));
            }

            if (useCache && _cache!.TryGet<T>(cacheKey, out var cached) && cached is not null)
                return cached;
        }

        // Concurrent misses for one key are one read: the call that leads reads and caches, the others wait
        // for it. A forced refresh (useCache false) reads for itself.
        if (cacheKey is not null && useCache)
        {
            return await _cache!.Coalesce(
                cacheKey,
                () => ReadAndCache<T>(command, target, isQuery, cacheKey, cacheCeiling, cancellationToken),
                cancellationToken).ConfigureAwait(false);
        }

        return await ReadAndCache<T>(command, target, isQuery, cacheKey, cacheCeiling, cancellationToken).ConfigureAwait(false);
    }

    private async Task<IGenericResult<T>> ReadAndCache<T>(
        IDataCommand command,
        DataStoreTarget target,
        bool isQuery,
        string? cacheKey,
        TimeSpan cacheCeiling,
        CancellationToken cancellationToken)
    {
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
                    CachePolicy.GetDuration(command, cacheCeiling));
            }

            return;
        }

        _cache!.InvalidateByTags(CacheKeyBuilder.GetInvalidationTags(command, target));
    }

    private async Task<IGenericResult<T>> ExecuteCore<T>(IDataCommand command, DataStoreTarget target, CancellationToken cancellationToken)
    {
        var resolved = await ResolveTargetConnection(target, cancellationToken).ConfigureAwait(false);
        if (!resolved.IsSuccess)
            return resolved.ToNewResult<T>();

        var (dataContainer, connection) = resolved.Value;
        return await RunOnContainer(command, target, ct => connection.Execute<T>(command, dataContainer, ct), cancellationToken).ConfigureAwait(false);
    }

    // The container a target names and the connection its store reaches it by: what every command run on a store needs
    // before it runs, a data command and a vectorized one alike.
    private async Task<IGenericResult<(IDataContainer Container, IDataConnection Connection)>> ResolveTargetConnection(
        DataStoreTarget target, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(target.DataStore))
            return GenericResult<(IDataContainer, IDataConnection)>.Failure(DataServiceResultCodes.ByName("DataStoreNameRequired"));

        DataGatewayLogger.ExecuteContainerEntering(_logger, target.DataStore, target.Path, target.Container);

        var containerResult = await ResolveContainerResult(target, cancellationToken).ConfigureAwait(false);
        if (!containerResult.IsSuccess)
            return containerResult.ToNewResult<(IDataContainer, IDataConnection)>();

        var dataContainer = containerResult.Value;
        if (dataContainer is null)
            return GenericResult<(IDataContainer, IDataConnection)>.Failure(DataGatewayLogger.ContainerNotFound(_logger, target.Container));

        var connectionResult = await ResolveStoreConnection(target.DataStore, cancellationToken).ConfigureAwait(false);
        if (!connectionResult.IsSuccess || connectionResult.Value is null)
            return connectionResult.ToNewResult<(IDataContainer, IDataConnection)>();

        return GenericResult<(IDataContainer, IDataConnection)>.Success((dataContainer, connectionResult.Value));
    }

    // The connection a data store reaches its data by: what a command needs whether or not it names a container, a scripted
    // command naming none.
    private async Task<IGenericResult<IDataConnection>> ResolveStoreConnection(string dataStore, CancellationToken cancellationToken)
    {
        // The store's row alone: ConnectionId is a field of it. Reading it whole composed every path,
        // container, field and key under it, a query per collection per row, on every command (FDW-970).
        var storeResult = await _dataStoreConfigProvider.Get(dataStore, ConfigurationLevels.RecordOnly, cancellationToken).ConfigureAwait(false);
        if (!storeResult.IsSuccess || storeResult.Value is null)
        {
            return GenericResult<IDataConnection>.Failure(
                DataGatewayLogger.DataStoreNotFoundForSource(_logger, dataStore));
        }

        var store = storeResult.Value;
        if (store.ConnectionId == Guid.Empty)
        {
            return GenericResult<IDataConnection>.Failure(
                DataGatewayLogger.DataStoreHasNoConnectionId(_logger, dataStore));
        }

        var connectionResult = await _connectionProvider.Get(store.ConnectionId, cancellationToken).ConfigureAwait(false);
        if (!connectionResult.IsSuccess || connectionResult.Value == null)
        {
            var reason = connectionResult.CurrentMessage ?? "Unknown error";
            return GenericResult<IDataConnection>.Failure(
                DataGatewayLogger.DataStoreConnectionNotResolved(_logger, dataStore, store.ConnectionId.ToString(), reason));
        }

        return GenericResult<IDataConnection>.Success(connectionResult.Value);
    }

    // Runs one command's work on the container, timed and logged the same for every command.
    private async Task<IGenericResult<T>> RunOnContainer<T>(
        IGenericCommand command, DataStoreTarget target, Func<CancellationToken, Task<IGenericResult<T>>> run, CancellationToken cancellationToken)
    {
        DataGatewayLogger.ExecutingContainerCommand(_logger, command.GetType().Name, target.Container, target.DataStore);

        var stopwatch = Stopwatch.StartNew();
        var result = await run(cancellationToken).ConfigureAwait(false);
        stopwatch.Stop();

        if (result.IsSuccess)
            DataGatewayLogger.ContainerCommandCompleted(_logger, target.Container, stopwatch.Elapsed.TotalMilliseconds);
        else
            DataGatewayLogger.ContainerCommandFailed(_logger, target.Container, result.CurrentMessage ?? "Unknown error");

        return result;
    }

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

        // Both rows alone: the partition needs the store's ConnectionId and the connection's
        // Implementation, and this runs before every cached command's cache is even checked (FDW-970).
        var storeResult = await _dataStoreConfigProvider.Get(target.DataStore, ConfigurationLevels.RecordOnly, cancellationToken).ConfigureAwait(false);
        if (!storeResult.IsSuccess || storeResult.Value is null)
        {
            return GenericResult<IConnectionType>.Failure(
                DataGatewayCacheLog.CachePartitionUnavailable(
                    _logger, target.DataStore, "the DataStore could not be resolved"));
        }

        var connectionResult = await _connectionConfigProvider
            .Get(storeResult.Value.ConnectionId, ConfigurationLevels.RecordOnly, cancellationToken).ConfigureAwait(false);
        if (!connectionResult.IsSuccess || connectionResult.Value is null)
        {
            return GenericResult<IConnectionType>.Failure(
                DataGatewayCacheLog.CachePartitionUnavailable(
                    _logger, target.DataStore, "the DataStore's connection configuration could not be resolved"));
        }

        if (string.IsNullOrWhiteSpace(connectionResult.Value.Implementation))
        {
            return GenericResult<IConnectionType>.Failure(
                DataGatewayCacheLog.CachePartitionUnavailable(
                    _logger, target.DataStore, "the connection declares no Implementation"));
        }

        if (ReferenceEquals(ConnectionTypes.ByName(connectionResult.Value.Implementation), ConnectionTypes.NotFound))
        {
            return GenericResult<IConnectionType>.Failure(
                DataGatewayCacheLog.CachePartitionUnavailable(
                    _logger,
                    target.DataStore,
                    $"connection type '{connectionResult.Value.Implementation}' is not registered"));
        }

        return GenericResult<IConnectionType>.Success(
            ConnectionTypes.ByName(connectionResult.Value.Implementation));
    }

    /// <inheritdoc/>
    public async Task<IGenericResult<IRecordSource<DataRecord>>> OpenRecordSource(
        IDataCommand command, DataStoreTarget target, CancellationToken cancellationToken = default)
    {
        if (cancellationToken.IsCancellationRequested)
            return GenericResult<IRecordSource<DataRecord>>.Failure(ConnectionLimitLog.OperationCancelled(_logger, Guid.Empty));

        if (await IsHostDeclared(target.DataStore, cancellationToken).ConfigureAwait(false))
            return await OpenOnHostStore(command, target, cancellationToken).ConfigureAwait(false);

        var admitted = Admit(target.DataStore, command, cancellationToken);
        if (!admitted.IsSuccess)
            return admitted.ToNewResult<IRecordSource<DataRecord>>();

        return await OpenWithinLimits(admitted.Value!, command, target, cancellationToken).ConfigureAwait(false);
    }

    // What the limits hold has to outlive this call: the connection returns once the cursor is open and
    // the rows are read afterwards. So on success the lease (concurrency slots) and the timeout source are
    // handed to the returned source, which keeps them until the caller disposes the cursor; on any other
    // exit they are released here. A connection with nothing to keep enforcing gets its cursor back as is.
    private async Task<IGenericResult<IRecordSource<DataRecord>>> OpenWithinLimits(
        ConnectionLimitLease lease,
        IDataCommand command,
        DataStoreTarget target,
        CancellationToken cancellationToken)
    {
        var limits = lease.Limits;
        var timeoutSeconds = FindQueryTimeoutSeconds(limits);
        var effectiveCt = ApplyQueryTimeout(limits, cancellationToken, out var timeoutCts);
        var handedOver = false;

        try
        {
            var opened = await OpenRecordSourceCore(command, target, effectiveCt).ConfigureAwait(false);
            if (!opened.IsSuccess)
                return opened;

            IncrementDailyCounters(limits);

            if (timeoutCts is null && !lease.HoldsSlots)
                return opened;

            handedOver = true;
            return GenericResult<IRecordSource<DataRecord>>.Success(
                new LimitedRecordSource(opened.Value!, lease, timeoutCts, timeoutSeconds.GetValueOrDefault(), _logger));
        }
        catch (OperationCanceledException ex) when (timeoutCts?.IsCancellationRequested == true && ex is not null)
        {
            return GenericResult<IRecordSource<DataRecord>>.Failure(
                ConnectionLimitLog.QueryTimeoutExceeded(_logger, Guid.Empty, timeoutSeconds.GetValueOrDefault()));
        }
        catch (OperationCanceledException ex) when (ex is not null)
        {
            return GenericResult<IRecordSource<DataRecord>>.Failure(
                ConnectionLimitLog.OperationCancelled(_logger, Guid.Empty));
        }
        finally
        {
            if (!handedOver)
            {
                timeoutCts?.Dispose();
                lease.Dispose();
            }
        }
    }

    private async Task<IGenericResult<IRecordSource<DataRecord>>> OpenRecordSourceCore(
        IDataCommand command, DataStoreTarget target, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(target.DataStore))
            return GenericResult<IRecordSource<DataRecord>>.Failure(DataServiceResultCodes.ByName("DataStoreNameRequired"));

        DataGatewayLogger.OpeningRecordSource(_logger, target.DataStore, target.Container);

        var containerResult = await ResolveContainerResult(target, cancellationToken).ConfigureAwait(false);
        if (!containerResult.IsSuccess)
            return containerResult.ToNewResult<IRecordSource<DataRecord>>();

        var dataContainer = containerResult.Value;
        if (dataContainer is null)
            return GenericResult<IRecordSource<DataRecord>>.Failure(DataGatewayLogger.ContainerNotFound(_logger, target.Container));

        // The store's row alone, for its ConnectionId, as in ExecuteCore.
        var storeResult = await _dataStoreConfigProvider.Get(target.DataStore, ConfigurationLevels.RecordOnly, cancellationToken).ConfigureAwait(false);
        if (!storeResult.IsSuccess || storeResult.Value is null)
            return GenericResult<IRecordSource<DataRecord>>.Failure(DataGatewayLogger.DataStoreNotFoundForSource(_logger, target.DataStore));

        var store = storeResult.Value;
        if (store.ConnectionId == Guid.Empty)
            return GenericResult<IRecordSource<DataRecord>>.Failure(DataGatewayLogger.DataStoreHasNoConnectionId(_logger, target.DataStore));

        var connectionResult = await _connectionProvider.Get(store.ConnectionId, cancellationToken).ConfigureAwait(false);
        if (!connectionResult.IsSuccess || connectionResult.Value == null)
            return GenericResult<IRecordSource<DataRecord>>.Failure(
                DataGatewayLogger.DataStoreConnectionNotResolved(_logger, target.DataStore, store.ConnectionId.ToString(), connectionResult.CurrentMessage ?? "Unknown error"));

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
        var cfgResult = await _dataSetProvider.Get(target.DataSet, cancellationToken).ConfigureAwait(false);
        if (!cfgResult.IsSuccess)
            return cfgResult.ToNewResult<T>();
        if (cfgResult.Value is null)
            return GenericResult<T>.Failure(
                DataGatewayLogger.DataSetNotFound(_logger, target.DataSet, DataGatewayLogger.DataSetNotVisibleReason));

        var config = cfgResult.Value;

        if (_dataStoreProvider is null)
            return GenericResult<T>.Failure(
                DataGatewayLogger.DataSetNotFound(_logger, target.DataSet, "IDataStoreProvider is not available; cannot execute a DataSet"));

        var sourcesAdmitted = await AdmitSources(config, cancellationToken).ConfigureAwait(false);
        if (sourcesAdmitted.IsFailure)
            return sourcesAdmitted.ToNewResult<T>();

        if (string.IsNullOrWhiteSpace(config.Implementation))
            return GenericResult<T>.Failure(
                DataGatewayLogger.DataSetNotFound(_logger, target.DataSet, "DataSet configuration has no Implementation (strategy kind)"));

        var strategy = DataSetTypes.ByName(config.Implementation);
        if (ReferenceEquals(strategy, DataSetTypes.NotFound))
            return GenericResult<T>.Failure(
                DataGatewayLogger.DataSetNotFound(_logger, target.DataSet, $"DataSet strategy type '{config.Implementation}' is not a registered DataSetTypes member"));

        DataGatewayLogger.RoutingToDataSet(_logger, target.DataSet);
        var context = new DataSetExecutionContext(
            config, _connectionProvider, _dataStoreProvider, _predicatePushdown, _fieldTransformations, _logger);
        return await strategy.Execute<T>(context, command, cancellationToken).ConfigureAwait(false);
    }



    // ============================================================
    // Host-declared stores (FDW-1068)
    // ============================================================

    // Whether the store a target names is one the host declared in configurationSchema.json. Read off the store
    // itself (IDataStore.DeclaredByHost), which the configuration gateway that built it set; never off its name. A
    // name that resolves to no store is not a host store: the read goes on and fails where it always did.
    private async Task<bool> IsHostDeclared(string? dataStoreName, CancellationToken cancellationToken)
    {
        if (_dataStoreProvider is null || string.IsNullOrWhiteSpace(dataStoreName))
            return false;

        var store = await _dataStoreProvider.Get(dataStoreName, cancellationToken).ConfigureAwait(false);
        return store is { IsSuccess: true, Value.DeclaredByHost: true };
    }

    // A host store is the platform's own: a request is refused, and the system context reads it through the
    // configuration gateway onto it, which is the one path built for those stores. The host declares each such store
    // beside the connection of the same name that serves it, so that is the gateway asked for; a host that declares
    // none of that name gets the provider's refusal naming the ones it does declare.
    private IGenericResult<IConfigurationGateway> HostStoreGateway(string dataStoreName, string container)
    {
        if (_authenticationContextAccessor?.Current is not { IsSystemContext: true })
        {
            return GenericResult<IConfigurationGateway>.Failure(
                DataServiceResultCodes.ByName("HostDeclaredStoreRefused"), _logger,
                ResultDetails.Create("dataStore", dataStoreName, "container", container));
        }

        if (_configurationGateways is null)
        {
            return GenericResult<IConfigurationGateway>.Failure(
                DataServiceResultCodes.ByName("NoConfigurationGateway"), _logger,
                ResultDetails.Create("ConnectionName", dataStoreName, "Registered", "no configuration gateway provider was given to this data gateway"));
        }

        return _configurationGateways.Get(dataStoreName);
    }

    private async Task<IGenericResult<T>> ExecuteOnHostStore<T>(IDataCommand command, DataStoreTarget target, CancellationToken cancellationToken)
    {
        var gateway = HostStoreGateway(target.DataStore, target.Container);
        if (gateway.IsFailure)
            return gateway.ToNewResult<T>();

        return await gateway.Value!.Execute<T>(command, target, cancellationToken).ConfigureAwait(false);
    }

    private async Task<IGenericResult<IRecordSource<DataRecord>>> OpenOnHostStore(
        IDataCommand command, DataStoreTarget target, CancellationToken cancellationToken)
    {
        var gateway = HostStoreGateway(target.DataStore, target.Container);
        if (gateway.IsFailure)
            return gateway.ToNewResult<IRecordSource<DataRecord>>();

        return await gateway.Value!.OpenRecordSource(command, target, cancellationToken).ConfigureAwait(false);
    }

    // A data set over a host store would hand its rows to whoever may read the data set, so a non-system caller is
    // refused when any source sits in one. The system context runs the data set as it always has.
    private async Task<IGenericResult> AdmitSources(IDataSetImplementationConfiguration config, CancellationToken cancellationToken)
    {
        if (_authenticationContextAccessor?.Current is { IsSystemContext: true })
            return GenericResult.Success();

        foreach (var source in config.Sources)
        {
            if (await IsHostDeclared(source.DataStoreName, cancellationToken).ConfigureAwait(false))
            {
                return GenericResult.Failure(
                    DataServiceResultCodes.ByName("HostDeclaredStoreRefused"), _logger,
                    ResultDetails.Create("dataStore", source.DataStoreName, "container", source.ContainerName));
            }
        }

        return GenericResult.Success();
    }

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

    // The limit gate every DataStoreTarget read goes through -- Execute and OpenRecordSource both admit
    // here, so a streamed read is held to exactly the limits a buffered one is (FDW-947). On success the
    // lease carries the limits that applied and any concurrency slots taken, and the caller disposes it
    // when the work is finished. A limit that cannot be read is a refusal, not "no limit" (FDW-949); an
    // empty list is the normal state of a connection nobody has limited.
    private IGenericResult<ConnectionLimitLease> Admit(string connectionName, IGenericCommand command, CancellationToken cancellationToken)
    {
        var limitsResult = _limitResolver.Resolve(connectionName, cancellationToken);
        if (!limitsResult.IsSuccess)
        {
            return GenericResult<ConnectionLimitLease>.Failure(
                ConnectionLimitLog.LimitResolutionFailed(_logger, Guid.Empty, limitsResult.CurrentMessage ?? "unknown"));
        }

        var lease = new ConnectionLimitLease(limitsResult.Value!);
        if (lease.Limits.Count == 0)
        {
            ConnectionLimitLog.NoLimitsConfigured(_logger, Guid.Empty);
            return GenericResult<ConnectionLimitLease>.Success(lease);
        }

        foreach (var limit in lease.Limits)
        {
            var refusal = CheckLimit<ConnectionLimitLease>(limit, command, connectionName, lease);
            if (refusal is not null)
            {
                // Slots taken by an earlier limit, or an earlier check of this one, go back.
                lease.Dispose();
                return refusal;
            }
        }

        return GenericResult<ConnectionLimitLease>.Success(lease);
    }

    private IGenericResult<T>? CheckLimit<T>(
        ConnectionLimitConfiguration limit,
        IGenericCommand command,
        string connectionName,
        ConnectionLimitLease lease)
    {
        var baseCfg = limit;

        if (baseCfg.EnforceMaxPerSecond.HasValue)
        {
            var rateResult = CheckRateLimit<T>(baseCfg, connectionName);
            if (rateResult is not null)
                return rateResult;
        }

        if (baseCfg.EnforceMaxConcurrent.HasValue)
        {
            var concurrencyResult = CheckConcurrency<T>(baseCfg, connectionName, lease);
            if (concurrencyResult is not null)
                return concurrencyResult;
        }

        if (baseCfg.EnforceMaxRows.HasValue)
        {
            var sizeResult = CheckMaxResultSize<T>(baseCfg, command);
            if (sizeResult is not null)
                return sizeResult;
        }

        if (baseCfg.EnforceMaxQueriesPerDay.HasValue)
        {
            var budgetResult = CheckDailyBudget<T>(baseCfg);
            if (budgetResult is not null)
                return budgetResult;
        }

        return null;
    }

    private IGenericResult<T>? CheckRateLimit<T>(ConnectionLimitConfiguration limit, string connectionName)
    {
        int maxPerSecond = limit.EnforceMaxPerSecond!.Value;
        double burst = limit.EnforceBurstSize ?? maxPerSecond;
        var bucket = _tokenBuckets.GetOrAdd(connectionName,
            _ => new TokenBucket(maxPerSecond, burst));

        if (bucket.TryConsume())
            return null;

        double rate = maxPerSecond - bucket.CurrentTokens;
        return GenericResult<T>.Failure(
            ConnectionLimitLog.RateExceeded(_logger, limit.ConnectionConfigurationId, rate, maxPerSecond));
    }

    // Takes a slot and leaves it taken: the lease hands it back when the call finishes or the cursor is
    // disposed. This used to Wait(0) and Release() straight away, which never held a slot and so could
    // never refuse anything (FDW-949).
    private IGenericResult<T>? CheckConcurrency<T>(ConnectionLimitConfiguration limit, string connectionName, ConnectionLimitLease lease)
    {
        int maxConcurrent = limit.EnforceMaxConcurrent!.Value;
        var semaphore = _concurrencyGates.GetOrAdd(connectionName,
            _ => new SemaphoreSlim(maxConcurrent, maxConcurrent));

        if (!semaphore.Wait(0))
        {
            return GenericResult<T>.Failure(
                ConnectionLimitLog.ConcurrencyBlocked(
                    _logger,
                    limit.ConnectionConfigurationId,
                    maxConcurrent - semaphore.CurrentCount,
                    maxConcurrent));
        }

        lease.Hold(semaphore);
        return null;
    }

    private IGenericResult<T>? CheckMaxResultSize<T>(ConnectionLimitConfiguration limit, IGenericCommand command)
    {
        int maxRows = limit.EnforceMaxRows!.Value;

        // A nearest search asks for its rows by count, as a page does.
        if (command is INearestCommand nearest && nearest.Take > maxRows)
        {
            return GenericResult<T>.Failure(
                ConnectionLimitLog.MaxResultSizeExceeded(_logger, limit.ConnectionConfigurationId, nearest.Take, maxRows));
        }

        if (command is IQueryCommand qc && qc.Paging is not null)
        {
            int requested = qc.Paging.Take ?? int.MaxValue;
            if (requested > maxRows)
                return GenericResult<T>.Failure(
                    ConnectionLimitLog.MaxResultSizeExceeded(
                        _logger,
                        limit.ConnectionConfigurationId,
                        requested,
                        maxRows));
        }

        return null;
    }

    private IGenericResult<T>? CheckDailyBudget<T>(ConnectionLimitConfiguration limit)
    {
        var queries = _counters.ReadQueryCount(limit.ConnectionConfigurationId);

        if (limit.EnforceMaxQueriesPerDay.HasValue && queries >= limit.EnforceMaxQueriesPerDay.Value)
            return GenericResult<T>.Failure(
                ConnectionLimitLog.DailyQueryBudgetExhausted(
                    _logger,
                    limit.ConnectionConfigurationId,
                    queries,
                    limit.EnforceMaxQueriesPerDay.Value));

        return null;
    }

    private static CancellationToken ApplyQueryTimeout(
        IReadOnlyList<ConnectionLimitConfiguration> limits,
        CancellationToken callerCt,
        out CancellationTokenSource? timeoutCts)
    {
        int? timeoutSeconds = FindQueryTimeoutSeconds(limits);
        if (timeoutSeconds is null)
        {
            timeoutCts = null;
            return callerCt;
        }

        timeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(timeoutSeconds.Value));
        return CancellationTokenSource.CreateLinkedTokenSource(callerCt, timeoutCts.Token).Token;
    }

    private static int? FindQueryTimeoutSeconds(IReadOnlyList<ConnectionLimitConfiguration> limits)
    {
        foreach (var limit in limits)
        {
            if (limit.EnforceTimeoutSeconds.HasValue)
                return limit.EnforceTimeoutSeconds.Value;
        }

        return null;
    }

    private void IncrementDailyCounters(IReadOnlyList<ConnectionLimitConfiguration> limits)
    {
        foreach (var limit in limits)
        {
            if (limit.EnforceMaxQueriesPerDay.HasValue)
                _counters.IncrementQueryCount(limit.ConnectionConfigurationId);
        }
    }
    // Why these are explicit and refuse: a data gateway routes a command to a connection using an
    // address the caller supplies alongside it. IGenericService's command surface carries no address,
    // so there is no honest answer -- it fails loud rather than guessing a store.
    string IPlatformService.Id => "DataGateway";

    string IPlatformService.ServiceType => "DataGateway";

    bool IPlatformService.IsAvailable => true;

    Task<IGenericResult<T>> IGenericService.Execute<T>(IGenericCommand command, CancellationToken cancellationToken)
        => Task.FromResult(GenericResult<T>.Failure(
            DataGatewayProviderLog.CommandCarriesNoAddress(_logger)));

    Task<IGenericResult> IGenericService.Execute(IGenericCommand command, CancellationToken cancellationToken)
        => Task.FromResult<IGenericResult>(GenericResult.Failure(
            DataGatewayProviderLog.CommandCarriesNoAddress(_logger)));

}
