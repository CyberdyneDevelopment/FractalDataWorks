using System;
using System.Collections.Generic;
using Fdw.Collections;
using Fdw.Configuration.Abstractions;
using Fdw.Conventions;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Commands.Data;
using Fdw.Data.Abstractions;
using Fdw.MessageLogging;
using Fdw.Results;
using Fdw.Services.Abstractions;
using Fdw.Services.Connections;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Data.Logging;
using Fdw.Services.Data.Results;
using Fdw.ServiceTypes;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Fdw.Services.Authentication.Abstractions.Security;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Fdw.Services.Data;

/// <summary>
/// Server-side <see cref="IDataStoreProvider"/>. ConfigurationDb-shipped stores (the bounded schema tree
/// built from <c>configurationSchema.json</c>) are returned directly from
/// <see cref="IConfigurationGateway.DataStores"/> — a shortcut that avoids recursing back into the
/// gateway. Every other DataStore is composed and built by the connection-agnostic
/// <see cref="ConfiguredDataStoreProvider"/> core (Paths → Containers → Fields via the per-transport
/// <c>IDataStoreBuilder</c>); this class then merges ConfigurationDb's own stores into the full-tree
/// result so any endpoint targeting either set resolves correctly.
/// Also provides static Configure/Register/Initialize methods for three-phase DI registration.
/// </summary>
[PlatformServiceProvider(ServiceCategory = "DataStore")]
public sealed class ConfigurationGatewayDataStoreProvider : IDataStoreProvider
{
    // ============================================================
    // Static DI Orchestration (moved from DataStoreTypes)
    // ============================================================

    /// <summary>
    /// Phase 1a: Configures IOptions bindings for DataStore configurations.
    /// Call before Build(). Configuration source must be added BEFORE calling this method.
    /// </summary>
    /// <param name="builder">The host builder.</param>
    /// <param name="loggerFactory">The host's logger factory, when one is available.</param>
    /// <param name="force">Run regardless of the skip flag and whether the phase has already run.</param>
    /// <param name="defer">Claim the phase without running it: the collect skips it and the next explicit call runs it.</param>
    public static IGenericResult<IHostApplicationBuilder> Configure(IHostApplicationBuilder builder, ILoggerFactory? loggerFactory = null, bool force = false, bool defer = false)
    {
        // Why: defer is the host claiming this phase to run at a position it chooses.
        // Stateless, so there is no flag to set - returning is the whole of it.
        if (defer)
        {
            return GenericResult<IHostApplicationBuilder>.Success(builder);
        }


        builder.Services.Configure<List<DataPathConfiguration>>(
            builder.Configuration.GetSection("DataStores:DataPath"));
        builder.Services.Configure<List<DataContainerConfiguration>>(
            builder.Configuration.GetSection("DataStores:DataContainer"));
        builder.Services.Configure<List<DataContainerFieldConfiguration>>(
            builder.Configuration.GetSection("DataStores:DataContainerField"));

        var logger = loggerFactory?.CreateLogger(typeof(ConfigurationGatewayDataStoreProvider)) ?? NullLogger.Instance;
        DataStoreTypesLog.ConfiguredOptionsBindings(logger);

        return GenericResult<IHostApplicationBuilder>.Success(builder);
    }

