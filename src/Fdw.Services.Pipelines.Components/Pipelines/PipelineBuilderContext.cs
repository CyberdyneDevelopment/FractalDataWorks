using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Fdw.Services.Data.Clients.Models;
using Fdw.Services.Pipelines.Clients.Abstractions;
using Fdw.Services.Pipelines.Components.Canvas;
using Fdw.UI.Pipelines.Clients.Models;
using Fdw.UI.Providers;

namespace Fdw.Services.Pipelines.Components.Pipelines;

/// <summary>
/// Immutable context object passed to the consumer RenderFragment by <see cref="PipelineBuilderProvider"/>.
/// Carries both state snapshots and callback delegates so that markup can stay free of logic.
/// </summary>
// Why: pure view-model — state + callback delegates only, no logic.
[ExcludeFromCodeCoverage]
public sealed class PipelineBuilderContext : ProviderContextBase
{
    // ── State ──────────────────────────────────────────────────────────────────

    /// <summary>Gets the list of available task types for the pipeline builder.</summary>
    public IReadOnlyList<TaskTypeInfo> TaskTypes { get; init; } = [];

    /// <summary>Gets the list of available pipeline step types loaded from the designer API.</summary>
    public IReadOnlyList<PipelineStepTypeSummary> PipelineStepTypes { get; init; } = [];

    /// <summary>Gets the list of available DataSets for task binding.</summary>
    public IReadOnlyList<DataSetSummaryPayload> DataSets { get; init; } = [];

    /// <summary>
    /// Gets the list of registered ETL pipeline engine types (e.g. "BatchCopy", "Streaming"),
    /// sourced from <c>IPipelineClient.GetPipelineTypes</c> — the real, user-facing source for the
    /// canvas's engine picker. Never defaulted; a new pipeline's <c>CanvasModel.PipelineType</c>
    /// stays null until the user picks one of these.
    /// </summary>
    public IReadOnlyList<PipelineTypeSummary> PipelineTypes { get; init; } = [];

    /// <summary>
    /// Gets the projected canvas model for the loaded pipeline, or <c>null</c> when creating new
    /// or while loading is still in progress.
    /// </summary>
    public PipelineCanvasModel? CanvasModel { get; init; }


    /// <summary>Gets whether the pipeline is being saved.</summary>
    public bool IsSaving { get; init; }

    /// <summary>Gets whether the pipeline is being published.</summary>
    public bool IsPublishing { get; init; }


    /// <summary>
    /// Gets the DataSet name to pre-select on the first Source task when creating a new pipeline,
    /// or <c>null</c> when no pre-selection was requested (e.g., not arriving from catalog/detail).
    /// </summary>
    public string? InitialSourceDataSet { get; init; }

    /// <summary>
    /// Gets the write capability of the currently selected Destination task's connection, or
    /// <c>null</c> when no Destination task is selected or no connection is bound.
    /// Values are "Table", "API", or "None" based on the connection's supported write commands.
    /// </summary>
    public string? DestinationWriteMode { get; init; }

    /// <summary>
    /// Gets whether the currently selected Destination task has a usable write capability.
    /// <c>false</c> when no Destination is selected or the connection has no write-capable command.
    /// </summary>
    public bool HasDestinationCapability { get; init; }

    // ── Callbacks ──────────────────────────────────────────────────────────────

    /// <summary>Invoked to reload task types.</summary>
    public Func<Task> OnLoadTaskTypes { get; init; } = () => Task.CompletedTask;

    /// <summary>Invoked to load an existing pipeline by ID.</summary>
    public Func<Guid, Task> OnLoadExisting { get; init; } = _ => Task.CompletedTask;

    /// <summary>
    /// Invoked to save the current canvas model. Returns <c>true</c> on success, <c>false</c>
    /// when validation or persistence fails (inspect <c>LastResult</c> for details).
    /// </summary>
    public Func<string, string?, Task<bool>> OnSave { get; init; } = (_, _) => Task.FromResult(false);

    /// <summary>Invoked to publish the pipeline by name. Returns true if successful.</summary>
    public Func<string, Task<bool>> OnPublish { get; init; } = _ => Task.FromResult(false);

    /// <summary>Invoked to create a new DataSet from the inline editor. Returns the created DataSet name on success, or null on failure.</summary>
    public Func<CreateDataSetPayload, Task<string?>> OnCreateDataSet { get; init; } = _ => Task.FromResult<string?>(null);

    /// <summary>Invoked to set the canvas model's engine discriminator from a user selection.</summary>
    public Func<string, Task> OnSetPipelineType { get; init; } = _ => Task.CompletedTask;
}
