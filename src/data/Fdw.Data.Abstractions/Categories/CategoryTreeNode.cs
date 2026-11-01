using System;
using System.Collections.Generic;

namespace Fdw.Data.Abstractions;

/// <summary>A node of a built category tree: the node, the path of display names that leads to it, and the nodes under it.</summary>
public sealed class CategoryTreeNode
{
    private readonly List<CategoryTreeNode> _children = [];

    /// <summary>Initializes a new instance of the <see cref="CategoryTreeNode"/> class.</summary>
    /// <param name="node">The node.</param>
    /// <param name="path">The display names from the top of the tree down to this node, joined by the path separator.</param>
    internal CategoryTreeNode(CategoryNode node, string path)
    {
        Node = node ?? throw new ArgumentNullException(nameof(node));
        Path = path ?? throw new ArgumentNullException(nameof(path));
    }

    /// <summary>Gets the node.</summary>
    public CategoryNode Node { get; }

    /// <summary>Gets the display names from the top of the tree down to this node.</summary>
    public string Path { get; }

    /// <summary>Gets the nodes under this one, in display-name order.</summary>
    public IReadOnlyList<CategoryTreeNode> Children => _children;

    internal void Add(CategoryTreeNode child) => _children.Add(child);
}
