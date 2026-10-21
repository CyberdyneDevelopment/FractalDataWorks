using System;
using System.Collections.Generic;
using System.Linq;

namespace Fdw.Data.Abstractions;

/// <summary>
/// A category tree built from the nodes a domain stores (FDW-1465): the paths, the nodes under a node, and the checks a write
/// must pass (a parent that exists, a move that makes no cycle). It holds no domain: every domain organised in a tree maps its
/// rows to <see cref="CategoryNode"/> and asks this.
/// </summary>
public sealed class CategoryTree
{
    /// <summary>What joins the display names of a path.</summary>
    public const string PathSeparator = " / ";

    private readonly Dictionary<Guid, CategoryTreeNode> _byId;
    private readonly Dictionary<string, List<CategoryTreeNode>> _byName;

    private CategoryTree(List<CategoryTreeNode> roots, Dictionary<Guid, CategoryTreeNode> byId, Dictionary<string, List<CategoryTreeNode>> byName)
    {
        Roots = roots;
        _byId = byId;
        _byName = byName;
    }

    /// <summary>Gets the top-level nodes, in display-name order.</summary>
    public IReadOnlyList<CategoryTreeNode> Roots { get; }

    /// <summary>Builds the tree from the nodes.</summary>
    /// <param name="nodes">Every node of the tree.</param>
    /// <param name="problems">What is wrong with the nodes: an identity used twice, a name used twice within one tenant and organisation, a parent that is not a node, a cycle. Empty when the tree is sound.</param>
    /// <returns>The tree, or null when <paramref name="problems"/> says the nodes do not make one.</returns>
    public static CategoryTree? TryBuild(IEnumerable<CategoryNode> nodes, out IReadOnlyList<string> problems)
    {
        if (nodes is null)
        {
            throw new ArgumentNullException(nameof(nodes));
        }

        var found = new List<string>();
        var all = nodes.ToList();
        var byId = new Dictionary<Guid, CategoryNode>();
        var names = new HashSet<(string Name, Guid? TenantId, Guid? VisibilityGroupId)>();
        foreach (var node in all)
        {
            if (byId.ContainsKey(node.Id))
            {
                found.Add($"the category identity {node.Id} is used by more than one node");
            }
            else
            {
                byId.Add(node.Id, node);
            }

            // A tenant and each organisation in it hold their own hierarchy, so a name only has to be unique within its scope: an
            // administrator who sees more than one scope sees one hierarchy per scope, and two scopes may both have 'finance'.
            if (!names.Add((node.Name, node.TenantId, node.VisibilityGroupId)))
            {
                found.Add($"the category name '{node.Name}' is used by more than one node of the same tenant and organisation");
            }
        }

        found.AddRange(all
            .Where(node => node.ParentId is { } parent && !byId.ContainsKey(parent))
            .Select(node => $"the category '{node.Name}' sits under a node that does not exist"));
        found.AddRange(Cycles(all, byId));
        problems = found;
        return found.Count > 0 ? null : Assemble(all);
    }

    /// <summary>Finds a node by its identity.</summary>
    /// <param name="id">The identity.</param>
    /// <returns>The node, or null when the tree has none.</returns>
    public CategoryTreeNode? Find(Guid id) => _byId.TryGetValue(id, out var found) ? found : null;

    /// <summary>Finds a node by its stable name.</summary>
    /// <param name="name">The name.</param>
    /// <returns>The node, or null when the tree has none, or when more than one scope holds the name (<see cref="IsAmbiguous"/> tells those apart).</returns>
    public CategoryTreeNode? FindByName(string name) => FindAllByName(name) is { Count: 1 } found ? found[0] : null;

    /// <summary>Finds every node that has a stable name: one per tenant and organisation that holds it.</summary>
    /// <param name="name">The name.</param>
    /// <returns>The nodes; empty when the tree has none.</returns>
    public IReadOnlyList<CategoryTreeNode> FindAllByName(string name)
    {
        if (name is null)
        {
            throw new ArgumentNullException(nameof(name));
        }

        return _byName.TryGetValue(name, out var found) ? found.ToArray() : [];
    }

