using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Fdw.Operations.Clients.Models;
using Fdw.Services.Connections.Clients.Models;
using Fdw.UI.Providers;

namespace Fdw.Services.Connections.Components.Connections;

/// <summary>
/// Immutable context object passed to the consumer RenderFragment by <see cref="ConnectionProvider"/>.
/// Carries both state snapshots and callback delegates so that markup can stay free of logic.
/// </summary>
public sealed class ConnectionContext : ProviderContextBase
{
    // ── State ──────────────────────────────────────────────────────────────────

    /// <summary>Gets the full list of connections.</summary>
    public IReadOnlyList<ConnectionPayload> Connections { get; init; } = [];

    /// <summary>Gets the available connection types.</summary>
    public IReadOnlyList<ConnectionTypePayload> ConnectionTypes { get; init; } = [];



    /// <summary>Gets the current search/filter string.</summary>
    public string SearchString { get; init; } = string.Empty;

    /// <summary>Gets the filtered connections based on <see cref="SearchString"/>.</summary>
    public IEnumerable<ConnectionPayload> FilteredConnections { get; init; } = [];

    /// <summary>Gets the active discovery ID, if any.</summary>
    public string? ActiveDiscoveryId { get; init; }

    /// <summary>Gets whether schema discovery is in progress.</summary>
    public bool IsDiscovering { get; init; }

    /// <summary>Gets the data store name associated with the current discovery.</summary>
    public string? DiscoveryDataStoreName { get; init; }

    /// <summary>Gets the timestamp when discovery completed.</summary>
    public DateTimeOffset? DiscoveryCompletedAt { get; init; }

    // ── Callbacks ──────────────────────────────────────────────────────────────

    /// <summary>Loads all connections and connection types.</summary>
    public Func<Task> OnLoadData { get; init; } = () => Task.CompletedTask;

    /// <summary>Gets details for a specific connection by name.</summary>
    public Func<string, Task<ConnectionDetailResponse?>> OnGetConnectionDetails { get; init; } = _ => Task.FromResult<ConnectionDetailResponse?>(null);

    /// <summary>Creates a new connection.</summary>
    public Func<CreateConnectionClientRequest, Task<ConnectionDetailResponse?>> OnCreateConnection { get; init; } = _ => Task.FromResult<ConnectionDetailResponse?>(null);

    /// <summary>Updates an existing connection.</summary>
    public Func<string, UpdateConnectionClientRequest, Task<ConnectionDetailResponse?>> OnUpdateConnection { get; init; } = (_, _) => Task.FromResult<ConnectionDetailResponse?>(null);

    /// <summary>Deletes a connection by name.</summary>
    public Func<string, Task<bool>> OnDeleteConnection { get; init; } = _ => Task.FromResult(false);

    /// <summary>Tests a connection by name.</summary>
    public Func<string, Task<TestConnectionClientResponse?>> OnTestConnection { get; init; } = _ => Task.FromResult<TestConnectionClientResponse?>(null);

    /// <summary>Returns cached TypeCollection values by collection name.</summary>
    public Func<string, IReadOnlyList<TypeCollectionValueSummary>> OnGetCollectionValues { get; init; } = _ => [];

    /// <summary>Sets the search string for filtering.</summary>
    public Action<string> OnSearchStringChanged { get; init; } = _ => { };

    /// <summary>Marks the current discovery as completed.</summary>
    public Action<string?> OnCompleteDiscovery { get; init; } = _ => { };

    /// <summary>Clears the discovery state.</summary>
    public Action OnClearDiscovery { get; init; } = () => { };
}
