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
using Fdw.Services.Connections;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Data.Logging;
using Fdw.Services.Data.Results;
using Fdw.ServiceTypes;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
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

        builder.Services.Configure<List<DataPathConfiguration>>(
            builder.Configuration.GetSection("DataStores:DataPath"));
        builder.Services.Configure<List<DataContainerConfiguration>>(
            builder.Configuration.GetSection("DataStores:DataContainer"));
        builder.Services.Configure<List<DataContainerFieldConfiguration>>(
            builder.Configuration.GetSection("DataStores:DataContainerField"));

        foreach (var type in DataStoreTypes.All())
        {
            type.Configure(builder.Services, builder.Configuration, loggerFactory);
        }

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
        var services = builder.Services;

        services.TryAddSingleton<IDataStoreBuilderSelector, DataStoreTypesBuilderSelector>();
        services.TryAddSingleton<ConfiguredDataStoreProvider>(sp => new ConfiguredDataStoreProvider(
            sp.GetService<ILogger<ConfiguredDataStoreProvider>>(),
            sp.GetRequiredService<DataStoreConfigurationProvider>(),
            sp.GetRequiredService<IDataStoreBuilderSelector>()));

        services.AddScoped<ConfigurationGatewayDataStoreProvider>(sp =>
        {
            var providerLogger = sp.GetRequiredService<ILoggerFactory>().CreateLogger<ConfigurationGatewayDataStoreProvider>();
            var coreProvider = sp.GetRequiredService<ConfiguredDataStoreProvider>();
            var configProvider = sp.GetRequiredService<DataStoreConfigurationProvider>();
            var gatewayProvider = sp.GetRequiredService<IConfigurationGatewayProvider>();
            return new ConfigurationGatewayDataStoreProvider(providerLogger, coreProvider, configProvider, gatewayProvider);
        });
        services.AddScoped<IDataStoreProvider>(sp => sp.GetRequiredService<ConfigurationGatewayDataStoreProvider>());



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
        using var scope = host.Services.CreateScope();
        var provider = scope.ServiceProvider.GetRequiredService<IDataStoreProvider>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<ConfigurationGatewayDataStoreProvider>>();

#pragma warning disable VSTHRD002
        LoadStores(scope.ServiceProvider, provider, logger).GetAwaiter().GetResult();