    /// <summary>Says whether more than one tenant or organisation holds a name, so the name alone cannot say which category is meant.</summary>
    /// <param name="name">The name.</param>
    /// <returns>True when two or more nodes of different scopes have the name.</returns>
    public bool IsAmbiguous(string name) => FindAllByName(name).Count > 1;

    /// <summary>Lists a node and every node under it, at any depth: what a filter on the node includes.</summary>
    /// <param name="id">The node's identity.</param>
    /// <returns>The identities, the node's own first; empty when the tree has no such node.</returns>
    public IReadOnlyList<Guid> SelfAndDescendants(Guid id)
    {
        if (!_byId.TryGetValue(id, out var start))
        {
            return [];
        }

        var found = new List<Guid>();
        var pending = new Stack<CategoryTreeNode>();
        pending.Push(start);
        while (pending.Count > 0)
        {
            var next = pending.Pop();
            found.Add(next.Node.Id);
            foreach (var child in next.Children.Reverse())
            {
                pending.Push(child);
            }
        }

        return found;
    }

    /// <summary>Says whether putting a node under a parent would make the tree loop back on itself.</summary>
    /// <param name="id">The node being moved.</param>
    /// <param name="newParentId">The node it is to sit under; null for the top of the tree.</param>
    /// <returns>True when the parent is the node itself or one of the nodes under it.</returns>
    public bool WouldMakeCycle(Guid id, Guid? newParentId) =>
        newParentId is { } parent && SelfAndDescendants(id).Contains(parent);

    private static CategoryTree Assemble(List<CategoryNode> all)
    {
        var childrenOf = all.Where(node => node.ParentId is not null).ToLookup(node => node.ParentId!.Value);
        var byId = new Dictionary<Guid, CategoryTreeNode>();
        var byName = new Dictionary<string, List<CategoryTreeNode>>(StringComparer.Ordinal);
        var roots = Level(all.Where(node => node.ParentId is null), string.Empty, childrenOf, byId, byName);
        return new CategoryTree(roots, byId, byName);
    }

    private static List<CategoryTreeNode> Level(
        IEnumerable<CategoryNode> nodes,
        string parentPath,
        ILookup<Guid, CategoryNode> childrenOf,
        Dictionary<Guid, CategoryTreeNode> byId,
        Dictionary<string, List<CategoryTreeNode>> byName)
    {
        List<CategoryTreeNode> built = [];
        foreach (var node in nodes
            .OrderBy(node => node.DisplayName, StringComparer.OrdinalIgnoreCase)
            .ThenBy(node => node.Name, StringComparer.Ordinal))
        {
            var treeNode = new CategoryTreeNode(node, parentPath.Length == 0 ? node.DisplayName : parentPath + PathSeparator + node.DisplayName);
            byId.Add(node.Id, treeNode);
            if (!byName.TryGetValue(node.Name, out var holders))
            {
                holders = [];
                byName.Add(node.Name, holders);
            }

            holders.Add(treeNode);
            foreach (var child in Level(childrenOf[node.Id], treeNode.Path, childrenOf, byId, byName))
            {
                treeNode.Add(child);
            }

            built.Add(treeNode);
        }

        return built;
    }

    private static IEnumerable<string> Cycles(List<CategoryNode> all, Dictionary<Guid, CategoryNode> byId)
    {
        var reported = new HashSet<Guid>();
        foreach (var node in all)
        {
            var seen = new HashSet<Guid> { node.Id };
            var parent = node.ParentId;
            while (parent is { } id && byId.TryGetValue(id, out var up))
            {
                if (!seen.Add(id))
                {
                    // The walk re-entered a node: that node is on the loop, which is not always the node the walk began at.
                    if (reported.Add(id))
                    {
                        yield return $"the category '{up.Name}' sits under itself";
                    }

                    break;
                }

                parent = up.ParentId;
            }
        }
    }
}
