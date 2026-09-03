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

using Fdw.Services.Data.Configuration;
using Fdw.Abstractions;
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
    private readonly IDataSetConfigurationProvider _dataSetProvider;
    private readonly DataStoreConfigurationProvider _dataStoreConfigProvider;
    private readonly IDataStoreProvider? _dataStoreProvider;
    private readonly PredicatePushdownAnalyzer _predicatePushdown;
    private readonly IFrameworkAuthorizationService? _authorizationService;

    private readonly IAuthenticationContextAccessor? _authenticationContextAccessor;

    private readonly ConnectionConfigurationProvider? _connectionConfigProvider;
    private readonly DataGatewayResultCache? _cache;
    private readonly MainDataGatewayConfiguration? _options;

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
        IDataSetConfigurationProvider dataSetProvider,
        DataStoreConfigurationProvider dataStoreConfigProvider,
        IFrameworkAuthorizationService? authorizationService = null,
        IDataStoreProvider? dataStoreProvider = null,
        DataGatewayResultCache? cache = null,
        MainDataGatewayConfiguration? options = null,
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
        => Execute<T>(command, target, useCache: true, cancellationToken);

    /// <inheritdoc/>
    public async Task<IGenericResult<T>> Execute<T>(IDataCommand command, DataStoreTarget target, bool useCache, CancellationToken cancellationToken = default)
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
        if (string.IsNullOrWhiteSpace(target.DataStore))
            return GenericResult<T>.Failure(DataServiceResultCodes.ByName("DataStoreNameRequired"));

        DataGatewayLogger.ExecuteContainerEntering(_logger, target.DataStore, target.Path, target.Container);

        var containerResult = await ResolveContainerResult(target, cancellationToken).ConfigureAwait(false);
        if (!containerResult.IsSuccess)
            return containerResult.ToNewResult<T>();

        var dataContainer = containerResult.Value;
        if (dataContainer is null)
            return GenericResult<T>.Failure(DataGatewayLogger.ContainerNotFound(_logger, target.Container));

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
        if (string.IsNullOrWhiteSpace(target.DataStore))
            return GenericResult<IRecordSource<DataRecord>>.Failure(DataServiceResultCodes.ByName("DataStoreNameRequired"));

        DataGatewayLogger.OpeningRecordSource(_logger, target.DataStore, target.Container);

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
                DataGatewayLogger.DataSetNotFound(_logger, target.DataSet, "configuration provider returned a null value for a successful result"));

        var config = cfgResult.Value;

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
