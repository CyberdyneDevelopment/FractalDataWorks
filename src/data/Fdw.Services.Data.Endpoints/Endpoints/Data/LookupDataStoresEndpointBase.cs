using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.Services.Abstractions;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Data.Configuration;
using Fdw.Web.RestEndpoints.Crud;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Data.Endpoints;

/// <summary>
/// Base endpoint for looking up data stores by name (FDW-1194), for pickers and name-taken checks.
/// </summary>
/// <remarks>
/// One query on the data stores domain table: no store's implementation, paths or containers are read. It offers the rows
/// the data store list offers: a row with no name or implementation, a row naming the domain as its implementation, and a
/// store the host declared are never offered (FDW-1068). The stores the host declared are read once per request, as the
/// list reads them.
/// </remarks>
public abstract class LookupDataStoresEndpointBase : LookupEndpointBase<DataStoreDomainConfiguration>
{
    private readonly DataStoreConfigurationProvider _dataStoreProvider;
    private readonly IConfigurationGatewayProvider _configurationGateways;

    // The names of the stores the host declared, read once per request before its page.
    private HashSet<string>? _hostDeclared;

    /// <summary>Initializes a new instance of the <see cref="LookupDataStoresEndpointBase"/> class.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="dataStoreProvider">Reads the page of data store rows.</param>
    /// <param name="configurationGateways">Holds the stores the host declared, which are never offered.</param>
    protected LookupDataStoresEndpointBase(
        ILogger<LookupDataStoresEndpointBase> logger,
        DataStoreConfigurationProvider dataStoreProvider,
        IConfigurationGatewayProvider configurationGateways)
        : base(logger)
    {
        _dataStoreProvider = dataStoreProvider;
        _configurationGateways = configurationGateways;
    }

    /// <summary>Gets the resource name used for route and policy generation.</summary>
    protected override string ResourceName => "datastores";

    /// <inheritdoc />
    protected override async Task<IGenericResult<ConfigurationPage<DataStoreDomainConfiguration>>> Read(DomainQuery query, CancellationToken ct)
    {
        var hostDeclared = HostDeclaredDataStores.Names(_configurationGateways);
        if (hostDeclared.IsFailure)
            return hostDeclared.ToNewResult<ConfigurationPage<DataStoreDomainConfiguration>>();

        _hostDeclared = hostDeclared.Value!;
        return await _dataStoreProvider.Get(query, ct).ConfigureAwait(false);
    }

    /// <summary>Leaves out the rows the data store list leaves out.</summary>
    /// <param name="row">The data store row.</param>
    /// <returns>True to offer the store.</returns>
    protected override bool Includes(DataStoreDomainConfiguration row)
    {
        var hostDeclared = _hostDeclared
            ?? throw new InvalidOperationException("A data store was filtered before its page was loaded.");

        return !string.IsNullOrWhiteSpace(row.Name)
            && !string.IsNullOrWhiteSpace(row.Implementation)
            && !string.Equals(row.Implementation, "DataStore", StringComparison.OrdinalIgnoreCase)
            && !hostDeclared.Contains(row.Name);
    }
}
