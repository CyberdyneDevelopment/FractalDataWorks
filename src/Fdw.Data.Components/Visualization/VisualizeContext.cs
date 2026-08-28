using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Fdw.Services.Data.Clients.Models;
using Fdw.UI.Abstractions.Charts;
using Fdw.UI.Providers;

namespace Fdw.Data.Components.Visualization;

/// <summary>
/// Immutable context object passed to the consumer RenderFragment by <see cref="VisualizePageProvider"/>.
/// Carries state snapshots and callback delegates so that the Visualize page markup stays free of logic.
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class VisualizeContext : ProviderContextBase
{
    // ── State ──────────────────────────────────────────────────────────────────

    /// <summary>Gets the list of available datasets for the picker.</summary>
    public IReadOnlyList<DataSetSummaryPayload> DataSets { get; init; } = [];

    /// <summary>Gets the currently selected dataset name (empty when none selected).</summary>
    public string SelectedDataSetName { get; init; } = string.Empty;

    /// <summary>Gets the column names from the most recent preview.</summary>
    public IReadOnlyList<string> Columns { get; init; } = [];

    /// <summary>Gets the data rows from the most recent preview.</summary>
    public IReadOnlyList<IReadOnlyDictionary<string, object?>> Rows { get; init; } = [];

    /// <summary>Gets all registered chart types for the type picker.</summary>
    public IReadOnlyList<IChartType> ChartTypes { get; init; } = [];

    /// <summary>Gets the current chart model. Null until a dataset is selected.</summary>
    public ChartModel? ChartModel { get; init; }

    /// <summary>Gets the name of the currently selected chart type.</summary>
    public string SelectedChartTypeName { get; init; } = string.Empty;

    /// <summary>Gets the encoding bindings: role name → selected column name.</summary>
    public IReadOnlyDictionary<string, string> EncodingBindings { get; init; } =
        new Dictionary<string, string>(StringComparer.Ordinal);



    // ── Callbacks ──────────────────────────────────────────────────────────────

    /// <summary>Invoked when the user selects a dataset. Loads columns and rows.</summary>
    public Func<string, Task> OnSelectDataSet { get; init; } = _ => Task.CompletedTask;

    /// <summary>Invoked when the user changes the chart type.</summary>
    public Func<string, Task> OnSelectChartType { get; init; } = _ => Task.CompletedTask;

    /// <summary>Invoked when the user changes an encoding role binding (roleName, columnName).</summary>
    public Func<string, string, Task> OnBindEncoding { get; init; } = (_, _) => Task.CompletedTask;

    /// <summary>Invoked when the chart type is changed via the ChartHost's built-in dropdown.</summary>
    public Func<IChartType, Task> OnChartTypeChanged { get; init; } = _ => Task.CompletedTask;
}
