using System;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Configuration;
using Fdw.Results;
using Fdw.Services.Abstractions;
using Fdw.Services.Data.Abstractions;
using Fdw.Web.RestEndpoints.Crud;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Connections.Endpoints;

/// <summary>
/// Base endpoint for looking up connections by name (FDW-1194), for pickers and name-taken checks.
/// </summary>
/// <remarks>
/// One query on the connections domain table: no connection's implementation is read. It offers the rows the connection
/// list offers: a row with no name or implementation, and a row naming the domain as its implementation, are never offered.
/// </remarks>
public abstract class LookupConnectionsEndpointBase : LookupEndpointBase<DomainConfiguration>
{
    private readonly ConnectionConfigurationProvider _configProvider;

    /// <summary>Initializes a new instance of the <see cref="LookupConnectionsEndpointBase"/> class.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="configProvider">Reads the page of connection rows.</param>
    protected LookupConnectionsEndpointBase(ILogger<LookupConnectionsEndpointBase> logger, ConnectionConfigurationProvider configProvider)
        : base(logger)
    {
        _configProvider = configProvider;
    }

    /// <summary>Gets the resource name used for route and policy generation.</summary>
    protected override string ResourceName => "connections";

    /// <inheritdoc />
    protected override Task<IGenericResult<ConfigurationPage<DomainConfiguration>>> Read(DomainQuery query, CancellationToken ct)
        => _configProvider.Get(query, ct);

    /// <summary>Leaves out the rows the connection list leaves out.</summary>
    /// <param name="row">The connection row.</param>
    /// <returns>True to offer the connection.</returns>
    protected override bool Includes(DomainConfiguration row)
        => !string.IsNullOrWhiteSpace(row.Name)
            && !string.IsNullOrWhiteSpace(row.Implementation)
            && !string.Equals(row.Implementation, "Connection", StringComparison.OrdinalIgnoreCase);
}
