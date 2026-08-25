using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Data.Abstractions;
using Fdw.Results;
using Fdw.Services.Abstractions;
using Fdw.Services.Connections;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Data.Logging;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Services.Data;

/// <summary>
/// Resolves DataStores as the canonical navigable <see cref="IDataNode"/> tree, from a configuration
/// source and a per-transport builder selector only.
/// </summary>
/// <remarks>
/// Why: this is the PURE counterpart to <c>Fdw.Services.Data.DataStoreProvider</c> — it has no
/// dependency on <c>IDataConnectionProvider</c> or <c>IConfigurationGateway</c> (both excluded from
/// <c>Fdw.Data.DataNodes</c>), so it never merges in ConfigurationDb's own gateway-owned DataStores and
/// never resolves connections directly. Config reads go through the abstract
/// <see cref="IServiceConfigurationProvider{TConfig}"/> and transport dispatch through
/// <see cref="IDataStoreBuilderSelector"/> — both supplied by the caller, which CAN reference the
/// excluded packages. Each store is built once by its selected <c>IDataStoreBuilder</c> from the
/// cascaded <c>DataStoreConfiguration</c> (Paths → Containers → Fields); path and container lookups
/// dot-walk the built tree, mirroring <c>DataStoreProvider</c>'s instance members minus the gateway
/// shortcut branches.
/// </remarks>
public sealed class ConfiguredDataStoreProvider : IDataStoreProvider
{
    private readonly ILogger<ConfiguredDataStoreProvider> _logger;
    private readonly IServiceConfigurationProvider<DataStoreConfiguration> _configurationProvider;
    private readonly IDataStoreBuilderSelector _builderSelector;

    /// <summary>
    /// Initializes a new instance of the <see cref="ConfiguredDataStoreProvider"/> class.
    /// </summary>
    /// <param name="logger">Logger for provider diagnostics.</param>
    /// <param name="configurationProvider">The DataStore configuration source.</param>
    /// <param name="builderSelector">Selects the per-transport <see cref="IDataStoreBuilder"/> for a resolved configuration.</param>
    public ConfiguredDataStoreProvider(
        ILogger<ConfiguredDataStoreProvider>? logger,
        IServiceConfigurationProvider<DataStoreConfiguration> configurationProvider,
        IDataStoreBuilderSelector builderSelector)
    {
        // Why: NullLogger<T>.Instance is the only sanctioned ?? fallback (functional without DI logging).
        _logger = logger ?? NullLogger<ConfiguredDataStoreProvider>.Instance;
        ArgumentNullException.ThrowIfNull(configurationProvider);
        ArgumentNullException.ThrowIfNull(builderSelector);
        _configurationProvider = configurationProvider;
        _builderSelector = builderSelector;
    }

    /// <inheritdoc/>
    public async Task<IGenericResult<IDataStore>> Get(string name, CancellationToken cancellationToken = default)
    {
        ConfiguredDataStoreProviderLog.TraceGetByNameEntry(_logger, name);
        if (string.IsNullOrWhiteSpace(name))
            return GenericResult<IDataStore>.Failure(ConfiguredDataStoreProviderLog.StoreNameRequired(_logger));

        var cfgResult = await _configurationProvider.Get(name, cancellationToken).ConfigureAwait(false);
        if (!cfgResult.IsSuccess || cfgResult.Value is null)
            return GenericResult<IDataStore>.Failure(ConfiguredDataStoreProviderLog.StoreNotFound(_logger, name));

        var buildResult = await BuildStore(cfgResult.Value, cancellationToken).ConfigureAwait(false);
        if (buildResult.IsSuccess)
            ConfiguredDataStoreProviderLog.StoreBuilt(_logger, name, cfgResult.Value.ServiceOptionType ?? "(none)");
        return buildResult;
    }

