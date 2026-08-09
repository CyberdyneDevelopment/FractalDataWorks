using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Configuration;
using Fdw.Results;
using Fdw.Services.Abstractions;
using Fdw.Services.Configuration;
using Fdw.Services.Connections;
using Fdw.Services.Connections.Commands;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Data.Commands;
using Fdw.Services.Data.Logging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Fdw.Services.Data;

/// <summary>
/// Domain-specific configuration provider for DataStore configurations.
/// The polymorphic typed-body read (dispatch on <see cref="DataStoreConfiguration.ServiceOptionType"/> to
/// load the typed body row, e.g. <c>data.MsSqlDataStore</c>, and attach it to
/// <see cref="DataStoreConfiguration.Configuration"/>) is composed uniformly by
/// <see cref="DefaultConfigurationProvider{TConfig,TCommand}"/>; typed providers are registered via the
/// inherited <c>Register</c>. Unlike most domains, a missing typed provider is not a defect
/// here — see <see cref="OnNoTypedProvider"/> for the body-less-store rule.
/// </summary>
/// <remarks>
/// The hierarchy assembly that previously lived here (<c>AssembleHierarchy</c>) was removed
/// in Phase 6.M — it caused the ConfigurationGateway/DataStoreLoader cycle by making
/// gateway round-trips for every DataStore lookup. The IDataStore tree is now assembled
/// in memory by the per-transport <c>DataStoreBuilderBase</c> from the nested store configuration
/// (the same builder mechanism ConfigurationGateway and ConfigurationGatewayDataStoreProvider.Load feed).
/// </remarks>
public class DataStoreConfigurationProvider : DefaultConfigurationProvider<DataStoreConfiguration, DataStoreConfigurationCommand>
{
    // Why: Lazy to avoid DI resolution-order cycle — the container provider is registered AFTER
    // DataStoreConfigurationProvider in RegisterDomainConfiguration, so eager resolution would fail.
    private readonly Lazy<DefaultConfigurationProvider<DataContainerConfiguration, DataContainerConfigurationCommand>> _containerProvider;

    private readonly ILogger<DataStoreConfigurationProvider> _logger;

