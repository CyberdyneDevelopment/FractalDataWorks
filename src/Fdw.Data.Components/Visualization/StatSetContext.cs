#pragma warning disable CS1591
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Fdw.Services.Data.Abstractions.Visualization;
using Fdw.UI.Providers;

namespace Fdw.Data.Components.Visualization;

/// <summary>
/// Immutable context for the per-column statistics display.
/// </summary>
// Why: pure view-model — state + callback delegates only, no logic.
[ExcludeFromCodeCoverage]
public sealed class StatSetContext : ProviderContextBase
{
    // ── State ──────────────────────────────────────────────────────────────────

    /// <summary>Gets the computed statistics response.</summary>
    public StatSetResponse? Stats { get; init; }

    /// <summary>Gets the selected column name (for detail view), or null for summary.</summary>
    public string? SelectedColumn { get; init; }

    /// <summary>Gets the available column names.</summary>
    public IReadOnlyList<string> AvailableColumns { get; init; } = [];



    // ── Callbacks ──────────────────────────────────────────────────────────────

    /// <summary>Invoked to compute statistics for the given columns.</summary>
    public Func<StatSetRequest, Task> OnComputeStats { get; init; } = _ => Task.CompletedTask;

    /// <summary>Invoked when the user selects a column for detail view.</summary>
    public Func<string, Task> OnSelectColumn { get; init; } = _ => Task.CompletedTask;
}
