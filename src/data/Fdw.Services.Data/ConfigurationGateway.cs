using System;
using Fdw.Services.Abstractions;
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
public sealed partial class ConfigurationGateway : IConfigurationGateway, IDerivedConfigurationCache, IDatabaseGenerationSource
{
    /// <inheritdoc />
    public string ConnectionName { get; }
    private const string ConnectionTypeMsSql = "MsSql";
    private const string ConnectionTypePostgreSql = "PostgreSql";

    private readonly ILogger<ConfigurationGateway> _logger;
    // The same implementation provider every other caller reaches this connection type through.
    // The gateway does not resolve secrets itself: that is the provider's job, here as everywhere.
    // The same implementation provider every other caller reaches this connection type through.
    // The gateway does not resolve secrets itself: that is the provider's job, here as everywhere.
    private readonly IImplementationServiceProvider<IGenericConnection, IConnectionImplementationConfiguration> _connectionProvider;
    private readonly ConfigurationSchema _schema;


    private readonly Lazy<IReadOnlyList<IDataStore>> _dataStores;

    private readonly DataGatewayResultCache? _cache;
    private readonly MainDataGatewayConfiguration? _options;

    private readonly IAuthenticationContextAccessor? _authenticationContextAccessor;

    private readonly Lazy<IGenericResult<IConnectionType>> _connectionTypeLazy;

    // The reads of the derived-value build running on this async flow, or null when none is. Per flow and not
    // a field of the gateway, which every request shares: a read by another request while a build runs must
    // not be mixed into that build's tags, and a read by the build must not be lost from them.
    private readonly AsyncLocal<DerivedScope?> _derivedScope = new();