    /// <summary>
    /// Registers the DataStoreConfigurationProvider with DI, targeting this domain's own default
    /// location. To override, call <c>SetConfiguration</c> on the resolved singleton.
    /// </summary>
    public static void RegisterDomainConfiguration(IServiceCollection services)
    {
        // Why literal: the child-type providers below are plain DefaultConfigurationProvider<,>
        // instances (not domain-specific subclasses), so there is no per-domain constructor default
        // to fall back on — this is the domain's own default location.
        const string dataStoreName = "ConfigurationDb";
        const string pathName = "data";

        services.TryAddSingleton<DataStoreConfigurationProvider>(sp =>
            new DataStoreConfigurationProvider(
                sp.GetService<ILogger<DataStoreConfigurationProvider>>(),
                sp.GetRequiredService<Lazy<IConfigurationGateway>>(),
                new Lazy<DefaultConfigurationProvider<DataContainerConfiguration, DataContainerConfigurationCommand>>(
                    () => sp.GetRequiredService<DefaultConfigurationProvider<DataContainerConfiguration, DataContainerConfigurationCommand>>()),
                invalidator: new Lazy<ICacheInvalidator?>(() => sp.GetService<ICacheInvalidator>())));
        services.TryAddSingleton<DefaultConfigurationProvider<DataStoreConfiguration, DataStoreConfigurationCommand>>(
            sp => sp.GetRequiredService<DataStoreConfigurationProvider>());
        services.TryAddSingleton<IServiceConfigurationProvider<DataStoreConfiguration>>(
            sp => sp.GetRequiredService<DataStoreConfigurationProvider>());

        // Why: Child types (DataPath/DataContainer/DataContainerField) need their own providers so
        // SchemaInformationService and MsSqlSchemaImportPersister can Save discovered schema.
        services.TryAddSingleton<DefaultConfigurationProvider<DataPathConfiguration, DataPathConfigurationCommand>>(sp =>
            new DefaultConfigurationProvider<DataPathConfiguration, DataPathConfigurationCommand>(
                sp.GetService<ILoggerFactory>()?.CreateLogger<DefaultConfigurationProvider<DataPathConfiguration, DataPathConfigurationCommand>>()
                    ?? NullLogger<DefaultConfigurationProvider<DataPathConfiguration, DataPathConfigurationCommand>>.Instance,
                sp.GetRequiredService<Lazy<IConfigurationGateway>>(),
                dataStoreName, pathName,
                new Lazy<ICacheInvalidator?>(() => sp.GetService<ICacheInvalidator>())));

        services.TryAddSingleton<DefaultConfigurationProvider<DataContainerConfiguration, DataContainerConfigurationCommand>>(sp =>
            new DefaultConfigurationProvider<DataContainerConfiguration, DataContainerConfigurationCommand>(
                sp.GetService<ILoggerFactory>()?.CreateLogger<DefaultConfigurationProvider<DataContainerConfiguration, DataContainerConfigurationCommand>>()
                    ?? NullLogger<DefaultConfigurationProvider<DataContainerConfiguration, DataContainerConfigurationCommand>>.Instance,
                sp.GetRequiredService<Lazy<IConfigurationGateway>>(),
                dataStoreName, pathName,
                new Lazy<ICacheInvalidator?>(() => sp.GetService<ICacheInvalidator>())));

        services.TryAddSingleton<DefaultConfigurationProvider<DataContainerFieldConfiguration, DataContainerFieldConfigurationCommand>>(sp =>
            new DefaultConfigurationProvider<DataContainerFieldConfiguration, DataContainerFieldConfigurationCommand>(
                sp.GetService<ILoggerFactory>()?.CreateLogger<DefaultConfigurationProvider<DataContainerFieldConfiguration, DataContainerFieldConfigurationCommand>>()
                    ?? NullLogger<DefaultConfigurationProvider<DataContainerFieldConfiguration, DataContainerFieldConfigurationCommand>>.Instance,
                sp.GetRequiredService<Lazy<IConfigurationGateway>>(),
                dataStoreName, pathName,
                new Lazy<ICacheInvalidator?>(() => sp.GetService<ICacheInvalidator>())));

        // Why (FDW-403 slice 2): DataPathPolicy and FileTypeHandlerOverride are child tables of
        // data.DataPath using a physical FK (DataPathRowId → DataPath.RowId). Registering their
        // providers here makes them available for cascade load in FileSystemDataStoreConfigProvider
        // without the FileSystem package taking a dependency on IConfigurationGateway directly.
        services.TryAddSingleton<DefaultConfigurationProvider<DataPathPolicyConfiguration, DataPathPolicyConfigurationCommand>>(sp =>
            new DefaultConfigurationProvider<DataPathPolicyConfiguration, DataPathPolicyConfigurationCommand>(
                sp.GetService<ILoggerFactory>()?.CreateLogger<DefaultConfigurationProvider<DataPathPolicyConfiguration, DataPathPolicyConfigurationCommand>>()
                    ?? NullLogger<DefaultConfigurationProvider<DataPathPolicyConfiguration, DataPathPolicyConfigurationCommand>>.Instance,
                sp.GetRequiredService<Lazy<IConfigurationGateway>>(),
                dataStoreName, pathName,
                new Lazy<ICacheInvalidator?>(() => sp.GetService<ICacheInvalidator>())));

        services.TryAddSingleton<DefaultConfigurationProvider<FileTypeHandlerOverrideConfiguration, FileTypeHandlerOverrideConfigurationCommand>>(sp =>
            new DefaultConfigurationProvider<FileTypeHandlerOverrideConfiguration, FileTypeHandlerOverrideConfigurationCommand>(
                sp.GetService<ILoggerFactory>()?.CreateLogger<DefaultConfigurationProvider<FileTypeHandlerOverrideConfiguration, FileTypeHandlerOverrideConfigurationCommand>>()
                    ?? NullLogger<DefaultConfigurationProvider<FileTypeHandlerOverrideConfiguration, FileTypeHandlerOverrideConfigurationCommand>>.Instance,
                sp.GetRequiredService<Lazy<IConfigurationGateway>>(),
                dataStoreName, pathName,
                new Lazy<ICacheInvalidator?>(() => sp.GetService<ICacheInvalidator>())));
    }

    /// <summary>Initializes a new instance of the <see cref="DataStoreConfigurationProvider"/> class.</summary>
    public DataStoreConfigurationProvider(
        ILogger<DataStoreConfigurationProvider>? logger,
        Lazy<IConfigurationGateway> lazyGateway,
        Lazy<DefaultConfigurationProvider<DataContainerConfiguration, DataContainerConfigurationCommand>> containerProvider,
        string dataStoreName = "ConfigurationDb",
        string pathName = "data",
        Lazy<ICacheInvalidator?>? invalidator = null)
        : base(logger ?? NullLogger<DataStoreConfigurationProvider>.Instance,
               lazyGateway,
               dataStoreName, pathName,
               invalidator)
    {
        _logger = logger ?? NullLogger<DataStoreConfigurationProvider>.Instance;
        _containerProvider = containerProvider ?? throw new ArgumentNullException(nameof(containerProvider));
    }

