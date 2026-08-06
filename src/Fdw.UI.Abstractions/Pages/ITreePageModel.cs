using System;
using System.Collections.Generic;

namespace Fdw.UI.Abstractions.Pages;

/// <summary>
/// Represents a tree/hierarchy page for viewing nested structures.
/// </summary>
/// <remarks>
/// Tree pages are ideal for:
/// - DataSet configurations with mappings and joins
/// - Pipeline stages with steps
/// - Workflow definitions
/// - Directory/file structures
/// - Organizational hierarchies
/// </remarks>
public interface ITreePageModel
{
    /// <summary>
    /// Gets the unique identifier for this page.
    /// </summary>
    string Id { get; }

    /// <summary>
    /// Gets the page title.
    /// </summary>
    string Title { get; }

    /// <summary>
    /// Gets the page description.
    /// </summary>
    string? Description { get; }

    /// <summary>
    /// Gets the root nodes of the tree.
    /// </summary>
    IReadOnlyList<ITreeNode> RootNodes { get; }

    /// <summary>
    /// Gets or sets the currently selected node.
    /// </summary>
    ITreeNode? SelectedNode { get; set; }

    /// <summary>
    /// Gets the available actions for the tree (e.g., Add Root, Expand All).
    /// </summary>
    IReadOnlyList<IPageAction> TreeActions { get; }

    /// <summary>
    /// Gets the available actions for nodes (context menu actions).
    /// </summary>
    IReadOnlyList<IPageAction> NodeActions { get; }

    /// <summary>
    /// Gets a value indicating whether drag-and-drop reordering is enabled.
    /// </summary>
    bool AllowReorder { get; }

    /// <summary>
    /// Gets or sets the search/filter text.
    /// </summary>
    string? SearchText { get; set; }

    /// <summary>
    /// Gets a value indicating whether nodes should be expanded by default.
    /// </summary>
    bool ExpandByDefault { get; }
}