#pragma warning restore VSTHRD002
    
        return GenericResult<IHost>.Success(host);
    }

    [ConventionOverride(MaxCyclomaticComplexity = 25)]
    private static async Task LoadStores(
        IServiceProvider services,
        IDataStoreProvider provider,
        ILogger<ConfigurationGatewayDataStoreProvider> logger)
    {
        var configProvider = services.GetRequiredService<DataStoreConfigurationProvider>();
        var configResult = await configProvider.Get().ConfigureAwait(false);
        if (!configResult.IsSuccess)
        {
            DataStoreTypesLog.OptionsBindingSummary(logger, 0, 0, 0, 0);
            DataStoreProviderLog.ContainerCreationFailed(logger, "all", "Initialize", configResult.CurrentMessage ?? "Config provider failure");
            return;
        }

        var dataStores = new List<Fdw.Services.Connections.DataStoreConfiguration>();
        foreach (var shallow in configResult.Value!)
        {
            if (string.IsNullOrWhiteSpace(shallow.Name)) continue;
            var composed = await configProvider.Get(shallow.Name).ConfigureAwait(false);
            dataStores.Add(composed.IsSuccess && composed.Value is not null ? composed.Value : shallow);
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
            if (string.IsNullOrEmpty(ds.ServiceOptionType))
            {
                DataStoreTypesLog.DataStoreMissingServiceOptionType(logger, ds.Name);
                continue;
            }

            DataStoreTypesLog.DataStoreLoaded(logger, ds.Id, ds.Name, ds.ServiceOptionType);
        }

        foreach (var path in dataPaths.Where(p => !string.IsNullOrWhiteSpace(p.Name)))
        {
            DataStoreTypesLog.DataPathLoaded(logger, path.Id, path.Name, path.DataStoreId);
        }

        foreach (var container in dataContainers.Where(c => !string.IsNullOrWhiteSpace(c.Name)))
        {
            DataStoreTypesLog.DataContainerLoaded(logger, container.Id, container.Name, container.DataPathId);
        }

        foreach (var field in dataContainerFields.Where(f => !string.IsNullOrWhiteSpace(f.Name)))
        {
            DataStoreTypesLog.FieldLoaded(logger, field.Id, field.Name, field.DataContainerId, field.DataType ?? string.Empty);
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
    }

    // ============================================================
    // Instance Members
    // ============================================================

    private readonly ILogger<ConfigurationGatewayDataStoreProvider> _logger;

    private readonly ConfiguredDataStoreProvider _coreProvider;

    private readonly DataStoreConfigurationProvider _dataStoreConfigProvider;

    private const string ConfigurationConnectionName = "PlatformConfiguration";

    private readonly IConfigurationGatewayProvider? _gatewayProvider;

    /// <summary>
    /// Initializes a new instance of the <see cref="ConfigurationGatewayDataStoreProvider"/> class.
    /// </summary>
    public ConfigurationGatewayDataStoreProvider(
        ILogger<ConfigurationGatewayDataStoreProvider> logger,
        ConfiguredDataStoreProvider coreProvider,
        DataStoreConfigurationProvider dataStoreConfigProvider,
        IConfigurationGatewayProvider? gatewayProvider = null)
    {
        _logger = logger ?? NullLogger<ConfigurationGatewayDataStoreProvider>.Instance;
        _coreProvider = coreProvider ?? throw new ArgumentNullException(nameof(coreProvider));
        _dataStoreConfigProvider = dataStoreConfigProvider ?? throw new ArgumentNullException(nameof(dataStoreConfigProvider));
        _gatewayProvider = gatewayProvider;
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
        }

        return await _coreProvider.Get(name, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async Task<IGenericResult<IReadOnlyList<IDataStore>>> Get(CancellationToken cancellationToken = default)
    {
        DataStoreProviderLog.TraceGetAllDataStoresEntry(_logger);
        var stores = await Load(cancellationToken).ConfigureAwait(false);
        DataStoreProviderLog.AllDataStoresRetrieved(_logger, stores.Count);
        return GenericResult<IReadOnlyList<IDataStore>>.Success(stores);
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
    public async Task<IGenericResult<IDataContainer>> Get(string dataStoreName, string pathName, string containerName, CancellationToken cancellationToken = default)
    {
        var pathResult = await Get(dataStoreName, pathName, cancellationToken).ConfigureAwait(false);
        if (!pathResult.IsSuccess || pathResult.Value is null) return pathResult.ToNewResult<IDataContainer>();

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
    /// <returns>The assembled IDataStore tree.</returns>
    public async Task<IReadOnlyList<Fdw.Data.Abstractions.IDataStore>> Load(CancellationToken ct = default)
    {
        DataStoreProviderLog.LoadStarted(_logger);

        var coreResult = await _coreProvider.Get(ct).ConfigureAwait(false);
        if (!coreResult.IsSuccess)
        {
            DataStoreProviderLog.LoadFailed(
                _logger,
                new InvalidOperationException(coreResult.CurrentMessage ?? "core provider Get(all) returned failure"),
                coreResult.CurrentMessage ?? "core provider Get(all) returned failure");
            return [];
        }

        var composedStores = coreResult.Value ?? [];
        var totalPaths = composedStores.Sum(s => s.Paths.Count);
        var totalContainers = composedStores.Sum(s => s.Paths.Sum(p => p.Containers.Count));

        var result = new List<Fdw.Data.Abstractions.IDataStore>(composedStores);
        MergeConfigurationGatewayDataStores(result);
        DataStoreProviderLog.LoadCompleted(_logger, result.Count, totalPaths, totalContainers);
        return result;
    }

    private void MergeConfigurationGatewayDataStores(List<Fdw.Data.Abstractions.IDataStore> result)
    {
        if (_gatewayProvider is null) return;

        IReadOnlyList<Fdw.Data.Abstractions.IDataStore> configDbStores;
        try
        {
            var gw = _gatewayProvider.Get(ConfigurationConnectionName);
            if (gw.IsFailure) return;
            configDbStores = gw.Value!.DataStores ?? [];
        }
        catch (Exception ex)
        {
            DataStoreProviderLog.LoadFailed(_logger, ex, "ConfigurationDb DataStores read failed");
            return;
        }

        if (configDbStores.Count == 0) return;

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
    }

}
