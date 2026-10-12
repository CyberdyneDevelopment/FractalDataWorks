using System;
using System.Collections.Generic;
using Fdw.UI.Abstractions.Pages;

namespace Fdw.UI.Components.Pages;

/// <summary>
/// Concrete implementation of a tree node.
/// </summary>
public sealed class TreeNode : ITreeNode
{
    private readonly List<TreeNode> _children = [];
    private readonly Dictionary<string, object?> _metadata = new(StringComparer.Ordinal);

    /// <inheritdoc />
    public string Id { get; set; } = "";

    /// <inheritdoc />
    public string Label { get; set; } = "";

    /// <inheritdoc />
    public string NodeType { get; set; } = "";

    /// <inheritdoc />
    public string? Icon { get; set; }

    /// <inheritdoc />
    public IReadOnlyList<ITreeNode> Children => _children;

    /// <summary>
    /// Gets the child nodes as concrete types.
    /// </summary>
    public IReadOnlyList<TreeNode> ChildNodes => _children;

    /// <inheritdoc />
    public bool HasChildren => _children.Count > 0;

    /// <inheritdoc />
    public bool IsExpanded { get; set; } = true;

    /// <inheritdoc />
    public bool IsSelectable { get; set; } = true;

    /// <inheritdoc />
    public bool AllowChildren { get; set; } = true;

    /// <inheritdoc />
    public IRowStatus Status { get; set; } = RowStatuses.Normal;

    /// <inheritdoc />
    public IReadOnlyDictionary<string, object?>? Metadata => _metadata.Count > 0 ? _metadata : null;

    /// <inheritdoc />
    public object? Data { get; set; }

    /// <inheritdoc />
    public ITreeNode? Parent { get; private set; }

    /// <inheritdoc />
    public int Depth { get; internal set; }

    /// <summary>
    /// Adds a child node.
    /// </summary>
    public void AddChild(TreeNode child)
    {
        child.Parent = this;
        child.Depth = Depth + 1;
        _children.Add(child);
    }

    /// <summary>
    /// Removes a child node.
    /// </summary>
    public bool RemoveChild(TreeNode child)
    {
        if (_children.Remove(child))
        {
            child.Parent = null;
            return true;
        }
        return false;
    }

    /// <summary>
    /// Sets a metadata value.
    /// </summary>
    public void SetMetadata(string key, object? value) => _metadata[key] = value;

    /// <summary>
    /// Gets a metadata value.
    /// </summary>
    public T? GetMetadata<T>(string key) =>
        _metadata.TryGetValue(key, out var value) && value is T typed ? typed : default;

    /// <summary>
    /// Creates a node for a configuration section.
    /// </summary>
    public static TreeNode Section(string id, string label, string? icon = null) =>
        new() { Id = id, Label = label, NodeType = "section", Icon = icon ?? "📁" };

    /// <summary>
    /// Creates a node for a configuration item.
    /// </summary>
    public static TreeNode Item(string id, string label, object? data = null, string? icon = null) =>
        new() { Id = id, Label = label, NodeType = "item", Data = data, Icon = icon ?? "📄", AllowChildren = false };

    /// <summary>
    /// Creates a node for a field/property.
    /// </summary>
    public static TreeNode Field(string id, string label, object? value, string? icon = null) =>
        new() { Id = id, Label = $"{label}: {value}", NodeType = "field", Data = value, Icon = icon ?? "•", AllowChildren = false, IsSelectable = false };
}