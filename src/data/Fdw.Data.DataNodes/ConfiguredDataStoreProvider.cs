using Fdw.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
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
/// <see cref="IDomainConfigurationProvider{TImplementationConfiguration}"/> and transport dispatch through
/// <see cref="IDataStoreBuilderSelector"/> — both supplied by the caller, which CAN reference the
/// excluded packages. Each store is built once by its selected <c>IDataStoreBuilder</c> from the
/// cascaded <c>IDataStoreImplementationConfiguration</c> (Paths → Containers → Fields); path and container lookups
/// dot-walk the built tree, mirroring <c>DataStoreProvider</c>'s instance members minus the gateway
/// shortcut branches.
/// </remarks>
public sealed class ConfiguredDataStoreProvider : IDataStoreProvider
{
    private readonly ILogger<ConfiguredDataStoreProvider> _logger;
    private readonly IDomainConfigurationProvider<IDataStoreImplementationConfiguration> _configurationProvider;
    private readonly IDataStoreBuilderSelector _builderSelector;

    /// <summary>
    /// Initializes a new instance of the <see cref="ConfiguredDataStoreProvider"/> class.
    /// </summary>
    /// <param name="logger">Logger for provider diagnostics.</param>
    /// <param name="configurationProvider">The DataStore configuration source.</param>
    /// <param name="builderSelector">Selects the per-transport <see cref="IDataStoreBuilder"/> for a resolved configuration.</param>
    public ConfiguredDataStoreProvider(
        ILogger<ConfiguredDataStoreProvider>? logger,
        IDomainConfigurationProvider<IDataStoreImplementationConfiguration> configurationProvider,
        IDataStoreBuilderSelector builderSelector)
    {
        _logger = logger ?? NullLogger<ConfiguredDataStoreProvider>.Instance;
        _configurationProvider = configurationProvider;
        _builderSelector = builderSelector;
    }

    /// <inheritdoc/>
    /// <remarks>
    /// The store's own row, read at the record level (no paths, containers or fields), so a store of any size costs one statement. This core
    /// provider merges in no host-declared store, so what it answers is never declared by the host.
    /// </remarks>
    public async Task<IGenericResult<DataStoreReference>> GetReference(string name, CancellationToken cancellationToken = default)
    {
        ConfiguredDataStoreProviderLog.TraceGetByNameEntry(_logger, name);
        if (string.IsNullOrWhiteSpace(name))
            return GenericResult<DataStoreReference>.Failure(ConfiguredDataStoreProviderLog.StoreNameRequired(_logger));

        var cfgResult = await _configurationProvider.Get(name, ConfigurationLevels.RecordOnly, cancellationToken).ConfigureAwait(false);

        // As Get(name) does (FDW-4281): a read that failed for a reason other than the row being absent is not "no such store", and a caller
        // that asks whether a store is the host's (the ETL gateway's guard) must not read a broken lookup as a negative answer.
        if (cfgResult.IsFailure && !FailureCategory.IsMissing(cfgResult))
            return cfgResult.ToNewResult<DataStoreReference>();

        if (!cfgResult.IsSuccess || cfgResult.Value is null)
            return GenericResult<DataStoreReference>.Failure(ConfiguredDataStoreProviderLog.StoreNotFound(_logger, name));

        return GenericResult<DataStoreReference>.Success(new DataStoreReference(cfgResult.Value.Name, cfgResult.Value.ConnectionId, false));
    }

