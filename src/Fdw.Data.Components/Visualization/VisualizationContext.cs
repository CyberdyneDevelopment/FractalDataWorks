#pragma warning disable CS1591
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Fdw.Data.Abstractions;
using Fdw.Services.Data.Abstractions.Visualization;
using Fdw.Web.Clients.Abstractions.Contracts;
using Fdw.UI.Providers;

namespace Fdw.Data.Components.Visualization;

/// <summary>
/// Immutable context for the visualization provider.
/// Carries the current data, visualization type, config, stats, and filter state.
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class VisualizationContext : ProviderContextBase
{
    // ── State ──────────────────────────────────────────────────────────────────

    /// <summary>Gets the data to visualize.</summary>
    public IDataPreviewResponse? Data { get; init; }

    /// <summary>Gets the currently selected visualization type.</summary>
    public IVisualizationType? VisualizationType { get; init; }

    /// <summary>Gets the visualization configuration.</summary>
    public VisualizationConfig Config { get; init; } = new();

    /// <summary>Gets the computed statistical summary, or null if not applicable.</summary>
    public StatSetResponse? StatSet { get; init; }

    /// <summary>Gets the active filter conditions.</summary>
    public IReadOnlyList<FilterCondition> Filters { get; init; } = [];

    /// <summary>Gets the list of available visualization type names.</summary>
    public IReadOnlyList<string> AvailableVisualizationTypes { get; init; } = [];



    // ── Callbacks ──────────────────────────────────────────────────────────────

    /// <summary>Invoked to change the active visualization type.</summary>
    public Func<IVisualizationType, Task> OnChangeVisualization { get; init; } = _ => Task.CompletedTask;

    /// <summary>Invoked to apply a new set of filter conditions.</summary>
    public Func<IReadOnlyList<FilterCondition>, Task> OnApplyFilters { get; init; } = _ => Task.CompletedTask;

    /// <summary>Invoked to add a calculation to the pipeline.</summary>
    public Func<ColumnCalculation, Task> OnAddCalculation { get; init; } = _ => Task.CompletedTask;
}
