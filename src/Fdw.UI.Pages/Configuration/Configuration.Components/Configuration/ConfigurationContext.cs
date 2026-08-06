using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Fdw.Operations.Clients.Models;
using Fdw.UI.Providers;

namespace Fdw.Configuration.Components.Configuration;

/// <summary>
/// Immutable context object passed to the consumer RenderFragment by <see cref="ConfigurationProvider"/>.
/// Carries both state snapshots and callback delegates so that markup can stay free of logic.
/// </summary>
public sealed class ConfigurationContext : ProviderContextBase
{
    // ── State ──────────────────────────────────────────────────────────────────

    /// <summary>Gets the list of configuration instances.</summary>
    public IReadOnlyList<ConfigurationInstanceSummaryPayload> Instances { get; init; } = [];

    /// <summary>Gets the list of configuration types.</summary>
    public IReadOnlyList<ConfigurationTypeSummary> Types { get; init; } = [];



    /// <summary>Gets the current search/filter string.</summary>
    public string SearchString { get; init; } = string.Empty;

    /// <summary>Gets the filtered instances based on <see cref="SearchString"/>.</summary>
    public IEnumerable<ConfigurationInstanceSummaryPayload> FilteredInstances { get; init; } = [];

    // ── Callbacks ──────────────────────────────────────────────────────────────

    /// <summary>Loads instances, optionally filtered by category.</summary>
    public Func<string?, Task> OnLoadInstances { get; init; } = _ => Task.CompletedTask;

    /// <summary>Loads types for a given category.</summary>
    public Func<string, Task> OnLoadTypes { get; init; } = _ => Task.CompletedTask;

    /// <summary>Gets details for a specific instance by category and name.</summary>
    public Func<string, string, Task<ConfigurationInstanceDetailPayload?>> OnGetInstanceDetails { get; init; } = (_, _) => Task.FromResult<ConfigurationInstanceDetailPayload?>(null);

    /// <summary>Creates a new configuration instance.</summary>
    public Func<string, CreateConfigurationInstanceRequest, Task<ConfigurationInstanceDetailPayload?>> OnCreateInstance { get; init; } = (_, _) => Task.FromResult<ConfigurationInstanceDetailPayload?>(null);

    /// <summary>Updates an existing configuration instance.</summary>
    public Func<string, string, UpdateConfigurationInstanceRequest, Task<ConfigurationInstanceDetailPayload?>> OnUpdateInstance { get; init; } = (_, _, _) => Task.FromResult<ConfigurationInstanceDetailPayload?>(null);

    /// <summary>Gets all root types.</summary>
    public Func<Task<IReadOnlyList<ConfigurationTypeSummary>?>> OnGetRootTypes { get; init; } = () => Task.FromResult<IReadOnlyList<ConfigurationTypeSummary>?>(null);

    /// <summary>Deletes a configuration instance by category and name.</summary>
    public Func<string, string, Task<bool>> OnDeleteInstance { get; init; } = (_, _) => Task.FromResult(false);

    /// <summary>Sets the search string for filtering.</summary>
    public Action<string> OnSearchStringChanged { get; init; } = _ => { };
}
