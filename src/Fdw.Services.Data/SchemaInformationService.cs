using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Configuration;
using Fdw.Data.Abstractions;
using Fdw.Messages;
using Fdw.Results;
using Fdw.Services.Abstractions;
using Fdw.Services.Connections;
using Fdw.Services.Connections.Abstractions;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Data.Logging;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Services.Data;

/// <summary>
/// The one place a connection's schema is discovered, and the one place what discovery finds is
/// added to a data store.
/// </summary>
/// <remarks>
/// <para>
/// Every discovery runs the option the connection chose
/// (<see cref="IConnectionImplementationConfiguration.SchemaDiscovery"/>), after checking that its
/// connection type offers that option and performs it. A connection whose option is <c>None</c> is
/// refused rather than reported as an empty store.
/// </para>
/// <para>
/// GetSchema is the stored-first path: if a DataStore already exists for the connection, it is
/// returned as it is. Otherwise discovery runs and the store is saved before returning.
/// RefreshSchema always re-discovers, enabling the UI "Re-discover" action.
/// </para>
/// <para>
/// What discovery finds is merged into the store additively (<see cref="DiscoveredSchemaMerge"/>)
/// and the store is saved once, through the DataStore domain.
/// </para>
/// </remarks>
public sealed class SchemaInformationService : ISchemaInformationService
{
    private readonly IConnectionProvider _connectionProvider;
    private readonly ConnectionConfigurationProvider _configProvider;
    private readonly IDomainConfigurationProvider<IDataStoreImplementationConfiguration> _dataStoreProvider;
    private readonly ILogger<SchemaInformationService> _logger;

