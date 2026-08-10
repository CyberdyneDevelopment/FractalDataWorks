using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Fdw.Services.Quality.Clients.Models;
using Fdw.UI.Providers;

namespace Fdw.Services.Quality.Components.QualityRules;

/// <summary>
/// Immutable context object passed to the consumer RenderFragment by <see cref="QualityRuleProvider"/>.
/// Carries both state snapshots and callback delegates so that markup can stay free of logic.
/// </summary>
public sealed class QualityRuleContext : ProviderContextBase
{
    // ── State ──────────────────────────────────────────────────────────────────

    /// <summary>Gets the list of quality rules.</summary>
    public IReadOnlyList<QualityRuleSummaryPayload> Rules { get; init; } = [];



    // ── Callbacks ──────────────────────────────────────────────────────────────

    /// <summary>Invoked to create a new quality rule.</summary>
    public Func<CreateQualityRulePayload, Task> OnCreate { get; init; } = _ => Task.CompletedTask;

    /// <summary>Invoked to update an existing quality rule.</summary>
    public Func<Guid, UpdateQualityRulePayload, Task> OnUpdate { get; init; } = (_, _) => Task.CompletedTask;

    /// <summary>Invoked to delete a quality rule.</summary>
    public Func<Guid, Task> OnDelete { get; init; } = _ => Task.CompletedTask;

    /// <summary>Invoked to execute a quality check for a specific rule.</summary>
    public Func<Guid, Task> OnExecute { get; init; } = _ => Task.CompletedTask;

}
