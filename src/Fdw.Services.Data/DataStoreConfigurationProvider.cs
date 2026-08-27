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
/// <see cref="ImplementationConfigurationProviderBase{TConfig,TCommand}"/>; typed providers are registered via the
/// inherited <c>Register</c>.
/// </summary>
/// <remarks>
/// The hierarchy assembly that previously lived here (<c>AssembleHierarchy</c>) was removed
/// in Phase 6.M — it caused the ConfigurationGateway/DataStoreLoader cycle by making
/// gatewayProvider round-trips for every DataStore lookup. The IDataStore tree is now assembled
/// in memory by the per-transport <c>DataStoreBuilderBase</c> from the nested store configuration
/// (the same builder mechanism ConfigurationGateway and ConfigurationGatewayDataStoreProvider.Load feed).
/// </remarks>
public class DataStoreConfigurationProvider : ImplementationConfigurationProviderBase<DataStoreConfiguration, DataStoreConfigurationCommand>
{
    // Why: Lazy to avoid DI resolution-order cycle — the container provider is registered AFTER
    // DataStoreConfigurationProvider in RegisterDomainConfiguration, so eager resolution would fail.
    private readonly Lazy<ImplementationConfigurationProviderBase<DataContainerConfiguration, DataContainerConfigurationCommand>> _containerProvider;

    private readonly ILogger<DataStoreConfigurationProvider> _logger;

