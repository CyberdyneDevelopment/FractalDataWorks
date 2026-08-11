using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Fdw.Services.Etl.Projects.Abstractions.Configuration;
using Fdw.UI.Providers;

namespace Fdw.Services.Etl.Projects.UI.Components.Providers;

/// <summary>
/// Immutable context object passed to the consumer RenderFragment by <see cref="OrchestrationNodeProvider"/>.
/// Carries both state snapshots and callback delegates so that markup stays free of logic.
/// </summary>
public sealed class OrchestrationNodeContext : ProviderContextBase
{
    // ── State ──────────────────────────────────────────────────────────────────

    /// <summary>Gets the list of root nodes (nodes with no parent).</summary>
    public IReadOnlyList<OrchestrationNodeConfiguration> RootNodes { get; init; } = [];

    /// <summary>Gets the currently selected node, or <c>null</c> when none is selected.</summary>
    public OrchestrationNodeConfiguration? CurrentNode { get; init; }



    /// <summary>Gets the current search/filter string.</summary>
    public string SearchString { get; init; } = string.Empty;

    /// <summary>Gets root nodes filtered by <see cref="SearchString"/>.</summary>
    public IEnumerable<OrchestrationNodeConfiguration> FilteredRootNodes { get; init; } = [];

    // ── Callbacks ──────────────────────────────────────────────────────────────

    /// <summary>Loads all root nodes.</summary>
    public Func<Task> OnLoadData { get; init; } = () => Task.CompletedTask;

    /// <summary>Gets a node by identifier, optionally expanding child depth.</summary>
    public Func<Guid, int?, Task<OrchestrationNodeConfiguration?>> OnGetNode { get; init; } = (_, _) => Task.FromResult<OrchestrationNodeConfiguration?>(null);

    /// <summary>Creates a new orchestration node.</summary>
    public Func<OrchestrationNodeConfiguration, Task<OrchestrationNodeConfiguration?>> OnCreateNode { get; init; } = _ => Task.FromResult<OrchestrationNodeConfiguration?>(null);

    /// <summary>Updates an existing orchestration node.</summary>
    public Func<Guid, OrchestrationNodeConfiguration, Task<OrchestrationNodeConfiguration?>> OnUpdateNode { get; init; } = (_, _) => Task.FromResult<OrchestrationNodeConfiguration?>(null);

    /// <summary>Deletes an orchestration node by identifier.</summary>
    public Func<Guid, Task<bool>> OnDeleteNode { get; init; } = _ => Task.FromResult(false);

    /// <summary>Sets the currently selected node.</summary>
    public Action<OrchestrationNodeConfiguration?> OnSelectNode { get; init; } = _ => { };

    /// <summary>Sets the search string for filtering root nodes.</summary>
    public Action<string> OnSearchStringChanged { get; init; } = _ => { };
}