    /// <summary>
    /// Phase 1b: Registers required services (factories) for all data store types.
    /// Call before Build().
    /// </summary>
    /// <param name="builder">The host builder.</param>
    /// <param name="loggerFactory">The host's logger factory, when one is available.</param>
    /// <param name="force">Run regardless of the skip flag and whether the phase has already run.</param>
    /// <param name="defer">Claim the phase without running it: the collect skips it and the next explicit call runs it.</param>
    public static IGenericResult<IHostApplicationBuilder> Register(IHostApplicationBuilder builder, ILoggerFactory? loggerFactory = null, bool force = false, bool defer = false)
    {
        // Why: defer is the host claiming this phase to run at a position it chooses.
        // Stateless, so there is no flag to set - returning is the whole of it.
        if (defer)
        {
            return GenericResult<IHostApplicationBuilder>.Success(builder);
        }

        var services = builder.Services;

        services.TryAddSingleton<IDataStoreBuilderSelector, DataStoreTypesBuilderSelector>();
        services.TryAddSingleton<ConfiguredDataStoreProvider>(sp => new ConfiguredDataStoreProvider(
            sp.GetService<ILogger<ConfiguredDataStoreProvider>>(),
            sp.GetRequiredService<DataStoreConfigurationProvider>(),
            sp.GetRequiredService<IDataStoreBuilderSelector>()));

        services.AddSingleton<ConfigurationGatewayDataStoreProvider>(sp =>
        {
            var providerLogger = sp.GetRequiredService<ILoggerFactory>().CreateLogger<ConfigurationGatewayDataStoreProvider>();
            var coreProvider = sp.GetRequiredService<ConfiguredDataStoreProvider>();
            var configProvider = sp.GetRequiredService<DataStoreConfigurationProvider>();
            var gatewayProvider = sp.GetRequiredService<IConfigurationGatewayProvider>();
            return new ConfigurationGatewayDataStoreProvider(providerLogger, coreProvider, configProvider, gatewayProvider);
        });
        services.AddSingleton<IDataStoreProvider>(sp => sp.GetRequiredService<ConfigurationGatewayDataStoreProvider>());



        // Register runs pre-Build, so there is no container to resolve a logger from — the factory the
        // host hands in is the only source available in this phase.
        var logger = loggerFactory?.CreateLogger(typeof(ConfigurationGatewayDataStoreProvider)) ?? NullLogger.Instance;
        DataStoreTypesLog.RegisteredInfrastructureServices(logger);

        return GenericResult<IHostApplicationBuilder>.Success(builder);
    }

    /// <summary>
    /// Phase 2: Initializes by registering factories and configurations with the DataStoreProvider.
    /// Call after Build().
    /// </summary>
    /// <remarks>
    /// Normalized to a synchronous <c>void</c> signature so <c>[PlatformServiceProvider]</c> matches the
    /// same three-phase shape every collected domain declares — the actual work (async config load) is in
    /// <see cref="LoadStores"/>, blocked-on here exactly once at startup.
    /// </remarks>
    /// <param name="host">The built host.</param>
    /// <param name="loggerFactory">The host's logger factory, when one is available.</param>
    /// <param name="force">Run regardless of the skip flag and whether the phase has already run.</param>
    /// <param name="defer">Claim the phase without running it: the collect skips it and the next explicit call runs it.</param>
    public static IGenericResult<IHost> Initialize(IHost host, ILoggerFactory? loggerFactory = null, bool force = false, bool defer = false)
    {
        // Why: defer is the host claiming this phase to run at a position it chooses.
        // Stateless, so there is no flag to set - returning is the whole of it.
        if (defer)
        {
            return GenericResult<IHost>.Success(host);
        }

        using var scope = host.Services.CreateScope();
        // Resolved so the provider is built at startup and a registration fault in it stops the host here.
        scope.ServiceProvider.GetRequiredService<IDataStoreProvider>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<ConfigurationGatewayDataStoreProvider>>();

        // Why the kinds are attached here (FDW-3863): a data store row names its kind, and composing the store needs the kind's implementation provider on the domain provider. The
        // DataStoreService domain attaches them in its own Initialize, but "DataStore" sorts before "DataStoreService" in the generated category order, so that had not run yet when the
        // stores were loaded and every store failed to compose. The attach is idempotent, so the later one is harmless.
        var attached = DataDomainRegistrations.AttachDataStoreKinds(scope.ServiceProvider);
        if (attached.IsFailure)
        {
            return attached.ToNewResult<IHost>();
        }

        // The elevation is opened and closed around this read, by the domain that needs it: a
        // boot-time load has no ClaimsPrincipal and no TenantId, so under the RLS predicates it
        // resolves to the deny-everywhere principal and reads nothing. Scoping it here rather than
        // in the host keeps the window to the load itself, and no host has to know to do it.
        using (new SystemAuthenticationContextScope(
            scope.ServiceProvider.GetRequiredService<IAuthenticationContextAccessor>()))
        {
#pragma warning disable VSTHRD002
            var loaded = LoadStores(scope.ServiceProvider, logger).GetAwaiter().GetResult();
#pragma warning restore VSTHRD002
            if (loaded.IsFailure)
            {
                return loaded.ToNewResult<IHost>();
            }
        }

        return GenericResult<IHost>.Success(host);
    }

