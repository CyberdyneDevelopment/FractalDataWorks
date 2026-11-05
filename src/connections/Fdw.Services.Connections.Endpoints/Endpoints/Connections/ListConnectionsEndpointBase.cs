using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.Services.Connections.Abstractions;
using Fdw.Services.Tags;
using Fdw.Web.RestEndpoints.Crud;
using Microsoft.Extensions.Logging;
using Fdw.Services.Abstractions;
using Fdw.Web.RestEndpoints.Models;

namespace Fdw.Services.Connections.Endpoints;

/// <summary>
/// Base endpoint for listing configured connections, one page at a time (FDW-951).
/// </summary>
/// <remarks>
/// Each connection is read as a record only; nothing in the summary hangs from it.
/// </remarks>
public abstract class ListConnectionsEndpointBase
    : CrudPageEndpointBase<IConnectionImplementationConfiguration, ConnectionSummaryDto>
{
    private readonly ConnectionConfigurationProvider _configProvider;
    private readonly ITagService? _tags;

    /// <summary>Initializes a new instance of the <see cref="ListConnectionsEndpointBase"/> class.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="configProvider">Reads the connections.</param>
    /// <param name="tags">The Tags service that answers <c>?tag=</c>; absent on a host that does not run it, which then refuses a request that sends <c>tag</c> (FDW-3816).</param>
    protected ListConnectionsEndpointBase(
        ILogger<ListConnectionsEndpointBase> logger,
        ConnectionConfigurationProvider configProvider,
        ITagService? tags = null) : base(logger)
    {
        _configProvider = configProvider ?? throw new ArgumentNullException(nameof(configProvider));
        _tags = tags;
    }

    /// <summary>Gets the resource name used for route and policy generation.</summary>
    protected override string ResourceName => "connections";

    /// <inheritdoc />
    /// <remarks><c>?tag=</c> keeps the connections that carry every tag named (FDW-3816).</remarks>
    protected override async Task<IGenericResult<ConfigurationPage<IConnectionImplementationConfiguration>>> LoadPage(
        PaginatedListRequest paging, CancellationToken ct)
    {
        var selection = await TagSelection.Read(TaggedWith, _tags, "Connection", Logger, ct).ConfigureAwait(false);
        if (selection.IsFailure || selection.Value is null)
            return selection.ToNewResult<ConfigurationPage<IConnectionImplementationConfiguration>>();

        return await selection.Value.Read(
            (skip, take, token) => _configProvider.Get(skip, take, ConfigurationLevels.RecordOnly, token),
            connection => connection.Id,
            paging.Skip,
            paging.Take,
            ct).ConfigureAwait(false);
    }

    /// <summary>Leaves out connections with no name or implementation, and rows naming the domain as their implementation.</summary>
    /// <param name="item">The connection.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>True to list the connection.</returns>
    protected override ValueTask<bool> Include(IConnectionImplementationConfiguration item, CancellationToken ct)
        => ValueTask.FromResult(!string.IsNullOrWhiteSpace(item.Name)
            && !string.IsNullOrWhiteSpace(item.Implementation)
            && !string.Equals(item.Implementation, "Connection", StringComparison.OrdinalIgnoreCase));

    /// <inheritdoc />
    protected override Task<IGenericResult<ConnectionSummaryDto>> ToSummary(IConnectionImplementationConfiguration item, CancellationToken ct)
        => Task.FromResult(GenericResult<ConnectionSummaryDto>.Success(MapToSummary(item)));

    /// <summary>Maps a connection to its summary.</summary>
    /// <param name="config">The connection.</param>
    /// <returns>The summary.</returns>
    protected virtual ConnectionSummaryDto MapToSummary(IConnectionImplementationConfiguration config)
    {
        return new ConnectionSummaryDto
        {
            Id = config.Id,
            Name = config.Name,
            ConnectionType = config.Implementation,
            SchemaDiscovery = SchemaDiscoveryTypeInfo.Of(config.SchemaDiscovery),
        };
    }
}