    /// <summary>
    /// Initializes a new instance of <see cref="ConfigurationGateway"/>.
    /// </summary>
    /// <param name="connectionName">The configuration connection this gateway reads and writes.</param>
    /// <param name="connectionProvider">The Connection implementation provider that opens ConfigurationDb.</param>
    /// <param name="schema">
    /// Deserialized <see cref="ConfigurationSchema"/> from <c>configurationSchema.json</c>.
    /// Registered as a singleton via <see cref="ConfigurationGatewayTypes"/>, one per connection
    /// declared in the schema.
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
        IImplementationServiceProvider<IGenericConnection, IConnectionImplementationConfiguration> connectionProvider,
        ConfigurationSchema schema,
        ILogger<ConfigurationGateway>? logger = null,
        DataGatewayResultCache? cache = null,
        MainDataGatewayConfiguration? options = null,
        IAuthenticationContextAccessor? authenticationContextAccessor = null)
    {
        ConnectionName = string.IsNullOrWhiteSpace(connectionName)
            ? throw new ArgumentNullException(nameof(connectionName))
            : connectionName;
        _connectionProvider = connectionProvider ?? throw new ArgumentNullException(nameof(connectionProvider));
        _schema = schema ?? throw new ArgumentNullException(nameof(schema));
        _logger = logger ?? NullLogger<ConfigurationGateway>.Instance;
        _cache = cache;
        _options = options;
        _authenticationContextAccessor = authenticationContextAccessor;

        _connectionTypeLazy = new Lazy<IGenericResult<IConnectionType>>(
            ResolveConnectionType,
            LazyThreadSafetyMode.ExecutionAndPublication);

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

        // Recorded on entry, from the command, whoever ends up running the read: a call answered from the cache,
        // or one that joins a read already in flight, still made this read, and a derived value being built
        // depends on it.
        RecordForDerivedBuild(command, target);

        string? cacheKey = null;
        var cacheCeiling = TimeSpan.MaxValue;
        if (CacheEnabled && isQuery)
        {
            var key = CacheKeyFor(command, target, typeof(T).FullName);
            if (!key.IsSuccess)
                return key.ToNewResult<T>();

            cacheKey = key.Value.Key;
            cacheCeiling = key.Value.Ceiling;

            if (useCache && _cache!.TryGet<T>(cacheKey, out var cached) && cached is not null)
            {
                ConfigurationGatewayLog.CachedRead(_logger, target.DataStore, target.Path, target.Container);
                return cached;
            }
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

    // The caller's partition of this configuration connection, and the longest the caller may be shown a cached
    // answer. The one place either is worked out, so that no entry this gateway keeps, a read or a derived value,
    // is keyed by anything else.
    private IGenericResult<(string Partition, TimeSpan Ceiling)> CallerCacheScope(string commandType, string container)
    {
        var connectionTypeResult = _connectionTypeLazy.Value;
        if (!connectionTypeResult.IsSuccess || connectionTypeResult.Value is null)
            return connectionTypeResult.ToNewResult<(string Partition, TimeSpan Ceiling)>();

        var authenticationContext = _authenticationContextAccessor?.Current;

        try
        {
            return GenericResult<(string Partition, TimeSpan Ceiling)>.Success((
                connectionTypeResult.Value.CachePartition(authenticationContext),
                connectionTypeResult.Value.MaxCacheDuration(authenticationContext)));
        }
        catch (Exception ex)
        {
            return GenericResult<(string Partition, TimeSpan Ceiling)>.Failure(
                DataGatewayCacheLog.KeyComputationFailed(_logger, commandType, container, ex.Message));
        }
    }

    // The key a read is cached under, and the longest its caller may be shown a cached answer. The key starts
    // with the caller's partition, so what one caller may see is never served to another: every entry this
    // gateway keeps is keyed from CallerCacheScope, and none without the partition.
    private IGenericResult<(string Key, TimeSpan Ceiling)> CacheKeyFor(IDataCommand command, DataStoreTarget target, string? resultType)
    {
        var scope = CallerCacheScope(command.CommandType, target.Container);
        if (!scope.IsSuccess)
            return scope.ToNewResult<(string Key, TimeSpan Ceiling)>();

        try
        {
            return GenericResult<(string Key, TimeSpan Ceiling)>.Success((
                string.Concat(
                    scope.Value.Partition,
                    "|_cfg|", CacheKeyBuilder.ComputeCacheKey(command, target), ":", resultType),
                scope.Value.Ceiling));
        }
        catch (Exception ex)
        {
            return GenericResult<(string Key, TimeSpan Ceiling)>.Failure(
                DataGatewayCacheLog.KeyComputationFailed(_logger, command.CommandType, target.Container, ex.Message));
        }
    }

    // The key a derived value is kept under: the caller's partition, then what the value is and what kind.
    private IGenericResult<(string Key, TimeSpan Ceiling)> DerivedKeyFor(string name, string? resultType)
    {
        var scope = CallerCacheScope("Derive", name);
        if (!scope.IsSuccess)
            return scope.ToNewResult<(string Key, TimeSpan Ceiling)>();

        return GenericResult<(string Key, TimeSpan Ceiling)>.Success((
            string.Concat(scope.Value.Partition, "|_derived|", name, ":", resultType),
            scope.Value.Ceiling));
    }

    /// <inheritdoc/>
    public async Task<IGenericResult<T>> Derive<T>(
        string name,
        Func<Task<IGenericResult<T>>> build,
        Func<T, bool> isWorthKeeping,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(name);
        ArgumentNullException.ThrowIfNull(build);
        ArgumentNullException.ThrowIfNull(isWorthKeeping);

        // With the cache off nothing is kept, as for every read this gateway makes.
        if (!CacheEnabled)
            return await build().ConfigureAwait(false);

        var key = DerivedKeyFor(name, typeof(T).FullName);
        if (!key.IsSuccess)
            return key.ToNewResult<T>();

        var cacheKey = key.Value.Key;
        var ceiling = key.Value.Ceiling;

        var derived = _cache!.TryGet<DerivedEntry<T>>(cacheKey, out var cached) && cached is not null
            ? cached
            : await _cache.Coalesce(
                cacheKey,
                () => BuildAndKeep(name, cacheKey, ceiling, build, isWorthKeeping),
                cancellationToken).ConfigureAwait(false);

        if (!derived.IsSuccess || derived.Value is not { } entry)
            return derived.ToNewResult<T>();

        // Whoever asked, whether it was handed a kept value, joined a build in flight or ran the build, is
        // depending on the reads that built it, and a build it is part of has to be dropped by them too.
        RecordForDerivedBuild(entry.Tags);

        return GenericResult<T>.Success(entry.Value);
    }

    // Records, in the derived-value build running on this async flow if there is one, the tags a query would be
    // cached under. Nothing is worked out when no build is running.
    private void RecordForDerivedBuild(IDataCommand command, DataStoreTarget target)
    {
        if (command is IQueryCommand && _derivedScope.Value is { } building)
            building.Note(CacheKeyBuilder.GetInvalidationTags(command, target));
    }

    private void RecordForDerivedBuild(IReadOnlyList<string> tags)
    {
        if (_derivedScope.Value is { } building)
            building.Note(tags);
    }

    private async Task<IGenericResult<DerivedEntry<T>>> BuildAndKeep<T>(
        string name,
        string cacheKey,
        TimeSpan ceiling,
        Func<Task<IGenericResult<T>>> build,
        Func<T, bool> isWorthKeeping)
    {
        // A scope of this build's own on this async flow, and the flow's previous one put back afterwards. The
        // reads the build makes are recorded in it, including those of tasks it starts; reads by other requests
        // are on other flows, and a build this one is inside keeps its own scope.
        var outer = _derivedScope.Value;
        var scope = new DerivedScope();
        _derivedScope.Value = scope;

        IGenericResult<T> built;
        try
        {
            built = await build().ConfigureAwait(false);
        }
        finally
        {
            _derivedScope.Value = outer;
        }

        if (!built.IsSuccess || built.Value is not { } value)
            return built.ToNewResult<DerivedEntry<T>>();

        var entry = new DerivedEntry<T>(value, scope.Tags());
        var result = GenericResult<DerivedEntry<T>>.Success(entry);

        // Kept only when something could drop it: a build that read nothing through this gateway has no
        // write that would ever invalidate what it produced.
        if (entry.Tags.Count == 0)
        {
            ConfigurationGatewayLog.DerivedNotKept(
                _logger, name, "the build read nothing through this gateway, so no write could drop it");
            return result;
        }

        if (!isWorthKeeping(value))
        {
            ConfigurationGatewayLog.DerivedNotKept(_logger, name, "the value describes nothing worth remembering");
            return result;
        }

        // The same duration a read is kept for: the default, capped by what the connection type permits.
        _cache!.Set(cacheKey, result, entry.Tags, ceiling < CachePolicy.DefaultDuration ? ceiling : CachePolicy.DefaultDuration);
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
            // A successful-but-empty read is not a fact worth remembering. Whatever produced it --
            // the deny principal seeing nothing before RequestContextMiddleware establishes a real
            // context, a row genuinely not existing yet, a container that hasn't propagated -- caching
            // it makes that transient emptiness durable for up to CachePolicy.DefaultDuration, so every read
            // in that window repeats the same wrong answer instead of getting a chance to see the
            // real one. IsSuccess only proves the query ran; it says nothing about whether the answer
            // is one worth keeping.
            if (cacheKey is not null && !IsEmptyResult(result.Value))
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

    // Only collection-shaped results (IEnumerable<T> query results, not a single row or a scalar)
    // can be "empty" in the sense this guards against -- a null Value already skips caching via
    // result.IsSuccess's own contract elsewhere, and a scalar or single-object Value is never empty.
    private static bool IsEmptyResult(object? value)
        => value is IEnumerable enumerable and not string && !enumerable.Cast<object?>().Any();

    private async Task<IGenericResult<T>> ExecuteCore<T>(IDataCommand command, DataStoreTarget target, CancellationToken cancellationToken)
    {
        var resolved = await ResolveTarget(target, cancellationToken).ConfigureAwait(false);
        if (!resolved.IsSuccess)
            return resolved.ToNewResult<T>();

        var (container, connection) = resolved.Value;
        return await RunOnContainer(target, ct => connection.Execute<T>(command, container, ct), cancellationToken).ConfigureAwait(false);
    }

    // The connection this gateway reaches its stores by, and the container a target names in its schema: what every
    // command run here needs first, a data command and a vectorized one alike.
    private async Task<IGenericResult<(IDataContainer Container, IDataConnection Connection)>> ResolveTarget(
        DataStoreTarget target, CancellationToken cancellationToken)
    {
        ConfigurationGatewayLog.ExecuteEntry(_logger, target.DataStore, target.Path, target.Container);

        if (string.IsNullOrWhiteSpace(target.DataStore))
        {
            return GenericResult<(IDataContainer, IDataConnection)>.Failure(
                ConfigurationGatewayLog.ExecuteFailed(_logger, target.Container,
                    "DataStoreTarget.DataStore is required"));
        }

        var connectionResult = await BuildConnection(cancellationToken).ConfigureAwait(false);
        if (!connectionResult.IsSuccess || connectionResult.Value is null)
            return connectionResult.ToNewResult<(IDataContainer, IDataConnection)>();

        var containerResult = ResolveContainerResult(target);

        if (!containerResult.IsSuccess)
        {
            ConfigurationGatewayLog.ExecuteFailed(_logger, target.Container,
                containerResult.CurrentMessage ?? "Container could not be resolved from the configuration schema");

            return containerResult.ToNewResult<(IDataContainer, IDataConnection)>();
        }

        if (containerResult.Value is null)
        {
            return GenericResult<(IDataContainer, IDataConnection)>.Failure(
                ConfigurationGatewayLog.ExecuteFailed(_logger, target.Container,
                    "Container could not be resolved from the configuration schema"));
        }

        return GenericResult<(IDataContainer, IDataConnection)>.Success((containerResult.Value, connectionResult.Value));
    }

    // Runs one command's work on the container: its exit logged, and a throw returned as a failure.
    private async Task<IGenericResult<T>> RunOnContainer<T>(
        DataStoreTarget target, Func<CancellationToken, Task<IGenericResult<T>>> run, CancellationToken cancellationToken)
    {
        try
        {
            var mainResult = await run(cancellationToken).ConfigureAwait(false);
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
    public async Task<IGenericResult<DatabaseGenerationReading>> DatabaseGeneration(CancellationToken cancellationToken = default)
    {
        var connectionResult = await BuildConnection(cancellationToken).ConfigureAwait(false);
        if (!connectionResult.IsSuccess || connectionResult.Value is null)
            return connectionResult.ToNewResult<DatabaseGenerationReading>();

        if (connectionResult.Value is not IDatabaseGenerationConnection generationConnection)
            return GenericResult<DatabaseGenerationReading>.Success(DatabaseGenerationReading.CannotTell);

        try
        {
            var generation = await generationConnection.DatabaseGeneration(cancellationToken).ConfigureAwait(false);
            return generation.IsSuccess && generation.Value is { } told
                ? GenericResult<DatabaseGenerationReading>.Success(DatabaseGenerationReading.Of(told))
                : generation.ToNewResult<DatabaseGenerationReading>();
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return GenericResult<DatabaseGenerationReading>.Failure(
                ConfigurationGatewayLog.GenerationNotRead(_logger, ConnectionName, ex.Message));
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

        var connectionResult = await BuildConnection(cancellationToken).ConfigureAwait(false);
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

        if (!CacheEnabled || _cache is not { } cache || command is not IQueryCommand query)
            return await ReadRows(command, target, rowType, cancellationToken).ConfigureAwait(false);

        // The typed-list child read a configuration aggregate makes once per owner row: cached like every other
        // read here, under the caller's partition, and dropped by a write to ANY container the read touches.
        var tags = RowTags(query, target);
        if (!tags.IsSuccess || tags.Value is not { } rowTags)
            return tags.ToNewResult<IEnumerable<object>>();

        // Recorded before the cache is asked, for the reason given on the typed read: a cached answer is a read too.
        RecordForDerivedBuild(rowTags);

        var key = CacheKeyFor(command, target, string.Concat(rowType.FullName, ":rows"));
        if (!key.IsSuccess)
            return key.ToNewResult<IEnumerable<object>>();

        var cacheKey = key.Value.Key;
        if (cache.TryGet<IEnumerable<object>>(cacheKey, out var cached) && cached is not null)
        {
            ConfigurationGatewayLog.CachedRead(_logger, target.DataStore, target.Path, target.Container);
            return cached;
        }

        return await cache.Coalesce(
            cacheKey,
            async () =>
            {
                var read = await ReadRows(command, target, rowType, cancellationToken).ConfigureAwait(false);

                // Kept even when it is empty: an owner with no children of a kind is a fact about an owner that
                // was just read, and a write to the child table drops it. A root read that finds nothing is not
                // kept (see ApplyCacheOutcome), because that one may just be a row that does not exist yet.
                if (read.IsSuccess)
                {
                    cache.Set(cacheKey, read, rowTags, CachePolicy.GetDuration(command, key.Value.Ceiling));
                }

                return read;
            },
            cancellationToken).ConfigureAwait(false);
    }

    private async Task<IGenericResult<IEnumerable<object>>> ReadRows(
        IDataCommand command, DataStoreTarget target, Type rowType, CancellationToken cancellationToken)
    {
        var connectionResult = await BuildConnection(cancellationToken).ConfigureAwait(false);
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

    // Everything a child read reads is something whose write must drop its entry: the child's own container,
    // and each container it joins to (the owner row, and above it the domain row, decide which children are
    // current). A container the schema does not declare cannot be tagged, so the read fails rather than being
    // kept under a tag nothing will ever drop.
    private IGenericResult<IReadOnlyList<string>> RowTags(IQueryCommand command, DataStoreTarget target)
    {
        var tags = new List<string>(CacheKeyBuilder.GetInvalidationTags(command, target));

        foreach (var join in command.Joins)
        {
            var joined = TagOf(target.DataStore, join.TargetContainerName);
            if (joined is null)
                return GenericResult<IReadOnlyList<string>>.Failure(ConfigurationGatewayLog.ExecuteFailed(
                    _logger, target.Container, $"Joined container '{join.TargetContainerName}' is not declared in data store '{target.DataStore}'"));

            tags.Add(joined);

            if (join.SourceContainerName is not { } source)
                continue;

            var from = TagOf(target.DataStore, source);
            if (from is null)
                return GenericResult<IReadOnlyList<string>>.Failure(ConfigurationGatewayLog.ExecuteFailed(
                    _logger, target.Container, $"Joined container '{source}' is not declared in data store '{target.DataStore}'"));

            tags.Add(from);
        }

        return GenericResult<IReadOnlyList<string>>.Success(tags);
    }

    // The invalidation tag a write to a container drops: its path and its name. Container names are unique
    // within a data store, so the name alone finds it.
    private string? TagOf(string dataStoreName, string containerName)
    {
        foreach (var store in _dataStores.Value)
        {
            if (!string.Equals(store.Name, dataStoreName, StringComparison.OrdinalIgnoreCase))
                continue;

            foreach (var container in store.Paths.SelectMany(path => path.Containers))
            {
                if (string.Equals(container.Name, containerName, StringComparison.Ordinal))
                    return string.Concat(container.Parent.Name, ".", container.Name);
            }
        }

        return null;
    }

    /// <inheritdoc/>
    public async Task<IGenericResult> Execute(IDataCommand command, DataStoreTarget target, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(target.DataStore))
        {
            return GenericResult.Failure(
                ConfigurationGatewayLog.ExecuteFailed(_logger, target.Container, "DataStoreTarget.DataStore is required"));
        }

        var connectionResult = await BuildConnection(cancellationToken).ConfigureAwait(false);
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
        var connectionResult = await BuildConnection(cancellationToken).ConfigureAwait(false);
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

            if (string.IsNullOrWhiteSpace(_schema.Connections[i].Implementation))
            {
                return GenericResult<IConnectionType>.Failure(
                    DataGatewayCacheLog.CachePartitionUnavailable(
                        _logger, ConnectionName, "the connection names no implementation"));
            }

            if (ReferenceEquals(ConnectionTypes.ByName(_schema.Connections[i].Implementation), ConnectionTypes.NotFound))
            {
                return GenericResult<IConnectionType>.Failure(
                    DataGatewayCacheLog.CachePartitionUnavailable(
                        _logger,
                        ConnectionName,
                        $"connection type '{_schema.Connections[i].Implementation}' is not registered"));
            }

            return GenericResult<IConnectionType>.Success(
                ConnectionTypes.ByName(_schema.Connections[i].Implementation));
        }

        return GenericResult<IConnectionType>.Failure(
            DataGatewayCacheLog.CachePartitionUnavailable(
                _logger, ConnectionName, "the connection is not declared in configurationSchema.json"));
    }

    private async Task<IGenericResult<IDataConnection>> BuildConnection(CancellationToken cancellationToken)
    {
        ConfigurationGatewayLog.BuildConnectionEntry(_logger, ConnectionName);

        IConnectionImplementationConfiguration? configDbEntry = null;

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

        var factoryResult = await _connectionProvider
            .Create(configDbEntry, cancellationToken)
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

    private static IDataStore? BuildStore(DataStoreImplementationConfiguration storeCfg, ILogger logger)
    {
        var storeType = string.IsNullOrEmpty(storeCfg.Implementation)
            ? null
            : DataStoreTypes.All().FirstOrDefault(t =>
                string.Equals(t.Name, storeCfg.Implementation, StringComparison.OrdinalIgnoreCase));

        if (storeType is null)
        {
            ConfigurationGatewayLog.ExecuteFailed(logger, storeCfg.Name,
                $"DataStore transport '{storeCfg.Implementation}' is not a registered DataStoreType");
            return null;
        }

        var builder = storeType.SupplyBuilder(logger);

        var configureResult = builder.Configure(storeCfg);
        if (!configureResult.IsSuccess)
            return null;

        // Every store this gateway builds is one the host declared in configurationSchema.json; it carries that fact
        // so the main data gateway can refuse requests over it (FDW-1068).
        builder.DeclareByHost();

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
