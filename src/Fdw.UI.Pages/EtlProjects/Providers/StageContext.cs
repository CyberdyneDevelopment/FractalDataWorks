using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Fdw.Services.Etl.Projects.Abstractions.Configuration;
using Fdw.UI.Providers;

namespace Fdw.Services.Etl.Projects.UI.Components.Providers;

/// <summary>
/// Immutable context object passed to the consumer RenderFragment by <see cref="StageProvider"/>.
/// Carries both state snapshots and callback delegates so that markup stays free of logic.
/// </summary>
public sealed class StageContext : ProviderContextBase
{
    // ── State ──────────────────────────────────────────────────────────────────

    /// <summary>Gets the list of stages for the current project.</summary>
    public IReadOnlyList<StageConfiguration> Stages { get; init; } = [];

    /// <summary>Gets the currently selected stage, if any.</summary>
    public StageConfiguration? CurrentStage { get; init; }

    /// <summary>Gets the project identifier whose stages are loaded.</summary>
    public Guid? ProjectId { get; init; }



    // ── Callbacks ──────────────────────────────────────────────────────────────

    /// <summary>Loads all stages for a given project.</summary>
    public Func<Guid, Task> OnLoadStages { get; init; } = _ => Task.CompletedTask;

    /// <summary>Creates a new stage.</summary>
    public Func<StageConfiguration, Task<StageConfiguration?>> OnCreateStage { get; init; } = _ => Task.FromResult<StageConfiguration?>(null);

    /// <summary>Updates an existing stage.</summary>
    public Func<Guid, StageConfiguration, Task<StageConfiguration?>> OnUpdateStage { get; init; } = (_, _) => Task.FromResult<StageConfiguration?>(null);

    /// <summary>Deletes a stage by identifier.</summary>
    public Func<Guid, Task<bool>> OnDeleteStage { get; init; } = _ => Task.FromResult(false);

    /// <summary>Sets the current stage selection.</summary>
    public Action<StageConfiguration?> OnSelectStage { get; init; } = _ => { };
}
