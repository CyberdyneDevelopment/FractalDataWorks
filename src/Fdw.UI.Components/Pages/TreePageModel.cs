using System;
using System.Collections.Generic;
using System.Linq;
using Fdw.UI.Abstractions.Pages;

namespace Fdw.UI.Components.Pages;

/// <summary>
/// Concrete implementation of a tree page model.
/// </summary>
public sealed class TreePageModel : ITreePageModel
{
    private readonly List<TreeNode> _rootNodes = [];
    private readonly List<PageAction> _treeActions = [];
    private readonly List<PageAction> _nodeActions = [];

    /// <inheritdoc />
    public string Id { get; set; } = "";

    /// <inheritdoc />
    public string Title { get; set; } = "";

    /// <inheritdoc />
    public string? Description { get; set; }

    /// <inheritdoc />
    public IReadOnlyList<ITreeNode> RootNodes => _rootNodes;

    /// <inheritdoc />
    public ITreeNode? SelectedNode { get; set; }

    /// <inheritdoc />
    public IReadOnlyList<IPageAction> TreeActions => _treeActions;

    /// <inheritdoc />
    public IReadOnlyList<IPageAction> NodeActions => _nodeActions;

    /// <inheritdoc />
    public bool AllowReorder { get; set; }

    /// <inheritdoc />
    public string? SearchText { get; set; }

    /// <inheritdoc />
    public bool ExpandByDefault { get; set; } = true;

    /// <summary>
    /// Adds a root node.
    /// </summary>
    public void AddRootNode(TreeNode node)
    {
        node.Depth = 0;
        _rootNodes.Add(node);
    }

    /// <summary>
    /// Adds a tree-level action.
    /// </summary>
    public void AddTreeAction(PageAction action) => _treeActions.Add(action);

    /// <summary>
    /// Adds a node-level action.
    /// </summary>
    public void AddNodeAction(PageAction action) => _nodeActions.Add(action);

    /// <summary>
    /// Finds a node by ID (searches entire tree).
    /// </summary>
    public TreeNode? FindNode(string id) => FindNodeRecursive(_rootNodes, id);

    private static TreeNode? FindNodeRecursive(IEnumerable<TreeNode> nodes, string id)
    {
        foreach (var node in nodes)
        {
            if (string.Equals(node.Id, id, StringComparison.Ordinal)) return node;
            var found = FindNodeRecursive(node.ChildNodes, id);
            if (found != null) return found;
        }
        return null;
    }

    /// <summary>
    /// Expands all nodes.
    /// </summary>
    public void ExpandAll() => SetExpandedRecursive(_rootNodes, true);

    /// <summary>
    /// Collapses all nodes.
    /// </summary>
    public void CollapseAll() => SetExpandedRecursive(_rootNodes, false);

    private static void SetExpandedRecursive(IEnumerable<TreeNode> nodes, bool expanded)
    {
        foreach (var node in nodes)
        {
            node.IsExpanded = expanded;
            SetExpandedRecursive(node.ChildNodes, expanded);
        }
    }

    /// <summary>
    /// Gets all nodes flattened (for iteration).
    /// </summary>
    public IEnumerable<TreeNode> GetAllNodes() => GetAllNodesRecursive(_rootNodes);

    private static IEnumerable<TreeNode> GetAllNodesRecursive(IEnumerable<TreeNode> nodes)
    {
        foreach (var node in nodes)
        {
            yield return node;
            foreach (var child in GetAllNodesRecursive(node.ChildNodes))
            {
                yield return child;
            }
        }
    }
}