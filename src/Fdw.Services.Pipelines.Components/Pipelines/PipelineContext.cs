#pragma warning disable CS1591
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Fdw.Services.Pipelines.Clients.Abstractions;
using Fdw.UI.Providers;

namespace Fdw.Services.Pipelines.Components.Pipelines;

// Why: pure view-model — state + callback delegates only, no logic.
[ExcludeFromCodeCoverage]
public sealed class PipelineContext : ProviderContextBase
{
    // ── State ──────────────────────────────────────────────────────────────────

    public IReadOnlyList<PipelineSummaryResponse> Pipelines { get; init; } = [];
    public IReadOnlyList<PipelineSummaryResponse> FilteredPipelines { get; init; } = [];
    public IReadOnlyList<TriggerPipelineResponse> RecentJobs { get; init; } = [];
    public string SearchString { get; init; } = "";

    // ── Callbacks ──────────────────────────────────────────────────────────────

    public Func<Task> OnLoadData { get; init; } = () => Task.CompletedTask;
    public Func<string, Task<PipelineDetailResponse?>> OnGetPipelineDetails { get; init; } = _ => Task.FromResult<PipelineDetailResponse?>(null);
    public Func<string, string?, Task<TriggerPipelineResponse?>> OnTriggerJob { get; init; } = (_, _) => Task.FromResult<TriggerPipelineResponse?>(null);
    public Func<Guid, Task<TriggerPipelineResponse?>> OnGetJobStatus { get; init; } = _ => Task.FromResult<TriggerPipelineResponse?>(null);
    public Func<CreatePipelineClientRequest, Task<PipelineDetailResponse?>> OnCreatePipeline { get; init; } = _ => Task.FromResult<PipelineDetailResponse?>(null);
    public Func<string, UpdatePipelineClientRequest, Task<PipelineDetailResponse?>> OnUpdatePipeline { get; init; } = (_, _) => Task.FromResult<PipelineDetailResponse?>(null);
    public Func<string, Task> OnSearchChanged { get; init; } = _ => Task.CompletedTask;
}
