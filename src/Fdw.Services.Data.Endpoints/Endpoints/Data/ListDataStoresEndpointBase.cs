using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.Services.Abstractions;
using Fdw.Services.Connections;
using Fdw.Services.Data;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Data.Configuration;
using Fdw.Web.RestEndpoints.Crud;
using Fdw.Web.RestEndpoints.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Data.Endpoints;

/// <summary>
/// Base endpoint for listing configured data stores, one page at a time.
/// </summary>
/// <remarks>
/// A page is one read of the data stores domain table: the fields a list shows live on the domain row, so no
/// store's implementation, paths, containers or fields are read to list it (FDW-1003). The connections the
/// page's stores name are read once for the whole page, from the connection domain's own provider.
/// <para>
/// A store the host declares in configurationSchema.json is the platform's own and is never listed: the request-facing
/// data gateway refuses requests over it (FDW-1068), so a row that bears its name could only mislead. Which stores those
/// are is read off the stores the configuration gateway built (<c>IDataStore.DeclaredByHost</c>), not from a list of names.
/// </para>
/// </remarks>
public abstract class ListDataStoresEndpointBase
    : CrudPageEndpointBase<DataStoreDomainConfiguration, DataStoreSummaryResponse>
{
    private readonly DataStoreConfigurationProvider _dataStoreProvider;
    private readonly ConnectionNames _connectionNames;
    private readonly IConfigurationGatewayProvider _configurationGateways;

    // The names of the stores the host declared, read once per page from the stores themselves.
    private HashSet<string>? _hostDeclared;

    // What this request's page found for the connections it names. An endpoint serves one request, so the
    // page that fills this is the page its summaries are written from.
    private ConnectionNameLookup? _connectionNameLookup;

    /// <summary>Initializes a new instance of the <see cref="ListDataStoresEndpointBase"/> class.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="dataStoreProvider">Reads the page of data stores.</param>
    /// <param name="connectionProvider">Reads the connections the page's stores name, for their names.</param>
    /// <param name="authorization">Asks whether the caller may read connections, before the summary names one.</param>
    /// <param name="configurationGateways">Holds the stores the host declared, which are never listed.</param>
    protected ListDataStoresEndpointBase(
        ILogger<ListDataStoresEndpointBase> logger,
        DataStoreConfigurationProvider dataStoreProvider,
        ConnectionConfigurationProvider connectionProvider,
        IAuthorizationService authorization,
        IConfigurationGatewayProvider configurationGateways) : base(logger)
    {
        _dataStoreProvider = dataStoreProvider ?? throw new ArgumentNullException(nameof(dataStoreProvider));
        _configurationGateways = configurationGateways ?? throw new ArgumentNullException(nameof(configurationGateways));
        _connectionNames = new ConnectionNames(connectionProvider, authorization, logger);
    }

    /// <summary>Gets the resource name used for route and policy generation.</summary>
    protected override string ResourceName => "datastores";

    /// <inheritdoc />
    protected override async Task<IGenericResult<ConfigurationPage<DataStoreDomainConfiguration>>> LoadPage(
        PaginatedListRequest paging, CancellationToken ct)
    {
        var page = await _dataStoreProvider.Get(new DomainQuery(null, null, paging.Skip, paging.Take, false), ct).ConfigureAwait(false);
        if (!page.IsSuccess)
            return page;

        // The rows are already read; a page is at most one page of them. Their connections are named in one query.
        var stores = await page.Value!.Items.ToListAsync(ct).ConfigureAwait(false);
        if (page.Value.Outcome.IsFailure)
            return page.Value.Outcome.ToNewResult<ConfigurationPage<DataStoreDomainConfiguration>>();

        var names = await _connectionNames.Read(
            stores.Select(store => store.ConnectionId).ToList(), HttpContext.User, ct).ConfigureAwait(false);
        if (!names.IsSuccess)
            return names.ToNewResult<ConfigurationPage<DataStoreDomainConfiguration>>();

        _connectionNameLookup = names.Value;

        var hostDeclared = HostDeclaredDataStores.Names(_configurationGateways);
        if (hostDeclared.IsFailure)
            return hostDeclared.ToNewResult<ConfigurationPage<DataStoreDomainConfiguration>>();

        _hostDeclared = hostDeclared.Value!;
        return GenericResult<ConfigurationPage<DataStoreDomainConfiguration>>.Success(new ConfigurationPage<DataStoreDomainConfiguration>(
            page.Value.Skip, page.Value.Take, page.Value.HasMore, _ => stores.ToAsyncEnumerable(), page.Value.Total));
    }

    /// <summary>
    /// Leaves out stores with no name or implementation, rows naming the domain as their implementation, and any row that
    /// bears the name of a store the host declared.
    /// </summary>
    /// <param name="item">The store.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>True to list the store.</returns>
    protected override ValueTask<bool> Include(DataStoreDomainConfiguration item, CancellationToken ct)
    {
        var hostDeclared = _hostDeclared
            ?? throw new InvalidOperationException("A data store was filtered before its page was loaded.");

        return ValueTask.FromResult(!string.IsNullOrWhiteSpace(item.Name)
            && !string.IsNullOrWhiteSpace(item.Implementation)
            && !string.Equals(item.Implementation, "DataStore", StringComparison.OrdinalIgnoreCase)
            && !hostDeclared.Contains(item.Name));
    }

    /// <summary>Maps a data store to its summary, naming the connection it is reached through.</summary>
    /// <param name="item">The store, as the data stores domain table holds it.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>The summary.</returns>
    /// <remarks>
    /// Connections are for administrators to see: the name is there only when the caller may read connections
    /// and the row is visible to them, and is left out otherwise. A connection that could not be READ failed
    /// the page before it started (see <see cref="LoadPage"/>) rather than listing a store with an empty name.
    /// </remarks>
    protected override Task<IGenericResult<DataStoreSummaryResponse>> ToSummary(DataStoreDomainConfiguration item, CancellationToken ct)
    {
        var names = _connectionNameLookup
            ?? throw new InvalidOperationException("A data store summary was written before its page was loaded.");

        return Task.FromResult(GenericResult<DataStoreSummaryResponse>.Success(new DataStoreSummaryResponse
        {
            Id = item.Id,
            Name = item.Name,
            StoreType = item.Implementation,
            ConnectionId = item.ConnectionId,
            ConnectionName = names.Of(item.ConnectionId, item.Name),
            Description = item.Description,
            ClassificationId = item.ClassificationId,
            LastDiscoveredAt = item.LastDiscoveredAt,
            CreatedAt = item.CreatedAt,
        }));
    }
}
