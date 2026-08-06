using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Fdw.Operations.Clients.Models;
using Fdw.UI.Providers;

namespace Fdw.UI.DrillDown;

/// <summary>
/// Immutable context object passed to the consumer RenderFragment by <see cref="ConfigurationDrillDownProvider"/>.
/// Carries tree state, configuration metadata for the selected node, and callback delegates.
/// </summary>
// Why: pure view-model — state + callback delegates only, no logic.
[ExcludeFromCodeCoverage]
public sealed class ConfigurationDrillDownContext : ProviderContextBase
{
    // ── Tree State ────────────────────────────────────────────────────────────



    /// <summary>Gets the flat list of top-level nodes in the drill-down tree.</summary>
    public IReadOnlyList<DrillDownNode<object>> Nodes { get; init; } = [];

    /// <summary>Gets the currently selected node, or <c>null</c> if nothing is selected.</summary>
    public DrillDownNode<object>? SelectedNode { get; init; }

    /// <summary>Gets the breadcrumb path from the root to the currently selected node.</summary>
    public IReadOnlyList<DrillDownNode<object>> BreadcrumbPath { get; init; } = [];

    // ── Configuration Metadata ────────────────────────────────────────────────

    /// <summary>Gets the configuration type metadata for the selected node, or <c>null</c> if unavailable.</summary>
    public ConfigurationTypeSummary? SelectedTypeMetadata { get; init; }

    /// <summary>Gets the ValuesFrom references (dropdown sources) for the selected node's type.</summary>
    public IReadOnlyList<RelatedCollectionRef> SelectedNodeValuesFrom { get; init; } = [];

    /// <summary>Gets the property name/value dictionary for the selected node's data object, or <c>null</c> if unavailable.</summary>
    public IDictionary<string, object?>? SelectedNodeProperties { get; init; }

    // ── Instance Info ─────────────────────────────────────────────────────────

    /// <summary>Gets the name of the configuration instance being displayed.</summary>
    public string? InstanceName { get; init; }

    /// <summary>Gets the service category of the configuration instance (e.g., "DataStore").</summary>
    public string? ServiceCategory { get; init; }

    // ── Callbacks ─────────────────────────────────────────────────────────────

    /// <summary>Selects a node in the tree, updating the detail panel and breadcrumb.</summary>
    public Action<DrillDownNode<object>> OnNodeSelected { get; init; } = _ => { };

    /// <summary>Toggles the expand/collapse state of a node.</summary>
    public Action<DrillDownNode<object>> OnToggleExpand { get; init; } = _ => { };


    /// <summary>Loads dropdown values for a named TypeCollection.</summary>
    public Func<string, Task<IReadOnlyList<string>>> OnLoadDropdownValues { get; init; } = _ => Task.FromResult<IReadOnlyList<string>>([]);
}
