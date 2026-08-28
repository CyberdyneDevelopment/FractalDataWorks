using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Fdw.UI.Providers;

namespace Fdw.UI.DrillDown;

/// <summary>
/// Immutable context object passed to the consumer RenderFragment by <see cref="DrillDownProvider{T}"/>.
/// Carries state snapshots for the drill-down tree and callback delegates for user interaction.
/// </summary>
/// <typeparam name="T">The type of the root data object.</typeparam>
[ExcludeFromCodeCoverage]
public sealed class DrillDownContext<T> : ProviderContextBase
{
    // ── State ──────────────────────────────────────────────────────────────────

    /// <summary>Gets the root data object, or <c>null</c> if not yet loaded.</summary>
    public T? Root { get; init; }



    /// <summary>Gets the flat list of top-level nodes in the drill-down tree.</summary>
    public IReadOnlyList<DrillDownNode<object>> Nodes { get; init; } = [];

    /// <summary>Gets the currently selected node, or <c>null</c> if nothing is selected.</summary>
    public DrillDownNode<object>? SelectedNode { get; init; }

    /// <summary>Gets the breadcrumb path from the root to the currently selected node.</summary>
    public IReadOnlyList<DrillDownNode<object>> BreadcrumbPath { get; init; } = [];

    // ── Callbacks ──────────────────────────────────────────────────────────────

    /// <summary>Selects a node in the tree, updating the detail panel and breadcrumb.</summary>
    public Action<DrillDownNode<object>> OnNodeSelected { get; init; } = _ => { };

    /// <summary>Toggles the expand/collapse state of a node.</summary>
    public Action<DrillDownNode<object>> OnToggleExpand { get; init; } = _ => { };

}
