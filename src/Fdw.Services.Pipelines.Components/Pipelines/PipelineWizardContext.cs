using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Fdw.Services.Connections.Clients.Models;
using Fdw.Services.Data.Clients.Models;
using Fdw.Services.Pipelines.Clients.Abstractions;
using Fdw.UI.Pipelines.Clients.Models;
using Fdw.UI.Wizard;
using Fdw.UI.Providers;

namespace Fdw.Services.Pipelines.Components.Pipelines;

/// <summary>
/// Immutable context object passed to the consumer RenderFragment by <see cref="PipelineWizardProvider"/>.
/// Carries both state snapshots and callback delegates so that markup can stay free of logic.
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class PipelineWizardContext : ProviderContextBase
{
    // ── Wizard Navigation ─────────────────────────────────────────────────────

    /// <summary>Gets the shared wizard navigation and status state from the base provider.</summary>
    public IWizardContext Wizard { get; init; } = new WizardContext();

    /// <summary>Gets the current wizard step (0=Engine, 1=Details, 2=Source, 3=Destination, 4=Review).</summary>
    public int Step { get; init; }

    /// <summary>Gets whether the wizard is on the first step.</summary>
    public bool IsFirstStep { get; init; }

    /// <summary>Gets whether the wizard is on the last step.</summary>
    public bool IsLastStep { get; init; }



    // ── Engine Type (pluggable — sourced from EtlPipelineTypes) ───────────────

    /// <summary>
    /// Gets the available pipeline engine types, sourced from the <c>EtlPipelineTypes</c>
    /// ServiceTypeCollection via <c>GET /pipelines/types</c>. Never hardcoded.
    /// </summary>
    public IReadOnlyList<PipelineTypeSummary> EngineTypes { get; init; } = [];

    /// <summary>Gets the name of the currently selected engine type, or <c>null</c> when none selected.</summary>
    public string? SelectedEngineTypeName { get; init; }

    // ── Details ────────────────────────────────────────────────────────────────

    /// <summary>Gets the pipeline name entered by the user.</summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>Gets the optional pipeline description entered by the user.</summary>
    public string? Description { get; init; }

    // ── Source / Destination ──────────────────────────────────────────────────

    /// <summary>Gets the available connections for the Source/Destination Connection-kind picker.</summary>
    public IReadOnlyList<ConnectionPayload> Connections { get; init; } = [];

    /// <summary>Gets the available DataSets for the Source/Destination DataSet-kind picker.</summary>
    public IReadOnlyList<DataSetSummaryPayload> DataSets { get; init; } = [];

    /// <summary>Gets the current source reference (Connection or DataSet kind).</summary>
    public DataSourceReference Source { get; init; } = new();

    /// <summary>Gets the current destination reference (Connection or DataSet kind).</summary>
    public DataDestinationReference Destination { get; init; } = new();

    // ── Completion ─────────────────────────────────────────────────────────────

    /// <summary>Gets whether the wizard has completed successfully.</summary>
    public bool IsComplete { get; init; }

    /// <summary>Gets the identifier of the created pipeline once <c>OnCreatePipeline</c> succeeds.</summary>
    public Guid? CreatedPipelineId { get; init; }

    // ── Callbacks ──────────────────────────────────────────────────────────────

    /// <summary>Invoked when the user selects an engine type from <see cref="EngineTypes"/>.</summary>
    public Action<string> OnSelectEngineType { get; init; } = _ => { };

    /// <summary>Invoked when the pipeline name changes.</summary>
    public Action<string> OnNameChanged { get; init; } = _ => { };

    /// <summary>Invoked when the pipeline description changes.</summary>
    public Action<string?> OnDescriptionChanged { get; init; } = _ => { };

    /// <summary>Invoked when the user switches the Source kind ("Connection" or "DataSet").</summary>
    public Action<string> OnSourceKindChanged { get; init; } = _ => { };

    /// <summary>Invoked when the user selects the Source connection/DataSet name.</summary>
    public Action<string> OnSourceNameChanged { get; init; } = _ => { };

    /// <summary>Invoked when the user switches the Destination kind ("Connection" or "DataSet").</summary>
    public Action<string> OnDestinationKindChanged { get; init; } = _ => { };

    /// <summary>Invoked when the user selects the Destination connection/DataSet name.</summary>
    public Action<string> OnDestinationNameChanged { get; init; } = _ => { };

    /// <summary>Advances to the next step, subject to per-step required-field validation.</summary>
    public Func<Task> OnNextStep { get; init; } = () => Task.CompletedTask;

    /// <summary>Returns to the previous step.</summary>
    public Action OnPreviousStep { get; init; } = () => { };

    /// <summary>
    /// Creates the pipeline shell via <c>IPipelineClient.CreatePipeline</c>. Fails loud (sets
    /// <c>LastResult</c>, returns <c>null</c>) when engine type, name, source, or destination
    /// is missing. Returns the created pipeline id on success so the host can navigate to the Builder.
    /// </summary>
    public Func<Task<Guid?>> OnCreatePipeline { get; init; } = () => Task.FromResult<Guid?>(null);
}
