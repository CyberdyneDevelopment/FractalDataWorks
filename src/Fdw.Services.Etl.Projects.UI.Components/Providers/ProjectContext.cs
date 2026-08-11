using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Fdw.Services.Etl.Projects.Abstractions;
using Fdw.Services.Etl.Projects.Abstractions.Configuration;
using Fdw.Services.Etl.Projects.Clients;
using Fdw.UI.Providers;

namespace Fdw.Services.Etl.Projects.UI.Components.Providers;

/// <summary>
/// Immutable context object passed to the consumer RenderFragment by <see cref="ProjectProvider"/>.
/// Carries both state snapshots and callback delegates so that markup stays free of logic.
/// </summary>
public sealed class ProjectContext : ProviderContextBase
{
    // ── State ──────────────────────────────────────────────────────────────────

    /// <summary>Gets the full list of projects.</summary>
    public IReadOnlyList<ProjectConfiguration> Projects { get; init; } = [];

    /// <summary>Gets the currently selected project, if any.</summary>
    public ProjectConfiguration? CurrentProject { get; init; }



    /// <summary>Gets the current search/filter string.</summary>
    public string SearchString { get; init; } = string.Empty;

    /// <summary>Gets the filtered projects based on <see cref="SearchString"/>.</summary>
    public IEnumerable<ProjectConfiguration> FilteredProjects { get; init; } = [];

    // ── Callbacks ──────────────────────────────────────────────────────────────

    /// <summary>Loads all projects.</summary>
    public Func<Task> OnLoadData { get; init; } = () => Task.CompletedTask;

    /// <summary>Gets a project by its identifier.</summary>
    public Func<Guid, Task<ProjectConfiguration?>> OnGetProject { get; init; } = _ => Task.FromResult<ProjectConfiguration?>(null);

    /// <summary>Creates a new project.</summary>
    public Func<ProjectConfiguration, Task<ProjectConfiguration?>> OnCreateProject { get; init; } = _ => Task.FromResult<ProjectConfiguration?>(null);

    /// <summary>Updates an existing project.</summary>
    public Func<Guid, ProjectConfiguration, Task<ProjectConfiguration?>> OnUpdateProject { get; init; } = (_, _) => Task.FromResult<ProjectConfiguration?>(null);

    /// <summary>Deletes a project by identifier.</summary>
    public Func<Guid, Task<bool>> OnDeleteProject { get; init; } = _ => Task.FromResult(false);

    /// <summary>Triggers a project execution.</summary>
    public Func<string, Task<TriggerResponse?>> OnTriggerProject { get; init; } = _ => Task.FromResult<TriggerResponse?>(null);

    /// <summary>Sets the current project selection.</summary>
    public Action<ProjectConfiguration?> OnSelectProject { get; init; } = _ => { };

    /// <summary>Sets the search string for filtering.</summary>
    public Action<string> OnSearchStringChanged { get; init; } = _ => { };
}
