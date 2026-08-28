#pragma warning disable CS1591
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Fdw.Data.Abstractions;
using Fdw.UI.Providers;

namespace Fdw.Data.Components.Visualization;

/// <summary>
/// Immutable context for the dynamic filter builder panel.
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class FilterPanelContext : ProviderContextBase
{
    // ── State ──────────────────────────────────────────────────────────────────

    /// <summary>Gets the current list of filter conditions.</summary>
    public IReadOnlyList<FilterCondition> Conditions { get; init; } = [];

    /// <summary>Gets the available column names for filtering.</summary>
    public IReadOnlyList<string> AvailableColumns { get; init; } = [];

    /// <summary>Gets the available filter operator names.</summary>
    public IReadOnlyList<string> AvailableOperators { get; init; } = [];

    /// <summary>Gets whether the panel is in a dirty (unapplied) state.</summary>
    public bool IsDirty { get; init; }


    // ── Callbacks ──────────────────────────────────────────────────────────────

    /// <summary>Invoked to add a new filter condition.</summary>
    public Func<FilterCondition, Task> OnAddCondition { get; init; } = _ => Task.CompletedTask;

    /// <summary>Invoked to remove a filter condition by index.</summary>
    public Func<int, Task> OnRemoveCondition { get; init; } = _ => Task.CompletedTask;

    /// <summary>Invoked to apply all current conditions (triggers data reload).</summary>
    public Func<Task> OnApply { get; init; } = () => Task.CompletedTask;

    /// <summary>Invoked to clear all filter conditions.</summary>
    public Func<Task> OnClear { get; init; } = () => Task.CompletedTask;
}
