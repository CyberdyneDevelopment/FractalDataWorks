using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Fdw.Services.Data.Clients.Models;
using Fdw.UI.Providers;

namespace Fdw.Data.Components.DataPreview;

/// <summary>
/// Immutable context object passed to the consumer RenderFragment by <see cref="DataPreviewProvider"/>.
/// Carries state snapshots for DataStore/DataSet loading and selection, plus callback delegates
/// so that markup can remain free of logic.
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class DataPreviewContext : ProviderContextBase
{
    // ── Mode ───────────────────────────────────────────────────────────────────

    /// <summary>
    /// Gets the currently active preview mode.
    /// Well-known values are defined in <see cref="PreviewModes"/>.
    /// </summary>
    public string Mode { get; init; } = PreviewModes.DataStore.Name;

    // ── DataStore state ────────────────────────────────────────────────────────

    /// <summary>Gets the list of available DataStores.</summary>
    public IReadOnlyList<DataStoreSummaryPayload> DataStores { get; init; } = [];

    /// <summary>Gets the name of the currently selected DataStore.</summary>
    public string SelectedDataStoreName { get; init; } = string.Empty;

    /// <summary>Gets the detail for the currently selected DataStore, or <c>null</c> when none is selected.</summary>
    public DataStoreDetailPayload? SelectedDataStoreDetail { get; init; }

    /// <summary>Gets the currently selected container within the DataStore, or <c>null</c> when none is selected.</summary>
    public DataStoreContainerPayload? SelectedContainer { get; init; }

    /// <summary>Gets the physical path name for the selected container.</summary>
    public string DsPathName { get; init; } = string.Empty;

    /// <summary>Gets the physical container name for the selected container.</summary>
    public string DsContainerName { get; init; } = string.Empty;

    /// <summary>Gets whether the DataStore list is currently loading.</summary>
    public bool IsLoadingDataStores { get; init; }

    /// <summary>Gets whether the DataStore detail is currently loading.</summary>
    public bool IsLoadingDataStoreDetail { get; init; }

    // ── DataSet state ──────────────────────────────────────────────────────────

    /// <summary>Gets the list of available DataSets.</summary>
    public IReadOnlyList<DataSetSummaryPayload> DataSets { get; init; } = [];

    /// <summary>Gets the name of the currently selected DataSet.</summary>
    public string SelectedDataSetName { get; init; } = string.Empty;

    /// <summary>Gets the detail for the currently selected DataSet, or <c>null</c> when none is selected.</summary>
    public DataSetDetailPayload? SelectedDataSetDetail { get; init; }

    /// <summary>Gets whether the DataSet list is currently loading.</summary>
    public bool IsLoadingDataSets { get; init; }

    // ── Connection/Table state ─────────────────────────────────────────────────

    /// <summary>Gets the name of the selected connection (used in Table mode).</summary>
    public string SelectedConnection { get; init; } = string.Empty;

    /// <summary>Gets the optional connection override for DataSet mode.</summary>
    public string DataSetConnection { get; init; } = string.Empty;

    /// <summary>Gets the schema name for Table mode.</summary>
    public string SchemaName { get; init; } = string.Empty;

    /// <summary>Gets the table name for Table mode.</summary>
    public string TableName { get; init; } = string.Empty;

    /// <summary>Gets the maximum number of rows to return in the preview.</summary>
    public int MaxRows { get; init; } = 100;

    // ── Error ──────────────────────────────────────────────────────────────────


    // ── Callbacks ──────────────────────────────────────────────────────────────

    /// <summary>Invoked when the user selects a DataStore. Loads detail and resets container selection.</summary>
    public Func<string, Task> OnDataStoreChanged { get; init; } = _ => Task.CompletedTask;

    /// <summary>Invoked when the user clicks a container in the schema browser tree.</summary>
    public Func<DataStorePathPayload, DataStoreContainerPayload, Task> OnContainerSelected { get; init; } = (_, _) => Task.CompletedTask;

    /// <summary>Invoked when the user selects a DataSet. Loads DataSet detail.</summary>
    public Func<string, Task> OnDataSetChanged { get; init; } = _ => Task.CompletedTask;

    /// <summary>Invoked when the user changes the DataSet connection override.</summary>
    public Func<string, Task> OnDataSetConnectionChanged { get; init; } = _ => Task.CompletedTask;

    /// <summary>Invoked when the user switches preview mode. Triggers lazy data loading as needed.</summary>
    public Func<string, Task> OnModeChanged { get; init; } = _ => Task.CompletedTask;

    /// <summary>Invoked when the user selects a connection in Table mode. Resets schema/table state.</summary>
    public Func<string, Task> OnConnectionChanged { get; init; } = _ => Task.CompletedTask;

    /// <summary>Invoked when the user clicks a table or view in the schema browser tree.</summary>
    public Func<string, string, Task> OnTableSelected { get; init; } = (_, _) => Task.CompletedTask;

    /// <summary>Invoked when the max-rows value changes.</summary>
    public Func<int, Task> OnMaxRowsChanged { get; init; } = _ => Task.CompletedTask;
}