    /// <summary>
    /// Adds a new container to the specified path within the named DataStore.
    /// Enforces store-exists, path-exists, and no-duplicate-container-name invariants before persisting.
    /// </summary>
    public async Task<IGenericResult<DataContainerConfiguration>> AddContainer(
        string storeName,
        string pathName,
        DataContainerConfiguration container,
        CancellationToken ct = default)
    {
        // Why: Get now cascades the full Paths → Containers hierarchy (no separate GetWithChildren verb),
        // so the store-exists / path-exists / duplicate-container guards below have the data they need.
        var storeResult = await Get(storeName, ct).ConfigureAwait(false);
        if (!storeResult.IsSuccess || storeResult.Value is null)
        {
            return GenericResult<DataContainerConfiguration>.Failure(
                DataStoreConfigurationProviderLog.StoreNotFoundForAddContainer(_logger, storeName));
        }

        var store = storeResult.Value;
        var path = store.Paths.FirstOrDefault(p => string.Equals(p.Name, pathName, StringComparison.Ordinal));
        if (path is null)
        {
            return GenericResult<DataContainerConfiguration>.Failure(
                DataStoreConfigurationProviderLog.PathNotFoundForAddContainer(_logger, pathName, storeName));
        }

        if (path.Containers.Any(c => string.Equals(c.Name, container.Name, StringComparison.Ordinal)))
        {
            return GenericResult<DataContainerConfiguration>.Failure(
                DataStoreConfigurationProviderLog.ContainerAlreadyExists(_logger, container.Name, pathName, storeName));
        }

        // Why: stamp only the LOGICAL parent FK (DataPathId). The physical DataPathRowId is RowId-invisible
        // (DB-managed IDENTITY, not a POCO property) — the save translator resolves it by subquery on this
        // DataPathId at insert time.
        container.DataPathId = path.Id;

        var saveResult = await _containerProvider.Value.Save(container, ct).ConfigureAwait(false);
        if (saveResult.IsFailure)
            return saveResult.ToNewResult<DataContainerConfiguration>();

        // Why: no DataStore-tree invalidation needed — the eager full-tree singleton is deleted.
        // Runtime container lookups go through ConfigurationGatewayDataStoreProvider.GetContainer over DataGatewayService
        // (caching built in); the base Save path already tag-invalidates so the new container surfaces
        // on the next read.
        DataStoreConfigurationProviderLog.ContainerAdded(_logger, container.Name, pathName, storeName);
        return GenericResult<DataContainerConfiguration>.Success(container);
    }

    /// <inheritdoc />
    // Why: a DataStore's ServiceOptionType is its CONNECTION's transport (MsSql, FileSystem, Http,
    // PostgreSql, ...), set at create time. Only transports with extra typed-body columns (MsSql,
    // FileSystem) register a typed provider in their DataStoreType.RegisterFactory; transports with NO
    // datastore-specific columns (Http, and the body-less Rest/Soap/File) carry the full configuration
    // on the header row itself. So "no typed provider registered" reliably means "body-less store" —
    // return the header as the complete config rather than failing loud (the base default). A genuinely
    // typed store cannot reach here because it always registers its provider before reads occur. This is
    // a well-defined domain rule (overriding the base fail-loud), NOT a value fallback.
    protected override IGenericResult<DataStoreConfiguration> OnNoTypedProvider(DataStoreConfiguration header)
    {
        DataStoreConfigurationProviderLog.HeaderIsTypedBody(_logger, header.Name, header.ServiceOptionType!);
        return GenericResult<DataStoreConfiguration>.Success(header);
    }

    /// <inheritdoc />
    // Why (FDW-558): the base Get(CancellationToken) returns bare header rows (by design — other
    // domains, e.g. lineage, rely on the cheap flat list). But DataStore's list DTO
    // (ListDataStoresEndpointBase.MapToSummary) computes PathCount/ContainerCount by counting
    // config.Paths/config.Paths[].Containers — which are empty on a bare header, so every store showed
    // 0/0. Scoped fix: override HERE (not the base) to compose each header into its full aggregate via
    // the inherited ComposeAggregate hook (reuses ComposeTypedBody + the SAME ComposeChildren recursion
    // Get(string)/Get(Guid) already use — no duplicated compose logic, and it inherits whatever the
    // Container->Field cascade already does correctly). One compose failure fails the whole list — no
    // partial/fallback result (NO FALLBACKS WITHOUT EXPLICIT APPROVAL).
    public override async Task<IGenericResult<IReadOnlyList<DataStoreConfiguration>>> Get(CancellationToken ct = default)
    {
        DataStoreConfigurationProviderLog.ComposingDataStoreList(_logger);
        var headers = await base.Get(ct).ConfigureAwait(false);
        if (!headers.IsSuccess || headers.Value is null) return headers;

        var composed = new List<DataStoreConfiguration>(headers.Value.Count);
        foreach (var header in headers.Value)
        {
            var aggregate = await ComposeAggregate(header, ct).ConfigureAwait(false);
            if (!aggregate.IsSuccess || aggregate.Value is null)
                return GenericResult<IReadOnlyList<DataStoreConfiguration>>.Failure(
                    DataStoreConfigurationProviderLog.DataStoreListComposeFailed(_logger, header.Name));
            composed.Add(aggregate.Value);
        }

        DataStoreConfigurationProviderLog.DataStoreListComposed(_logger, composed.Count);
        return GenericResult<IReadOnlyList<DataStoreConfiguration>>.Success(composed);
    }
}
