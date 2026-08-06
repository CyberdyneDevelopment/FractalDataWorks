using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Fdw.Services.Data.Clients.Models;
using Fdw.UI.Providers;

namespace Fdw.Data.Components.DataStores;

/// <summary>
/// Immutable context object passed to the consumer RenderFragment by <see cref="DataStoreProvider"/>.
/// Carries both state snapshots and callback delegates so that markup can stay free of logic.
/// </summary>
// Why: pure view-model — state + callback delegates only, no logic.
[ExcludeFromCodeCoverage]
public sealed class DataStoreContext : ProviderContextBase
{
    // ── State ──────────────────────────────────────────────────────────────────

    /// <summary>Gets the full list of data store summaries.</summary>
    public IReadOnlyList<DataStoreSummaryPayload> DataStores { get; init; } = [];



    /// <summary>Gets the current search/filter string.</summary>
    public string SearchString { get; init; } = string.Empty;

    /// <summary>Gets the filtered data stores based on <see cref="SearchString"/>.</summary>
    public IEnumerable<DataStoreSummaryPayload> FilteredDataStores { get; init; } = [];

    // ── Callbacks ──────────────────────────────────────────────────────────────

    /// <summary>Loads all data store summaries.</summary>
    public Func<Task> OnLoadDataStores { get; init; } = () => Task.CompletedTask;

    /// <summary>Gets details for a specific data store by name.</summary>
    public Func<string, Task<DataStoreDetailPayload?>> OnGetDataStoreDetails { get; init; } = _ => Task.FromResult<DataStoreDetailPayload?>(null);

    /// <summary>Creates a new data store.</summary>
    public Func<CreateDataStoreWithPathsRequest, Task<DataStoreDetailPayload?>> OnCreateDataStore { get; init; } = _ => Task.FromResult<DataStoreDetailPayload?>(null);

    /// <summary>Updates an existing data store.</summary>
    public Func<string, UpdateDataStoreWithPathsRequest, Task<DataStoreDetailPayload?>> OnUpdateDataStore { get; init; } = (_, _) => Task.FromResult<DataStoreDetailPayload?>(null);

    /// <summary>Deletes a data store by name.</summary>
    public Func<string, Task<bool>> OnDeleteDataStore { get; init; } = _ => Task.FromResult(false);

    /// <summary>Discovers containers for a connection and path.</summary>
    public Func<string, string, Task<IReadOnlyList<ContainerDiscoveryResult>?>> OnDiscoverContainers { get; init; } = (_, _) => Task.FromResult<IReadOnlyList<ContainerDiscoveryResult>?>(null);

    /// <summary>Sets the search string for filtering.</summary>
    public Action<string> OnSearchStringChanged { get; init; } = _ => { };
}
