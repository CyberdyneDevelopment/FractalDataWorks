using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Fdw.UI.Providers;

namespace Fdw.Data.UI.Components;

/// <summary>
/// Context exposed by <see cref="DataSetDetailPreviewPane"/> to its child content.
/// Carries the current preview state and action callbacks for the embedded preview pane
/// on the DataSet detail page.
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class DataSetDetailPreviewContext : ProviderContextBase
{
    // ── State ──────────────────────────────────────────────────────────────────

    /// <summary>Gets the column names returned by the last preview query.</summary>
    public IReadOnlyList<string> Columns { get; init; } = [];

    /// <summary>Gets the data rows returned by the last preview query.</summary>
    public IReadOnlyList<IReadOnlyDictionary<string, object?>> Rows { get; init; } = [];



    /// <summary>Gets a value indicating whether the preview pane is currently open.</summary>
    public bool IsPaneOpen { get; init; }

    /// <summary>Gets the current maximum number of rows to return per query.</summary>
    public int RowLimit { get; init; } = 25;

    // ── Callbacks ──────────────────────────────────────────────────────────────

    /// <summary>Gets the callback that triggers a preview query against the DataSet's primary source.</summary>
    public Func<Task> OnLoadPreview { get; init; } = () => Task.CompletedTask;

    /// <summary>Gets the callback that toggles the preview pane open or closed.</summary>
    public Func<Task> OnTogglePane { get; init; } = () => Task.CompletedTask;

    /// <summary>Gets the callback invoked when the user changes the row limit.</summary>
    public Action<int> OnRowLimitChanged { get; init; } = _ => { };

    /// <summary>Gets the callback that exports the current result set as a CSV download.</summary>
    public Func<Task> OnExportCsv { get; init; } = () => Task.CompletedTask;
}
