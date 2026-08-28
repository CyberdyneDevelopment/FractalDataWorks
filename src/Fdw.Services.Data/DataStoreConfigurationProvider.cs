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
    private readonly Lazy<ImplementationConfigurationProviderBase<DataContainerConfiguration, DataContainerConfigurationCommand>> _containerProvider;

    private readonly ILogger<DataStoreConfigurationProvider> _logger;

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

        container.DataPathId = path.Id;

        var saveResult = await _containerProvider.Value.Save(container, ct).ConfigureAwait(false);
        if (saveResult.IsFailure)
            return saveResult.ToNewResult<DataContainerConfiguration>();

        DataStoreConfigurationProviderLog.ContainerAdded(_logger, container.Name, pathName, storeName);
        return GenericResult<DataContainerConfiguration>.Success(container);
    }
    /// <inheritdoc />
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
