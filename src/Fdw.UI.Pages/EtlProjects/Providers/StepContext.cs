using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Fdw.Services.Etl.Projects.Abstractions.Configuration;
using Fdw.UI.Providers;

namespace Fdw.Services.Etl.Projects.UI.Components.Providers;

/// <summary>
/// Immutable context object passed to the consumer RenderFragment by <see cref="StepProvider"/>.
/// Carries both state snapshots and callback delegates so that markup stays free of logic.
/// </summary>
public sealed class StepContext : ProviderContextBase
{
    // ── State ──────────────────────────────────────────────────────────────────

    /// <summary>Gets the list of steps for the current stage.</summary>
    public IReadOnlyList<StepConfiguration> Steps { get; init; } = [];

    /// <summary>Gets the currently selected step, if any.</summary>
    public StepConfiguration? CurrentStep { get; init; }

    /// <summary>Gets the stage identifier whose steps are loaded.</summary>
    public Guid? StageId { get; init; }



    // ── Callbacks ──────────────────────────────────────────────────────────────

    /// <summary>Loads all steps for a given stage.</summary>
    public Func<Guid, Task> OnLoadSteps { get; init; } = _ => Task.CompletedTask;

    /// <summary>Creates a new step.</summary>
    public Func<StepConfiguration, Task<StepConfiguration?>> OnCreateStep { get; init; } = _ => Task.FromResult<StepConfiguration?>(null);

    /// <summary>Updates an existing step.</summary>
    public Func<Guid, StepConfiguration, Task<StepConfiguration?>> OnUpdateStep { get; init; } = (_, _) => Task.FromResult<StepConfiguration?>(null);

    /// <summary>Deletes a step by identifier.</summary>
    public Func<Guid, Task<bool>> OnDeleteStep { get; init; } = _ => Task.FromResult(false);

    /// <summary>Sets the current step selection.</summary>
    public Action<StepConfiguration?> OnSelectStep { get; init; } = _ => { };
}
