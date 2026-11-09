using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
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
/// Each connection is read as a record only; nothing in the summary hangs from it. The disabled states of the page's connections are read
/// together, once (FDW-4290).
/// </remarks>
public abstract class ListConnectionsEndpointBase
    : CrudPageEndpointBase<ConnectionListing, ConnectionSummaryDto>
{
    private readonly IConnectionConfigurationProvider _configProvider;
    private readonly IConnectionDisabledStates _states;
    private readonly ITagService? _tags;

    /// <summary>Initializes a new instance of the <see cref="ListConnectionsEndpointBase"/> class.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="configProvider">Reads the connections.</param>
    /// <param name="states">Where the disabled state of a connection is kept; the page's states are read with one read (FDW-4290).</param>
    /// <param name="tags">The Tags service that answers <c>?tag=</c>; absent on a host that does not run it, which then refuses a request that sends <c>tag</c> (FDW-3816).</param>
    protected ListConnectionsEndpointBase(
        ILogger<ListConnectionsEndpointBase> logger,
        IConnectionConfigurationProvider configProvider,
        IConnectionDisabledStates states,
        ITagService? tags = null) : base(logger)
    {
        _configProvider = configProvider ?? throw new ArgumentNullException(nameof(configProvider));
        _states = states ?? throw new ArgumentNullException(nameof(states));
        _tags = tags;
    }

    /// <summary>Gets the resource name used for route and policy generation.</summary>
    protected override string ResourceName => "connections";

    /// <inheritdoc />
    /// <remarks><c>?tag=</c> keeps the connections that carry every tag named (FDW-3816).</remarks>
    protected override async Task<IGenericResult<ConfigurationPage<ConnectionListing>>> LoadPage(
        PaginatedListRequest paging, CancellationToken ct)
    {
        var selection = await TagSelection.Read(TaggedWith, _tags, "Connection", Logger, ct).ConfigureAwait(false);
        if (selection.IsFailure || selection.Value is null)
            return selection.ToNewResult<ConfigurationPage<ConnectionListing>>();

        var page = await selection.Value.Read(
            (skip, take, token) => _configProvider.Get(skip, take, ConfigurationLevels.RecordOnly, token),
            connection => connection.Id,
            paging.Skip,
            paging.Take,
            ct).ConfigureAwait(false);
        if (page.IsFailure || page.Value is null)
            return page.ToNewResult<ConfigurationPage<ConnectionListing>>();

        var read = page.Value;
        return GenericResult<ConfigurationPage<ConnectionListing>>.Success(
            new ConfigurationPage<ConnectionListing>(read.Skip, read.Take, read.HasMore, fail => Listings(read, fail, ct), read.Total));
    }

    // The page holds at most Take records, so they are read first and their states are asked for together; a page whose records or states
    // cannot be read ends with that failure and no row is shown as enabled for want of an answer (FDW-4290).
    private async IAsyncEnumerable<ConnectionListing> Listings(
        ConfigurationPage<IConnectionImplementationConfiguration> page,
        Action<IGenericResult> fail,
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        var connections = new List<IConnectionImplementationConfiguration>();
        await foreach (var connection in page.Items.WithCancellation(ct).ConfigureAwait(false))
            connections.Add(connection);

        if (page.Outcome.IsFailure)
        {
            fail(page.Outcome);
            yield break;
        }

        var states = await _states.ReadMany([.. connections.Select(connection => connection.Id)], ct).ConfigureAwait(false);
        if (states.IsFailure || states.Value is null)
        {
            fail(states);
            yield break;
        }

        foreach (var connection in connections)
            yield return new ConnectionListing(connection, states.Value.TryGetValue(connection.Id, out var disabled) ? disabled : null);
    }

    /// <summary>Leaves out connections with no name or implementation, and rows naming the domain as their implementation.</summary>
    /// <param name="item">The connection.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>True to list the connection.</returns>
    protected override ValueTask<bool> Include(ConnectionListing item, CancellationToken ct)
        => ValueTask.FromResult(!string.IsNullOrWhiteSpace(item.Connection.Name)
            && !string.IsNullOrWhiteSpace(item.Connection.Implementation)
            && !string.Equals(item.Connection.Implementation, "Connection", StringComparison.OrdinalIgnoreCase));

    /// <inheritdoc />
    protected override Task<IGenericResult<ConnectionSummaryDto>> ToSummary(ConnectionListing item, CancellationToken ct)
    {
        var summary = MapToSummary(item.Connection);
        summary.Disabled = item.Disabled is { } disabled ? ConnectionDisabledDto.Of(disabled) : null;
        return Task.FromResult(GenericResult<ConnectionSummaryDto>.Success(summary));
    }

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