    /// <inheritdoc/>
    public async Task<IGenericResult<IDataStore>> Get(Guid id, CancellationToken cancellationToken = default)
    {
        ConfiguredDataStoreProviderLog.TraceGetByIdEntry(_logger, id);
        var cfgResult = await _configurationProvider.Get(id, cancellationToken).ConfigureAwait(false);
        if (!cfgResult.IsSuccess || cfgResult.Value is null || string.IsNullOrWhiteSpace(cfgResult.Value.Name))
            return GenericResult<IDataStore>.Failure(ConfiguredDataStoreProviderLog.StoreByIdNotFound(_logger, id));

        return await Get(cfgResult.Value.Name, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async Task<IGenericResult<IReadOnlyList<IDataStore>>> Get(CancellationToken cancellationToken = default)
    {
        ConfiguredDataStoreProviderLog.TraceGetAllEntry(_logger);
        var configResult = await _configurationProvider.Get(cancellationToken).ConfigureAwait(false);
        if (!configResult.IsSuccess)
            return GenericResult<IReadOnlyList<IDataStore>>.Failure(ConfiguredDataStoreProviderLog.LoadAllFailed(_logger));

        // Why: the list Get() returns shallow headers; compose each store's full aggregate
        // (Paths→Containers→Fields) via the base provider's single Get(name), whose recursive mapper-FK
        // cascade populates the children — mirrors DataStoreProvider.Load minus the gateway merge.
        var shallowConfigs = configResult.Value ?? [];
        var stores = new List<IDataStore>(shallowConfigs.Count);
        foreach (var shallowCfg in shallowConfigs)
        {
            if (string.IsNullOrWhiteSpace(shallowCfg.Name))
                continue;

            var composed = await _configurationProvider.Get(shallowCfg.Name, cancellationToken).ConfigureAwait(false);
            var buildResult = await BuildStore(
                composed.IsSuccess && composed.Value is not null ? composed.Value : shallowCfg,
                cancellationToken).ConfigureAwait(false);
            if (buildResult.IsSuccess && buildResult.Value is not null)
            {
                stores.Add(buildResult.Value);
            }
            else
            {
                // Why: a single store failing to build must not abort the whole list (partial-success
                // aggregate), but the failure is OBSERVED here rather than silently dropped — the
                // per-store builder/selector already logged the cause; this records which store the
                // aggregate omitted so the gap is traceable.
                ConfiguredDataStoreProviderLog.StoreSkippedInLoad(_logger, shallowCfg.Name);
            }
        }

        ConfiguredDataStoreProviderLog.AllStoresRetrieved(_logger, stores.Count);
        return GenericResult<IReadOnlyList<IDataStore>>.Success(stores);
    }

    /// <inheritdoc/>
    public async Task<IGenericResult<IDataNodePath>> Get(string dataStoreName, string pathName, CancellationToken cancellationToken = default)
    {
        ConfiguredDataStoreProviderLog.TraceGetPathEntry(_logger, dataStoreName, pathName);
        var storeResult = await Get(dataStoreName, cancellationToken).ConfigureAwait(false);
        if (!storeResult.IsSuccess || storeResult.Value is null)
            return storeResult.ToNewResult<IDataNodePath>();

        return storeResult.Value.Path(pathName);
    }

    /// <inheritdoc/>
    public async Task<IGenericResult<IDataContainer>> Get(string dataStoreName, string pathName, string containerName, CancellationToken cancellationToken = default)
    {
        ConfiguredDataStoreProviderLog.TraceGetContainerEntry(_logger, dataStoreName, pathName, containerName);
        var pathResult = await Get(dataStoreName, pathName, cancellationToken).ConfigureAwait(false);
        if (!pathResult.IsSuccess || pathResult.Value is null)
            return pathResult.ToNewResult<IDataContainer>();

        return pathResult.Value.Container(containerName);
    }

    // Why: resolve the store's transport builder via the injected selector, Configure with the
    // resolved store config, and Build the IDataNode tree. Selector/Configure/Build failures are
    // already MessageLogging-instrumented by their own implementations, so they propagate via
    // ToNewResult rather than being re-logged here (no double-logging).
    private async Task<IGenericResult<IDataStore>> BuildStore(DataStoreConfiguration storeCfg, CancellationToken cancellationToken)
    {
        var selectResult = _builderSelector.Select(storeCfg, _logger);
        if (!selectResult.IsSuccess || selectResult.Value is null)
            return selectResult.ToNewResult<IDataStore>();

        var configureResult = selectResult.Value.Configure(storeCfg);
        if (!configureResult.IsSuccess)
            return configureResult.ToNewResult<IDataStore>();

        return await selectResult.Value.Build(cancellationToken).ConfigureAwait(false);
    }
}