    [ConventionOverride(MaxCyclomaticComplexity = 25)]
    internal static async Task<IGenericResult> LoadStores(
        IServiceProvider services,
        ILogger<ConfigurationGatewayDataStoreProvider> logger)
    {
        var configProvider = services.GetRequiredService<DataStoreConfigurationProvider>();
        var configResult = await configProvider.Get().ConfigureAwait(false);
        if (!configResult.IsSuccess)
        {
            DataStoreTypesLog.OptionsBindingSummary(logger, 0, 0, 0, 0);
            return GenericResult.Failure(DataStoreProviderLog.ContainerCreationFailed(logger, "all", "Initialize", configResult.FailureReason().Message));
        }

        // A data store that cannot be composed from its rows fails the load: the header row alone has no paths or containers, so keeping it would list a store that nothing can use.
        var dataStores = new List<Fdw.Services.Connections.IDataStoreImplementationConfiguration>();
        foreach (var shallow in configResult.Value!)
        {
            if (string.IsNullOrWhiteSpace(shallow.Name)) continue;
            var composed = await configProvider.Get(shallow.Name).ConfigureAwait(false);
            if (composed.IsFailure)
            {
                return GenericResult.Failure(DataStoreProviderLog.ContainerCreationFailed(logger, shallow.Name, "Initialize", composed.FailureReason().Message));
            }

            if (composed.Value is null)
            {
                return GenericResult.Failure(DataStoreProviderLog.DataStoreComposedNothing(logger, shallow.Name));
            }

            dataStores.Add(composed.Value);
        }

        var dataPaths = dataStores.SelectMany(ds => ds.Paths ?? []).ToList();
        var dataContainers = dataPaths.SelectMany(p => p.Containers ?? []).ToList();
        var dataContainerFields = dataContainers.SelectMany(c => c.Fields ?? []).ToList();

        DataStoreTypesLog.OptionsBindingSummary(logger, dataStores.Count, dataPaths.Count, dataContainers.Count, dataContainerFields.Count);

        var storeNames = dataStores.Where(d => !string.IsNullOrWhiteSpace(d.Name)).Select(d => d.Name);
        DataStoreTypesLog.AvailableDataStoreNames(logger, string.Join(", ", storeNames));

        var containerNames = dataContainers.Where(c => !string.IsNullOrWhiteSpace(c.Name)).Select(c => c.Name);
        DataStoreTypesLog.AvailableContainerNames(logger, string.Join(", ", containerNames));

        foreach (var ds in dataStores.Where(d => !string.IsNullOrWhiteSpace(d.Name)))
        {
            if (string.IsNullOrEmpty(ds.Implementation))
            {
                DataStoreTypesLog.DataStoreMissingImplementation(logger, ds.Name);
                continue;
            }

            DataStoreTypesLog.DataStoreLoaded(logger, ds.Id, ds.Name, ds.Implementation);
        }

        foreach (var path in dataPaths.Where(p => !string.IsNullOrWhiteSpace(p.Name)))
        {
            DataStoreTypesLog.DataPathLoaded(logger, path.Id, path.Name);
        }

        foreach (var container in dataContainers.Where(c => !string.IsNullOrWhiteSpace(c.Name)))
        {
            DataStoreTypesLog.DataContainerLoaded(logger, container.Id, container.Name);
        }

        foreach (var field in dataContainerFields.Where(f => !string.IsNullOrWhiteSpace(f.Name)))
        {
            DataStoreTypesLog.FieldLoaded(logger, field.Id, field.Name, field.DataType ?? string.Empty);
        }

        foreach (var ds in dataStores.Where(d => !string.IsNullOrWhiteSpace(d.Name)))
        {
            var pathNameList = string.Join(", ", (ds.Paths ?? []).Select(sp => sp.Name));
            DataStoreTypesLog.DataStoreHierarchy(logger, ds.Name, ds.Id, pathNameList);

            foreach (var path in (ds.Paths ?? []))
            {
                var containerNameList = string.Join(", ", (path.Containers ?? []).Select(pc => pc.Name));
                DataStoreTypesLog.PathHierarchy(logger, path.Name, path.Id, containerNameList);

                foreach (var container in (path.Containers ?? []))
                {
                    DataStoreTypesLog.ContainerQualifiedName(logger, ds.Name, path.Name, container.Name);
                }
            }
        }

        DataStoreTypesLog.DataStoreTypesInitialized(logger, DataStoreTypes.All().Count, dataStores.Count(c => !string.IsNullOrWhiteSpace(c.Name)));
        return GenericResult.Success();
    }

