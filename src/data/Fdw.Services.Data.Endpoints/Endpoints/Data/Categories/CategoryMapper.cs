using System;
using System.Linq;
using Fdw.Data.Abstractions;
using Fdw.Services.Data.Clients.Models;

namespace Fdw.Services.Data.Endpoints;

/// <summary>Turns a node of the category tree into what the API answers.</summary>
public static class CategoryMapper
{
    /// <summary>Writes a node as one category, naming its parent.</summary>
    /// <param name="tree">The tree the node is in.</param>
    /// <param name="node">The node.</param>
    /// <returns>The payload.</returns>
    public static CategoryPayload Category(CategoryTree tree, CategoryTreeNode node)
    {
        ArgumentNullException.ThrowIfNull(tree);
        ArgumentNullException.ThrowIfNull(node);
        return new CategoryPayload
        {
            Id = node.Node.Id,
            Name = node.Node.Name,
            Type = node.Node.Type,
            DisplayName = node.Node.DisplayName,
            Description = node.Node.Description,
            Parent = node.Node.ParentId is { } parent ? tree.Find(parent)?.Node.Name : null,
            Path = node.Path,
            TenantId = node.Node.TenantId,
            VisibilityGroupId = node.Node.VisibilityGroupId,
        };
    }

    /// <summary>Writes a node with every node under it.</summary>
    /// <param name="node">The node.</param>
    /// <returns>The payload.</returns>
    public static CategoryNodePayload Branch(CategoryTreeNode node)
    {
        ArgumentNullException.ThrowIfNull(node);
        return new CategoryNodePayload
        {
            Id = node.Node.Id,
            Name = node.Node.Name,
            Type = node.Node.Type,
            DisplayName = node.Node.DisplayName,
            Description = node.Node.Description,
            Path = node.Path,
            TenantId = node.Node.TenantId,
            VisibilityGroupId = node.Node.VisibilityGroupId,
            Children = [.. node.Children.Select(Branch)],
        };
    }
}