    /// <summary>
    /// Initializes a new instance of <see cref="SchemaInformationService"/>.
    /// </summary>
    /// <param name="connectionProvider">Opens the connection discovery reads through.</param>
    /// <param name="configProvider">Reads the connection's configuration.</param>
    /// <param name="dataStoreProvider">Reads and saves data stores.</param>
    /// <param name="logger">The logger.</param>
    public SchemaInformationService(
        IConnectionProvider connectionProvider,
        ConnectionConfigurationProvider configProvider,
        IDomainConfigurationProvider<IDataStoreImplementationConfiguration> dataStoreProvider,
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

        var config = await ResolveConnectionConfig(connectionName, cancellationToken).ConfigureAwait(false);
        if (config.IsFailure || config.Value is not { } connection)
            return config.ToNewResult<SchemaInformation>();

        // A store that already exists for the connection is its schema, whether discovery built it or
        // it was declared by hand; only a connection with no store has to discover.
        var stored = await FindStoreFor(connection, cancellationToken).ConfigureAwait(false);
        if (stored.IsFailure)
            return stored.ToNewResult<SchemaInformation>();

        if (stored.Value is { } dataStore)
        {
            SchemaInformationLog.CacheHit(_logger, connectionName, dataStore.Name);
            return GenericResult<SchemaInformation>.Success(new SchemaInformation(dataStore));
        }

        SchemaInformationLog.CacheMiss(_logger, connectionName);
        return await DiscoverAndPersist(connection, refresh: false, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async Task<IGenericResult<SchemaInformation>> RefreshSchema(
        string connectionName,
        CancellationToken cancellationToken = default)
    {
        SchemaInformationLog.RefreshStarted(_logger, connectionName);

        var config = await ResolveConnectionConfig(connectionName, cancellationToken).ConfigureAwait(false);
        if (config.IsFailure || config.Value is not { } connection)
            return config.ToNewResult<SchemaInformation>();

        return await DiscoverAndPersist(connection, refresh: true, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async Task<IGenericResult<IReadOnlyList<IStorageContainer>>> DiscoverContainers(
        string connectionName,
        DataStoreDiscoveryOptions options,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(options);

        var config = await ResolveConnectionConfig(connectionName, cancellationToken).ConfigureAwait(false);
        if (config.IsFailure || config.Value is not { } connection)
            return config.ToNewResult<IReadOnlyList<IStorageContainer>>();

        return await Discover(connection, options, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async Task<IGenericResult<DataStoreSchemaImportResult>> DiscoverInto(
        IDataStoreImplementationConfiguration store,
        DataStoreDiscoveryOptions options,
        bool refresh,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(store);
        ArgumentNullException.ThrowIfNull(options);

        var config = await _configProvider.Get(store.ConnectionId, cancellationToken).ConfigureAwait(false);
        if (config.IsFailure || config.Value is not { } connection)
        {
            return GenericResult<DataStoreSchemaImportResult>.Failure(
                SchemaInformationLog.ConnectionConfigNotFound(_logger, store.ConnectionId.ToString()));
        }

        return await DiscoverIntoStore(store, connection, options, refresh, cancellationToken).ConfigureAwait(false);
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

    // The store already built for this connection, or none. A failed read is a failure, not "none":
    // treating it as none would create a second store for the same connection.
    private async Task<IGenericResult<IDataStoreImplementationConfiguration?>> FindStoreFor(
        IConnectionImplementationConfiguration connection,
        CancellationToken cancellationToken)
    {
        var stores = await _dataStoreProvider.Get(cancellationToken).ConfigureAwait(false);
        if (!stores.IsSuccess)
            return stores.ToNewResult<IDataStoreImplementationConfiguration?>();

        return GenericResult<IDataStoreImplementationConfiguration?>.Success(
            stores.Value!.FirstOrDefault(ds => ds.ConnectionId == connection.Id));
    }

    private async Task<IGenericResult<SchemaInformation>> DiscoverAndPersist(
        IConnectionImplementationConfiguration connection,
        bool refresh,
        CancellationToken cancellationToken)
    {
        var store = await ResolveOrCreateDataStore(connection, cancellationToken).ConfigureAwait(false);
        if (store.IsFailure || store.Value is not { } target)
            return store.ToNewResult<SchemaInformation>();

        var merged = await DiscoverIntoStore(
            target, connection, DataStoreDiscoveryOptions.Default, refresh, cancellationToken).ConfigureAwait(false);
        if (merged.IsFailure || merged.Value is not { } added)
            return merged.ToNewResult<SchemaInformation>();

        SchemaDiscoveryLog.PersistingDataStore(_logger, target.Name, connection.Id);
        var saved = await _dataStoreProvider.Save(
            target, target.Domain, target.Implementation, target.Name, cancellationToken).ConfigureAwait(false);
        if (!saved.IsSuccess)
        {
            LogSaveFailure(target.Name, saved.CurrentMessage);
            return saved.ToNewResult<SchemaInformation>();
        }

        SchemaDiscoveryLog.PersistCompleted(_logger, target.Name, added.PathsAdded, added.ContainersAdded, added.FieldsAdded);

        var reloaded = await _dataStoreProvider.Get(target.Name, cancellationToken).ConfigureAwait(false);
        if (!reloaded.IsSuccess || reloaded.Value is null)
        {
            return GenericResult<SchemaInformation>.Failure(
                SchemaInformationLog.DataStoreNotFoundAfterDiscovery(_logger, connection.Name));
        }

        SchemaInformationLog.DiscoverySucceeded(_logger, connection.Name, reloaded.Value.Name);
        return GenericResult<SchemaInformation>.Success(new SchemaInformation(reloaded.Value));
    }

    private void LogSaveFailure(string dataStoreName, string? upstreamError)
    {
        if (upstreamError is not null)
            SchemaDiscoveryLog.PersistFailed(_logger, dataStoreName, upstreamError);
        else
            SchemaDiscoveryLog.DataStoreSaveFailed(_logger, dataStoreName);
    }

    private async Task<IGenericResult<IDataStoreImplementationConfiguration>> ResolveOrCreateDataStore(
        IConnectionImplementationConfiguration connection,
        CancellationToken cancellationToken)
    {
        var found = await FindStoreFor(connection, cancellationToken).ConfigureAwait(false);
        if (found.IsFailure)
            return found.ToNewResult<IDataStoreImplementationConfiguration>();

        if (found.Value is { } existing)
        {
            SchemaDiscoveryLog.ExistingDataStoreFound(_logger, existing.Name, existing.Id);
            return GenericResult<IDataStoreImplementationConfiguration>.Success(existing);
        }

        // Why these: the first save writes the domain row from them. The store is named for its
        // connection and is the kind the connection is.
        return GenericResult<IDataStoreImplementationConfiguration>.Success(new DataStoreImplementationConfiguration
        {
            Name = connection.Name,
            Domain = "DataStore",
            Implementation = connection.Implementation,
            ConnectionId = connection.Id,
        });
    }

    private async Task<IGenericResult<DataStoreSchemaImportResult>> DiscoverIntoStore(
        IDataStoreImplementationConfiguration store,
        IConnectionImplementationConfiguration connection,
        DataStoreDiscoveryOptions options,
        bool refresh,
        CancellationToken cancellationToken)
    {
        var discovered = await Discover(connection, options, cancellationToken).ConfigureAwait(false);
        if (discovered.IsFailure || discovered.Value is not { } containers)
            return discovered.ToNewResult<DataStoreSchemaImportResult>();

        if (containers.Count == 0)
            SchemaInformationLog.NothingDiscovered(_logger, connection.Name);

        var merged = DiscoveredSchemaMerge.Merge(store, containers, refresh);

        var discoveredAt = DateTimeOffset.UtcNow;
        store.LastDiscoveredAt = discoveredAt;
        SchemaDiscoveryLog.LastDiscoveredAtUpdated(_logger, store.Name, discoveredAt);

        SchemaInformationLog.SchemaMerged(
            _logger, store.Name, merged.ContainersDiscovered, merged.PathsAdded, merged.ContainersAdded, merged.FieldsAdded, merged.FieldsUpdated);
        return GenericResult<DataStoreSchemaImportResult>.Success(merged);
    }

    // The one discovery every path runs: the connection's chosen option, checked against its kind,
    // then the kind's own discovery through an open connection.
    private async Task<IGenericResult<IReadOnlyList<IStorageContainer>>> Discover(
        IConnectionImplementationConfiguration connection,
        DataStoreDiscoveryOptions options,
        CancellationToken cancellationToken)
    {
        var chosen = DiscoveryFor(connection);
        if (chosen.IsFailure)
            return chosen.ToNewResult<IReadOnlyList<IStorageContainer>>();

        var (discovery, option) = chosen.Value;

        var live = await _connectionProvider.Get(connection.Name, cancellationToken).ConfigureAwait(false);
        if (!live.IsSuccess || live.Value is null)
        {
            return GenericResult<IReadOnlyList<IStorageContainer>>.Failure(
                live.CurrentMessage is { } buildError
                    ? SchemaInformationLog.ConnectionBuildFailed(_logger, connection.Name, buildError)
                    : SchemaInformationLog.ConnectionBuildFailedNoDetails(_logger, connection.Name));
        }

        SchemaInformationLog.DiscoveryStarting(_logger, connection.Name, connection.Implementation);
        var discovered = await discovery.DiscoverSchema(live.Value, option, options, cancellationToken).ConfigureAwait(false);
        if (!discovered.IsSuccess || discovered.Value is null)
        {
            return GenericResult<IReadOnlyList<IStorageContainer>>.Failure(
                discovered.CurrentMessage is { } discoveryError
                    ? SchemaInformationLog.DiscoveryFailed(_logger, connection.Name, discoveryError)
                    : SchemaInformationLog.DiscoveryFailedNoDetails(_logger, connection.Name));
        }

        return discovered;
    }

    // What the connection chose, and that its kind both offers it and performs it. Each refusal says
    // which of the three is missing; none of them is reported as an empty store.
    private IGenericResult<(ISchemaDiscovery Discovery, ISchemaDiscoveryType Option)> DiscoveryFor(
        IConnectionImplementationConfiguration connection)
    {
        var connectionType = ConnectionTypes.ByName(connection.Implementation);
        if (ReferenceEquals(connectionType, ConnectionTypes.NotFound))
            return Refuse(SchemaInformationLog.ConnectionTypeUnknown(_logger, connection.Name, connection.Implementation));

        var option = SchemaDiscoveryTypes.ByName(connection.SchemaDiscovery);
        if (ReferenceEquals(option, SchemaDiscoveryTypes.NotFound))
            return Refuse(SchemaInformationLog.DiscoveryOptionUnknown(_logger, connection.Name, connection.SchemaDiscovery));

        if (!connectionType.SupportedDiscoveryTypes.Contains(option))
        {
            return Refuse(SchemaInformationLog.DiscoveryOptionNotOffered(
                _logger, connection.Name, option.Name, connection.Implementation));
        }

        if (!option.DiscoversSchema)
            return Refuse(SchemaInformationLog.ConnectionDoesNotDiscover(_logger, connection.Name, option.Name));

        if (connectionType is not ISchemaDiscovery discovery)
        {
            return Refuse(SchemaInformationLog.ConnectionTypeNotDiscoverable(
                _logger, connection.Name, connection.Implementation, option.Name));
        }

        SchemaInformationLog.DiscoveryOptionResolved(_logger, connection.Name, connection.Implementation, option.Name);
        return GenericResult<(ISchemaDiscovery Discovery, ISchemaDiscoveryType Option)>.Success((discovery, option));
    }

    private static IGenericResult<(ISchemaDiscovery Discovery, ISchemaDiscoveryType Option)> Refuse(IGenericMessage message)
        => GenericResult<(ISchemaDiscovery Discovery, ISchemaDiscoveryType Option)>.Failure(message);
}
