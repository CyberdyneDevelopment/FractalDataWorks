using System.Collections.Generic;

namespace Fdw.UI.DrillDown;

/// <summary>
/// Recursive node model representing a single item in a hierarchical drill-down tree.
/// Each node carries its typed payload, display metadata, and a flat list of heterogeneous children.
/// </summary>
/// <typeparam name="T">The type of the item payload carried by this node.</typeparam>
public sealed class DrillDownNode<T>
{
    /// <summary>Gets the typed item payload for this node.</summary>
    public T Item { get; init; } = default!;

    /// <summary>Gets the primary display label for this node.</summary>
    public string Label { get; init; } = string.Empty;

    /// <summary>Gets the optional subtitle or secondary description.</summary>
    public string? Subtitle { get; init; }

    /// <summary>Gets the logical type of this node (e.g. "Path", "Container", "Field").</summary>
    public string NodeType { get; init; } = string.Empty;

    /// <summary>Gets the zero-based depth of this node in the tree.</summary>
    public int Depth { get; init; }

    /// <summary>Gets or sets a value indicating whether this node is expanded to show children.</summary>
    public bool IsExpanded { get; set; }

    /// <summary>Gets or sets a value indicating whether this node is currently selected.</summary>
    public bool IsSelected { get; set; }

    /// <summary>Gets a value indicating whether this node has no children (leaf node).</summary>
    public bool IsLeaf { get; init; }

    /// <summary>Gets the child nodes. Children use <c>object</c> payloads to allow heterogeneous hierarchies.</summary>
    public IReadOnlyList<DrillDownNode<object>> Children { get; init; } = [];
}
