using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Fdw.Services.Pipelines.Clients.Abstractions;
using Fdw.UI.Providers;

namespace Fdw.Services.Pipelines.Components.Pipelines;

/// <summary>
/// Immutable context object passed to the consumer RenderFragment by <see cref="SchedulePipelineProvider"/>.
/// Carries both state snapshots and callback delegates so that markup can stay free of logic.
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class SchedulePipelineContext : ProviderContextBase
{
    // ── State ──────────────────────────────────────────────────────────────────

    /// <summary>Gets the list of available pipelines for scheduling.</summary>
    public IReadOnlyList<PipelineSummaryResponse> Pipelines { get; init; } = [];



    // ── Callbacks ──────────────────────────────────────────────────────────────

    /// <summary>Invoked to reload the list of available pipelines.</summary>
    public Func<Task> OnLoadPipelines { get; init; } = () => Task.CompletedTask;
}