    /// <inheritdoc/>
    public async Task<IGenericResult<IDataStore>> Get(string name, CancellationToken cancellationToken = default)
    {
        ConfiguredDataStoreProviderLog.TraceGetByNameEntry(_logger, name);
        if (string.IsNullOrWhiteSpace(name))
            return GenericResult<IDataStore>.Failure(ConfiguredDataStoreProviderLog.StoreNameRequired(_logger));

        var cfgResult = await _configurationProvider.Get(name, cancellationToken).ConfigureAwait(false);

        // A read that failed for a reason other than the row being absent is not "no such store": answering StoreNotFound would let a
        // caller that asks whether a store is the host's (the ETL gateway's guard) read a broken lookup as a negative answer (FDW-4281).
        if (cfgResult.IsFailure && !FailureCategory.IsMissing(cfgResult))
            return cfgResult.ToNewResult<IDataStore>();

        if (!cfgResult.IsSuccess || cfgResult.Value is null)
            return GenericResult<IDataStore>.Failure(ConfiguredDataStoreProviderLog.StoreNotFound(_logger, name));

        var buildResult = await BuildStore(cfgResult.Value, cancellationToken).ConfigureAwait(false);
        if (buildResult.IsSuccess)
            ConfiguredDataStoreProviderLog.StoreBuilt(_logger, name, cfgResult.Value.GetType().Name);
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
    public Task<IGenericResult<IDataContainer>> Get(string dataStoreName, string pathName, string containerName, CancellationToken cancellationToken = default)
        => Get(dataStoreName, pathName, containerName, [], cancellationToken);

    /// <inheritdoc/>
    /// <remarks>
    /// Reads the named containers, then the containers their keys refer to, each with its fields, keys and key fields in a statement per table (FDW-4254), and builds
    /// those only. Container names are unique within a store, so a name finds its container whatever path it is in.
    /// </remarks>
    public async Task<IGenericResult<IDataContainer>> Get(
        string dataStoreName, string pathName, string containerName, IReadOnlyCollection<string> joinedContainers, CancellationToken cancellationToken = default)
    {
        ConfiguredDataStoreProviderLog.TraceGetContainerEntry(_logger, dataStoreName, pathName, containerName);
        if (string.IsNullOrWhiteSpace(dataStoreName))
            return GenericResult<IDataContainer>.Failure(ConfiguredDataStoreProviderLog.StoreNameRequired(_logger));

        var named = new[] { containerName }.Concat(joinedContainers).Where(n => !string.IsNullOrWhiteSpace(n)).Distinct(StringComparer.Ordinal).ToList();
        var slice = await ReadSlice(dataStoreName, named, cancellationToken).ConfigureAwait(false);
        if (slice.IsFailure)
            return slice.ToNewResult<IDataContainer>();

        var built = await BuildSlice(slice.Value!, named, cancellationToken).ConfigureAwait(false);
        if (built.IsFailure || built.Value is null)
            return built.ToNewResult<IDataContainer>();

        var path = built.Value.Path(pathName);
        if (path.IsFailure || path.Value is null)
            return path.ToNewResult<IDataContainer>();

        return path.Value.Container(containerName);
    }

    // The store as configuration, holding the named containers and the containers their keys refer to, and no other (FDW-4328).
    private async Task<IGenericResult<IDataStoreImplementationConfiguration>> ReadSlice(
        string dataStoreName, IReadOnlyList<string> named, CancellationToken cancellationToken)
    {
        var first = await ReadNamed(dataStoreName, named, cancellationToken).ConfigureAwait(false);
        if (first.IsFailure)
            return first;

        var referenced = first.Value!.Paths
            .SelectMany(path => path.Containers)
            .SelectMany(container => container.Keys)
            .Select(key => key.ReferencedContainerName)
            .Where(name => !string.IsNullOrEmpty(name) && !named.Contains(name!, StringComparer.Ordinal))
            .Select(name => name!)
            .Distinct(StringComparer.Ordinal)
            .ToList();
        if (referenced.Count == 0)
            return first;

        var second = await ReadNamed(dataStoreName, referenced, cancellationToken).ConfigureAwait(false);
        if (second.IsFailure)
            return second;

        foreach (var path in second.Value!.Paths.Where(p => p.Containers.Count > 0))
        {
            var held = first.Value.Paths.FirstOrDefault(p => string.Equals(p.Name, path.Name, StringComparison.Ordinal));
            if (held is null)
                first.Value.Paths.Add(path);
            else
                held.Containers.AddRange(path.Containers);
        }

        return first;
    }

    // The store with only the containers of these names (in any path) composed under it: a read narrowed on the container's name.
    private async Task<IGenericResult<IDataStoreImplementationConfiguration>> ReadNamed(
        string dataStoreName, IReadOnlyList<string> names, CancellationToken cancellationToken)
    {
        var cfgResult = await _configurationProvider
            .Get(dataStoreName, ConfigurationLevels.WholeAggregate, new ChildNarrowing(typeof(DataContainerConfiguration), "Name", names), cancellationToken)
            .ConfigureAwait(false);

        // As Get(name) does (FDW-4281): a read that failed for a reason other than the row being absent is not "no such store".
        if (cfgResult.IsFailure && !FailureCategory.IsMissing(cfgResult))
            return cfgResult;

        if (!cfgResult.IsSuccess || cfgResult.Value is null)
            return GenericResult<IDataStoreImplementationConfiguration>.Failure(ConfiguredDataStoreProviderLog.StoreNotFound(_logger, dataStoreName));

        return cfgResult;
    }

    private async Task<IGenericResult<IDataStore>> BuildSlice(
        IDataStoreImplementationConfiguration slice, IReadOnlyCollection<string> named, CancellationToken cancellationToken)
    {
        var selectResult = _builderSelector.Select(slice, _logger);
        if (!selectResult.IsSuccess || selectResult.Value is null)
            return selectResult.ToNewResult<IDataStore>();

        var configureResult = selectResult.Value.Configure(slice);
        if (!configureResult.IsSuccess)
            return configureResult.ToNewResult<IDataStore>();

        selectResult.Value.DeclareSlice(named);
        return await selectResult.Value.Build(cancellationToken).ConfigureAwait(false);
    }

    private async Task<IGenericResult<IDataStore>> BuildStore(IDataStoreImplementationConfiguration storeCfg, CancellationToken cancellationToken)
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
