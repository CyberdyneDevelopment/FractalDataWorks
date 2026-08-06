using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Fdw.Data.Components;
using Fdw.Data.Components.DataPreview;
using Fdw.Schema.Clients.Models;
using Fdw.Schema.Components.Schema;
using Fdw.Services.Data.Abstractions.Visualization;
using Fdw.UI.Providers;

namespace Fdw.Data.UI.Components;

/// <summary>
/// Context exposed by <see cref="DataPreviewPageProvider"/> to its child content.
/// </summary>
public sealed class DataPreviewPageContext : ProviderContextBase
{
    public string Mode { get; init; } = "";
    public bool IsTableMode => string.Equals(Mode, PreviewMode.Table, StringComparison.Ordinal);
    public string SelectedDataStoreName { get; init; } = "";
    public string SelectedPathName { get; init; } = "";
    public string SelectedContainerName { get; init; } = "";
    public int RowLimit { get; init; } = 25;
    /// <summary>Gets the root-level DataStore nodes for the DataStore → Path → Container picker.</summary>
    public IReadOnlyList<DataStoreNode> DataStorePickerItems { get; init; } = [];
    /// <summary>Gets the async child-loader for the DataStore tree picker.</summary>
    public Func<DataStoreNode, Task<IReadOnlyList<DataStoreNode>>> GetPickerChildren { get; init; }
        = _ => Task.FromResult<IReadOnlyList<DataStoreNode>>([]);
    /// <summary>Gets the callback raised when the picker selection chain changes.</summary>
    public Func<IReadOnlyList<DataStoreNode>, Task> OnPickerSelectionChanged { get; init; }
        = _ => Task.CompletedTask;
    // Why: exposed as IList<T> (not IReadOnlyList<T>) so QueryPanel.AddFilter() can mutate it
    // in-place. The provider assigns its own _filters list directly; mutations in the child
    // component are reflected back to the provider state without needing a round-trip callback.
    public IList<PreviewFilterCondition> Filters { get; init; } = [];
    public IReadOnlyList<string> Columns { get; init; } = [];
    public IReadOnlyList<IReadOnlyDictionary<string, object?>> Rows { get; init; } = [];
    public string SelectedVizType { get; init; } = "Table";
    public IReadOnlyList<IVisualizationType> AvailableVizTypes { get; init; } = [];
    public SchemaContext SchemaContext { get; init; } = new();
    public DataPreviewContext PreviewProvider { get; init; } = new();
    public Func<Task> OnSetTableMode { get; init; } = () => Task.CompletedTask;
    public Func<Task> OnSetDataSetMode { get; init; } = () => Task.CompletedTask;
    public Action<int> OnRowLimitChanged { get; init; } = _ => { };
    public Func<Task> OnExecute { get; init; } = () => Task.CompletedTask;
    public Action<string> OnVizTypeSelected { get; init; } = _ => { };
    public Func<Task> OnExportCsv { get; init; } = () => Task.CompletedTask;
}
