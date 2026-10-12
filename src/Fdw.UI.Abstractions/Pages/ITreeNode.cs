using System.Collections.Generic;

namespace Fdw.UI.Abstractions.Pages;

/// <summary>
/// Represents a node in a tree structure.
/// </summary>
public interface ITreeNode
{
    /// <summary>
    /// Gets the unique identifier for this node.
    /// </summary>
    string Id { get; }

    /// <summary>
    /// Gets the display label for this node.
    /// </summary>
    string Label { get; }

    /// <summary>
    /// Gets the node type (for styling and context menus).
    /// </summary>
    string NodeType { get; }

    /// <summary>
    /// Gets the icon for this node.
    /// </summary>
    string? Icon { get; }

    /// <summary>
    /// Gets the child nodes.
    /// </summary>
    IReadOnlyList<ITreeNode> Children { get; }

    /// <summary>
    /// Gets a value indicating whether this node has children.
    /// </summary>
    bool HasChildren { get; }

    /// <summary>
    /// Gets or sets a value indicating whether this node is expanded.
    /// </summary>
    bool IsExpanded { get; set; }

    /// <summary>
    /// Gets a value indicating whether this node is selectable.
    /// </summary>
    bool IsSelectable { get; }

    /// <summary>
    /// Gets a value indicating whether children can be added to this node.
    /// </summary>
    bool AllowChildren { get; }

    /// <summary>
    /// Gets the status indicator for this node.
    /// </summary>
    IRowStatus Status { get; }

    /// <summary>
    /// Gets additional metadata for the node (displayed in details panel).
    /// </summary>
    IReadOnlyDictionary<string, object?>? Metadata { get; }

    /// <summary>
    /// Gets the underlying data object for this node.
    /// </summary>
    object? Data { get; }

    /// <summary>
    /// Gets the parent node (null for root nodes).
    /// </summary>
    ITreeNode? Parent { get; }

    /// <summary>
    /// Gets the depth level (0 for root).
    /// </summary>
    int Depth { get; }
}