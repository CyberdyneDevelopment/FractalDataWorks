using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Fdw.Services.Data.Clients.Models;
using Fdw.UI.Providers;

namespace Fdw.Data.Components.DataSets;

/// <summary>
/// Immutable context object passed to the consumer RenderFragment by <see cref="DataSetProvider"/>.
/// Carries both state snapshots and callback delegates so that markup can stay free of logic.
/// </summary>
public sealed class DataSetContext : ProviderContextBase
{
    // ── State ──────────────────────────────────────────────────────────────────

    /// <summary>Gets the full list of data set summaries.</summary>
    public IReadOnlyList<DataSetSummaryPayload> DataSets { get; init; } = [];

    /// <summary>Gets the currently loaded data set detail, or <c>null</c>.</summary>
    public DataSetDetailPayload? CurrentDataSet { get; init; }



    /// <summary>Gets the current search/filter string.</summary>
    public string SearchString { get; init; } = string.Empty;

    /// <summary>Gets the currently selected category filter value (empty = all categories).</summary>
    public string CategoryFilter { get; init; } = string.Empty;

    /// <summary>Gets the distinct set of categories present in <see cref="DataSets"/>.</summary>
    public IReadOnlyList<string> Categories { get; init; } = [];

    /// <summary>Gets the filtered data sets based on <see cref="SearchString"/> and <see cref="CategoryFilter"/>.</summary>
    public IEnumerable<DataSetSummaryPayload> FilteredDataSets { get; init; } = [];

    // ── Callbacks ──────────────────────────────────────────────────────────────

    /// <summary>Loads all data set summaries.</summary>
    public Func<Task> OnLoadDataSets { get; init; } = () => Task.CompletedTask;

    /// <summary>Loads a single data set by name.</summary>
    public Func<string, Task> OnLoadDataSet { get; init; } = _ => Task.CompletedTask;

    /// <summary>Creates a new data set.</summary>
    public Func<CreateDataSetPayload, Task<DataSetDetailPayload?>> OnCreateDataSet { get; init; } = _ => Task.FromResult<DataSetDetailPayload?>(null);

    /// <summary>Updates an existing data set.</summary>
    public Func<string, UpdateDataSetPayload, Task<DataSetDetailPayload?>> OnUpdateDataSet { get; init; } = (_, _) => Task.FromResult<DataSetDetailPayload?>(null);

    /// <summary>Deletes a data set by name.</summary>
    public Func<string, Task<bool>> OnDeleteDataSet { get; init; } = _ => Task.FromResult(false);

    /// <summary>Sets the search string for filtering.</summary>
    public Action<string> OnSearchStringChanged { get; init; } = _ => { };

    /// <summary>Sets the category filter.</summary>
    public Action<string> OnCategoryFilterChanged { get; init; } = _ => { };

    /// <summary>
    /// Invoked to start the Pipeline Builder pre-seeded with the named DataSet as the source.
    /// Navigates to /pipelines/new?sourceDataSet={name}.
    /// </summary>
    public Func<string, Task> OnStartPipeline { get; init; } = _ => Task.CompletedTask;

    /// <summary>
    /// Invoked to open the DataSet wizard pre-seeded with the named DataSet as the derivation base.
    /// Navigates to /datasets/new?baseDataSet={name}.
    /// </summary>
    public Func<string, Task> OnDeriveDataSet { get; init; } = _ => Task.CompletedTask;
}