    // ============================================================
    // Instance Members
    // ============================================================

    private readonly ILogger<ConfigurationGatewayDataStoreProvider> _logger;

    private readonly ConfiguredDataStoreProvider _coreProvider;

    private readonly IDomainConfigurationProvider<IDataStoreImplementationConfiguration> _dataStoreConfigProvider;

    private static string ConfigurationConnectionName => PlatformConnection.Name;

    private readonly IConfigurationGatewayProvider? _gatewayProvider;

    /// <summary>
    /// Initializes a new instance of the <see cref="ConfigurationGatewayDataStoreProvider"/> class.
    /// </summary>
    public ConfigurationGatewayDataStoreProvider(
        ILogger<ConfigurationGatewayDataStoreProvider> logger,
        ConfiguredDataStoreProvider coreProvider,
        IDomainConfigurationProvider<IDataStoreImplementationConfiguration> dataStoreConfigProvider,
        IConfigurationGatewayProvider? gatewayProvider = null)
    {
        _logger = logger ?? NullLogger<ConfigurationGatewayDataStoreProvider>.Instance;
        _coreProvider = coreProvider;
        _dataStoreConfigProvider = dataStoreConfigProvider;
        _gatewayProvider = gatewayProvider;
    }

    /// <inheritdoc/>
    /// <remarks>
    /// A store the host declared is answered from the gateway's own stores, as <see cref="Get(string, CancellationToken)"/> does. Any other is its
    /// row, read as the caller, so one the caller cannot see is not found (FDW-4253): the tree is not built to say so.
    /// </remarks>
    public async Task<IGenericResult<DataStoreReference>> GetReference(string name, CancellationToken cancellationToken = default)
    {
        DataStoreProviderLog.TraceGetDataStoreEntry(_logger, name);

        if (_gatewayProvider is not null)
        {
            var gw = _gatewayProvider.Get(ConfigurationConnectionName);
            var gwStore = gw.IsSuccess
                ? gw.Value!.DataStores.FirstOrDefault(s => string.Equals(s.Name, name, StringComparison.OrdinalIgnoreCase))
                : null;
            if (gwStore is not null)
                return GenericResult<DataStoreReference>.Success(new DataStoreReference(gwStore.Name, gwStore.ConnectionId, gwStore.DeclaredByHost));
        }

        return await _coreProvider.GetReference(name, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async Task<IGenericResult<IDataStore>> Get(string name, CancellationToken cancellationToken = default)
    {
        DataStoreProviderLog.TraceGetDataStoreEntry(_logger, name);

        if (_gatewayProvider is not null)
        {
            var gw = _gatewayProvider.Get(ConfigurationConnectionName);
            var gwStore = gw.IsSuccess
                ? gw.Value!.DataStores.FirstOrDefault(s => string.Equals(s.Name, name, StringComparison.OrdinalIgnoreCase))
                : null;
            if (gwStore is not null)
            {
                DataStoreProviderLog.DataStoreRetrieved(_logger, name);
                return GenericResult<IDataStore>.Success(gwStore);
            }

            // Built once for the caller's partition and kept until a container the build read is written, not on
            // every command (FDW-992). The build composes the store from reads through this same gateway, which
            // is how it knows what to drop the store on. A host whose gateway offers no such cache, or that has
            // no gateway at all, builds as it always did.
            if (gw.IsSuccess && gw.Value is IDerivedConfigurationCache derived)
            {
                return await derived.Derive(
                    name, () => _coreProvider.Get(name, cancellationToken), HasContainers, cancellationToken)
                    .ConfigureAwait(false);
            }
        }

        return await _coreProvider.Get(name, cancellationToken).ConfigureAwait(false);
    }

    // A store built while the caller could see none of its configuration has no containers and describes
    // nothing; kept, that would last as long as the cache does. The same reason an empty read is not kept.
    private static bool HasContainers(IDataStore store) => store.Paths.Any(path => path.Containers.Count > 0);

    /// <inheritdoc/>
    public async Task<IGenericResult<IReadOnlyList<IDataStore>>> Get(CancellationToken cancellationToken = default)
    {
        DataStoreProviderLog.TraceGetAllDataStoresEntry(_logger);
        var stores = await Load(cancellationToken).ConfigureAwait(false);
        if (stores.IsFailure)
            return stores;

        // Load answers a success only with the list it built (a new List of the stores, possibly empty), never with no value, so there is no absence to pass on here.
        DataStoreProviderLog.AllDataStoresRetrieved(_logger, stores.Value!.Count);
        return stores;
    }

    /// <inheritdoc/>
    public async Task<IGenericResult<IDataStore>> Get(Guid id, CancellationToken cancellationToken = default)
    {
        DataStoreProviderLog.TraceGetDataStoreByIdEntry(_logger, id);
        var cfgResult = await _dataStoreConfigProvider.Get(id, cancellationToken).ConfigureAwait(false);
        if (!cfgResult.IsSuccess || cfgResult.Value is null || string.IsNullOrWhiteSpace(cfgResult.Value.Name))
        {
            DataStoreProviderLog.DataStoreByIdNotFound(_logger, id);
            return GenericResult<IDataStore>.Failure(
                DataServiceResultCodes.ByName("DataStoreNotFound"),
                ResultDetails.Create().With("DataStoreId", id));
        }
        return await Get(cfgResult.Value.Name, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async Task<IGenericResult<IDataNodePath>> Get(string dataStoreName, string pathName, CancellationToken cancellationToken = default)
    {
        var storeResult = await Get(dataStoreName, cancellationToken).ConfigureAwait(false);
        if (!storeResult.IsSuccess || storeResult.Value is null) return storeResult.ToNewResult<IDataNodePath>();

        var pathResult = storeResult.Value.Path(pathName);
        if (!pathResult.IsSuccess)
            DataStoreLoaderLog.PathNotFoundAddressed(_logger, pathName, dataStoreName);

        return pathResult;
    }

    /// <inheritdoc/>
    public Task<IGenericResult<IDataContainer>> Get(string dataStoreName, string pathName, string containerName, CancellationToken cancellationToken = default)
        => Get(dataStoreName, pathName, containerName, [], cancellationToken);

    /// <inheritdoc/>
    /// <remarks>
    /// A store the host declared is walked from the gateway's own tree. Any other builds the containers named and no others (FDW-4328), kept for the
    /// caller until a table it read is written, under a key of its own: the result is a container and is never kept as the store, whose tree is the
    /// complete child set.
    /// </remarks>
    public async Task<IGenericResult<IDataContainer>> Get(
        string dataStoreName, string pathName, string containerName, IReadOnlyCollection<string> joinedContainers, CancellationToken cancellationToken = default)
    {
        DataStoreProviderLog.TraceGetDataStoreEntry(_logger, dataStoreName);

        if (_gatewayProvider is not null)
        {
            var gw = _gatewayProvider.Get(ConfigurationConnectionName);
            var gwStore = gw.IsSuccess
                ? gw.Value!.DataStores.FirstOrDefault(s => string.Equals(s.Name, dataStoreName, StringComparison.OrdinalIgnoreCase))
                : null;
            if (gwStore is not null)
                return Located(gwStore.Path(pathName), pathName, containerName, dataStoreName);

            if (gw.IsSuccess && gw.Value is IDerivedConfigurationCache derived)
            {
                var names = new[] { containerName }.Concat(joinedContainers).Distinct(StringComparer.Ordinal).OrderBy(n => n, StringComparer.Ordinal);
                var kept = string.Join("|", dataStoreName, pathName, string.Join(",", names));
                return await derived.Derive(
                    kept,
                    () => _coreProvider.Get(dataStoreName, pathName, containerName, joinedContainers, cancellationToken),
                    _ => true,
                    cancellationToken).ConfigureAwait(false);
            }
        }

        return await _coreProvider.Get(dataStoreName, pathName, containerName, joinedContainers, cancellationToken).ConfigureAwait(false);
    }

    // The container a store holds in a path, with the failure logged as it is when the whole store is walked.
    private IGenericResult<IDataContainer> Located(IGenericResult<IDataNodePath> pathResult, string pathName, string containerName, string dataStoreName)
    {
        if (!pathResult.IsSuccess || pathResult.Value is null)
        {
            DataStoreLoaderLog.PathNotFoundAddressed(_logger, pathName, dataStoreName);
            return pathResult.ToNewResult<IDataContainer>();
        }

        var containerResult = pathResult.Value.Container(containerName);
        if (!containerResult.IsSuccess)
            DataStoreLoaderLog.ContainerNotFoundInPathAddressed(_logger, containerName, pathName, dataStoreName);

        return containerResult;
    }

    // ============================================================
    // Load — builds the Data.Abstractions.IDataStore tree
    // ============================================================

    /// <summary>
    /// Loads all DataStores and builds the <see cref="Fdw.Data.Abstractions.IDataStore"/> tree: every
    /// non-gatewayProvider store is composed and built by the connection-agnostic core, then ConfigurationDb's
    /// own gateway-owned stores are merged in.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The assembled IDataStore tree, or the failure of the read that could not be made (FDW-3630): an empty tree says there are no stores, not that they could not be read.</returns>
    public async Task<IGenericResult<IReadOnlyList<Fdw.Data.Abstractions.IDataStore>>> Load(CancellationToken ct = default)
    {
        DataStoreProviderLog.LoadStarted(_logger);

        var coreResult = await _coreProvider.Get(ct).ConfigureAwait(false);
        if (!coreResult.IsSuccess)
        {
            return GenericResult<IReadOnlyList<Fdw.Data.Abstractions.IDataStore>>.Failure(
                new[]
                {
                    DataStoreProviderLog.LoadFailed(
                        _logger,
                        new InvalidOperationException(coreResult.FailureReason().Message),
                        coreResult.FailureReason().Message)
                }.Concat(coreResult.Messages));
        }

        var composedStores = coreResult.Value ?? [];
        var totalPaths = composedStores.Sum(s => s.Paths.Count);
        var totalContainers = composedStores.Sum(s => s.Paths.Sum(p => p.Containers.Count));

        var result = new List<Fdw.Data.Abstractions.IDataStore>(composedStores);
        var merged = MergeConfigurationGatewayDataStores(result);
        if (merged.IsFailure)
            return merged.ToNewResult<IReadOnlyList<Fdw.Data.Abstractions.IDataStore>>();

        DataStoreProviderLog.LoadCompleted(_logger, result.Count, totalPaths, totalContainers);
        return GenericResult<IReadOnlyList<Fdw.Data.Abstractions.IDataStore>>.Success(result);
    }

    private IGenericResult MergeConfigurationGatewayDataStores(List<Fdw.Data.Abstractions.IDataStore> result)
    {
        if (_gatewayProvider is null) return GenericResult.Success();

        IReadOnlyList<Fdw.Data.Abstractions.IDataStore> configDbStores;
        try
        {
            var gw = _gatewayProvider.Get(ConfigurationConnectionName);
            if (gw.IsFailure)
                return GenericResult.Failure(new[] { DataStoreProviderLog.LoadFailed(_logger, new InvalidOperationException(gw.FailureReason().Message), gw.FailureReason().Message) }.Concat(gw.Messages));

            configDbStores = gw.Value!.DataStores ?? [];
        }
        catch (Exception ex)
        {
            return GenericResult.Failure(DataStoreProviderLog.LoadFailed(_logger, ex, "ConfigurationDb DataStores read failed"));
        }

        if (configDbStores.Count == 0) return GenericResult.Success();

        var existingNames = new HashSet<string>(
            result.Where(s => !string.IsNullOrWhiteSpace(s.Name)).Select(s => s.Name),
            StringComparer.OrdinalIgnoreCase);
        foreach (var store in configDbStores)
        {
            if (string.IsNullOrWhiteSpace(store.Name)) continue;
            if (existingNames.Contains(store.Name)) continue;
            result.Add(store);
            DataStoreProviderLog.DataStoreRegistered(_logger, store.Name, "configuration-gateway");
        }

        return GenericResult.Success();
    }

}