    /// <summary>
    /// Registers the DataStoreConfigurationProvider with DI, targeting this domain's own default
    /// location. To override, call <c>SetConfiguration</c> on the resolved singleton.
    /// </summary>
    public static void RegisterDomainConfiguration(IServiceCollection services)
    {
        // Why literal: the child-type providers below are plain ImplementationConfigurationProviderBase<,>
        // instances (not domain-specific subclasses), so there is no per-domain constructor default
        // to fall back on — this is the domain's own default location.

        services.TryAddSingleton<DataStoreConfigurationProvider>(sp =>
            new DataStoreConfigurationProvider(
                sp.GetService<ILogger<DataStoreConfigurationProvider>>(),
                sp.GetRequiredService<IConfigurationGatewayProvider>(),
                new Lazy<ImplementationConfigurationProviderBase<DataContainerConfiguration, DataContainerConfigurationCommand>>(
                    () => sp.GetRequiredService<ImplementationConfigurationProviderBase<DataContainerConfiguration, DataContainerConfigurationCommand>>()),
                DataStoreTypes.ConfigurationConnection, "data"));
        services.TryAddSingleton<ImplementationConfigurationProviderBase<DataStoreConfiguration, DataStoreConfigurationCommand>>(
            sp => sp.GetRequiredService<DataStoreConfigurationProvider>());
        services.TryAddSingleton<IServiceConfigurationProvider<DataStoreConfiguration>>(
            sp => sp.GetRequiredService<DataStoreConfigurationProvider>());

        // Why: Child types (DataPath/DataContainer/DataContainerField) need their own providers so
        // SchemaInformationService and MsSqlSchemaImportPersister can Save discovered schema.
        services.TryAddSingleton<ImplementationConfigurationProviderBase<DataPathConfiguration, DataPathConfigurationCommand>>(sp =>
            new ImplementationConfigurationProviderBase<DataPathConfiguration, DataPathConfigurationCommand>(
                sp.GetService<ILoggerFactory>()?.CreateLogger<ImplementationConfigurationProviderBase<DataPathConfiguration, DataPathConfigurationCommand>>()
                    ?? NullLogger<ImplementationConfigurationProviderBase<DataPathConfiguration, DataPathConfigurationCommand>>.Instance,
                sp.GetRequiredService<IConfigurationGatewayProvider>(),
                DataStoreTypes.ConfigurationConnection, "data"));

        services.TryAddSingleton<ImplementationConfigurationProviderBase<DataContainerConfiguration, DataContainerConfigurationCommand>>(sp =>
            new ImplementationConfigurationProviderBase<DataContainerConfiguration, DataContainerConfigurationCommand>(
                sp.GetService<ILoggerFactory>()?.CreateLogger<ImplementationConfigurationProviderBase<DataContainerConfiguration, DataContainerConfigurationCommand>>()
                    ?? NullLogger<ImplementationConfigurationProviderBase<DataContainerConfiguration, DataContainerConfigurationCommand>>.Instance,
                sp.GetRequiredService<IConfigurationGatewayProvider>(),
                DataStoreTypes.ConfigurationConnection, "data"));

        services.TryAddSingleton<ImplementationConfigurationProviderBase<DataContainerFieldConfiguration, DataContainerFieldConfigurationCommand>>(sp =>
            new ImplementationConfigurationProviderBase<DataContainerFieldConfiguration, DataContainerFieldConfigurationCommand>(
                sp.GetService<ILoggerFactory>()?.CreateLogger<ImplementationConfigurationProviderBase<DataContainerFieldConfiguration, DataContainerFieldConfigurationCommand>>()
                    ?? NullLogger<ImplementationConfigurationProviderBase<DataContainerFieldConfiguration, DataContainerFieldConfigurationCommand>>.Instance,
                sp.GetRequiredService<IConfigurationGatewayProvider>(),
                DataStoreTypes.ConfigurationConnection, "data"));


        // Why keys are registered with DataPath, DataContainer and DataContainerField rather than with
        // connections: a container's keys are the same kind of child of the same node, and the
        // connections collection owns transports, not the data schema. The cascade resolves a child by
        // finding the ConfigurationCommands option that claims its type, so without these a container
        // that declared a key saved as NoChildCommandForType and data.DataContainerKey stayed empty.
        services.TryAddSingleton<ImplementationConfigurationProviderBase<DataContainerKeyConfiguration, DataContainerKeyConfigurationCommand>>(sp =>
            new ImplementationConfigurationProviderBase<DataContainerKeyConfiguration, DataContainerKeyConfigurationCommand>(
                sp.GetService<ILoggerFactory>()?.CreateLogger<ImplementationConfigurationProviderBase<DataContainerKeyConfiguration, DataContainerKeyConfigurationCommand>>()
                    ?? NullLogger<ImplementationConfigurationProviderBase<DataContainerKeyConfiguration, DataContainerKeyConfigurationCommand>>.Instance,
                sp.GetRequiredService<IConfigurationGatewayProvider>(),
                DataStoreTypes.ConfigurationConnection, "data"));

        services.TryAddSingleton<ImplementationConfigurationProviderBase<DataContainerKeyFieldConfiguration, DataContainerKeyFieldConfigurationCommand>>(sp =>
            new ImplementationConfigurationProviderBase<DataContainerKeyFieldConfiguration, DataContainerKeyFieldConfigurationCommand>(
                sp.GetService<ILoggerFactory>()?.CreateLogger<ImplementationConfigurationProviderBase<DataContainerKeyFieldConfiguration, DataContainerKeyFieldConfigurationCommand>>()
                    ?? NullLogger<ImplementationConfigurationProviderBase<DataContainerKeyFieldConfiguration, DataContainerKeyFieldConfigurationCommand>>.Instance,
                sp.GetRequiredService<IConfigurationGatewayProvider>(),
                DataStoreTypes.ConfigurationConnection, "data"));

        // Why (FDW-403 slice 2): DataPathPolicy and FileTypeHandlerOverride are child tables of
        // data.DataPath using a physical FK (DataPathRowId → DataPath.RowId). Registering their
        // providers here makes them available for cascade load in FileSystemDataStoreConfigProvider
        // without the FileSystem package taking a dependency on IConfigurationGateway directly.
        services.TryAddSingleton<ImplementationConfigurationProviderBase<DataPathPolicyConfiguration, DataPathPolicyConfigurationCommand>>(sp =>
            new ImplementationConfigurationProviderBase<DataPathPolicyConfiguration, DataPathPolicyConfigurationCommand>(
                sp.GetService<ILoggerFactory>()?.CreateLogger<ImplementationConfigurationProviderBase<DataPathPolicyConfiguration, DataPathPolicyConfigurationCommand>>()
                    ?? NullLogger<ImplementationConfigurationProviderBase<DataPathPolicyConfiguration, DataPathPolicyConfigurationCommand>>.Instance,
                sp.GetRequiredService<IConfigurationGatewayProvider>(),
                DataStoreTypes.ConfigurationConnection, "data"));

        services.TryAddSingleton<ImplementationConfigurationProviderBase<FileTypeHandlerOverrideConfiguration, FileTypeHandlerOverrideConfigurationCommand>>(sp =>
            new ImplementationConfigurationProviderBase<FileTypeHandlerOverrideConfiguration, FileTypeHandlerOverrideConfigurationCommand>(
                sp.GetService<ILoggerFactory>()?.CreateLogger<ImplementationConfigurationProviderBase<FileTypeHandlerOverrideConfiguration, FileTypeHandlerOverrideConfigurationCommand>>()
                    ?? NullLogger<ImplementationConfigurationProviderBase<FileTypeHandlerOverrideConfiguration, FileTypeHandlerOverrideConfigurationCommand>>.Instance,
                sp.GetRequiredService<IConfigurationGatewayProvider>(),
                DataStoreTypes.ConfigurationConnection, "data"));
    }

    /// <summary>Initializes a new instance of the <see cref="DataStoreConfigurationProvider"/> class.</summary>
    public DataStoreConfigurationProvider(
        ILogger<DataStoreConfigurationProvider>? logger,
        IConfigurationGatewayProvider gatewayProvider,
        Lazy<ImplementationConfigurationProviderBase<DataContainerConfiguration, DataContainerConfigurationCommand>> containerProvider,
        string dataStoreName,
        string pathName = "data")
        : base(logger ?? NullLogger<DataStoreConfigurationProvider>.Instance,
               gatewayProvider,
               dataStoreName, pathName)
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
