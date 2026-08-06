using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Fdw.Services.Data.Clients.Models;
using Fdw.UI.Providers;

namespace Fdw.Data.Components.DataSets;

/// <summary>
/// Immutable context object passed to the consumer RenderFragment by <see cref="DataSetDetailProvider"/>.
/// Carries working-set state and callback delegates for the in-place composition workbench.
/// </summary>
// Why: pure view-model — state + callback delegates only, no logic.
[ExcludeFromCodeCoverage]
public sealed class DataSetDetailContext : ProviderContextBase
{
    // ── State ──────────────────────────────────────────────────────────────────

    /// <summary>Gets the DataSet detail loaded from the API.</summary>
    public DataSetDetailPayload? CurrentDataSet { get; init; }

    /// <summary>Gets the current composition mode, derived from the working sources and stores.</summary>
    public string CompositionMode { get; init; } = "Singular";

    /// <summary>Gets the working set of DataSet fields (read-only view, refreshed after each preview).</summary>
    public IReadOnlyList<DataSetFieldPayload> WorkingFields { get; init; } = [];

    /// <summary>Gets the working set of data sources being composed.</summary>
    public IReadOnlyList<DataSetSourceEditorPayload> WorkingSources { get; init; } = [];

    /// <summary>Gets the working set of join definitions being composed.</summary>
    public IReadOnlyList<DataSetJoinEditorPayload> WorkingJoins { get; init; } = [];

    /// <summary>Gets the working set of calculated field definitions being composed.</summary>
    public IReadOnlyList<DataSetCalculationEditorPayload> WorkingCalculations { get; init; } = [];

    /// <summary>Gets the working set of aggregation definitions being composed.</summary>
    public IReadOnlyList<DataSetAggregationEditorPayload> WorkingAggregations { get; init; } = [];

    /// <summary>Gets the preview rows produced by the most recent incremental preview operation.</summary>
    public IReadOnlyList<Dictionary<string, object?>> PreviewRows { get; init; } = [];

    /// <summary>
    /// Gets the ordered list of aggregation function names from the canonical AggregationFunctions
    /// TypeCollection (e.g. Sum, Count, Average, Min, Max). Empty until the API call resolves.
    /// </summary>
    public IReadOnlyList<string> AggregationFunctionNames { get; init; } = [];


    /// <summary>Gets whether a save operation is in progress.</summary>
    public bool IsSaving { get; init; }

    /// <summary>Gets whether a preview operation is in progress.</summary>
    public bool IsPreviewLoading { get; init; }


    // ── Callbacks ──────────────────────────────────────────────────────────────

    /// <summary>
    /// Adds a new source to the working set.
    /// Parameters: sourceEditorDto (the source to add).
    /// </summary>
    public Func<DataSetSourceEditorPayload, Task> OnAddSource { get; init; } = _ => Task.CompletedTask;

    /// <summary>
    /// Removes a source from the working set by its alias name.
    /// Parameters: sourceName.
    /// </summary>
    public Func<string, Task> OnRemoveSource { get; init; } = _ => Task.CompletedTask;

    /// <summary>
    /// Adds a join definition to the working set.
    /// Parameters: joinEditorDto (the join to add).
    /// </summary>
    public Func<DataSetJoinEditorPayload, Task> OnAddJoin { get; init; } = _ => Task.CompletedTask;

    /// <summary>
    /// Removes a join definition from the working set by its identifier.
    /// Parameters: joinId (Guid as string).
    /// </summary>
    public Func<string, Task> OnRemoveJoin { get; init; } = _ => Task.CompletedTask;

    /// <summary>
    /// Adds a calculated field definition to the working set.
    /// Parameters: calculationEditorDto (the calculation to add).
    /// </summary>
    public Func<DataSetCalculationEditorPayload, Task> OnAddCalculation { get; init; } = _ => Task.CompletedTask;

    /// <summary>
    /// Removes a calculated field definition from the working set by its name.
    /// Parameters: calculationName.
    /// </summary>
    public Func<string, Task> OnRemoveCalculation { get; init; } = _ => Task.CompletedTask;

    /// <summary>
    /// Adds an aggregation definition to the working set.
    /// Parameters: aggregationEditorDto (the aggregation to add).
    /// </summary>
    public Func<DataSetAggregationEditorPayload, Task> OnAddAggregation { get; init; } = _ => Task.CompletedTask;

    /// <summary>
    /// Removes an aggregation definition from the working set by its name.
    /// Parameters: aggregationName.
    /// </summary>
    public Func<string, Task> OnRemoveAggregation { get; init; } = _ => Task.CompletedTask;

    /// <summary>
    /// Executes an incremental preview using the current working set.
    /// After preview the <see cref="PreviewRows"/> and <see cref="WorkingFields"/> are refreshed.
    /// </summary>
    public Func<Task> OnPreview { get; init; } = () => Task.CompletedTask;

    /// <summary>
    /// Persists the current working-set changes back to ConfigurationDb.
    /// Calls the DataSet update API with sources, joins, calculations, and aggregations.
    /// </summary>
    public Func<Task> OnSaveDataSet { get; init; } = () => Task.CompletedTask;
}
