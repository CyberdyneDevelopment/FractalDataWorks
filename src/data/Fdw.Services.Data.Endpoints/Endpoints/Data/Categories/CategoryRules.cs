using System;
using System.Collections.Generic;
using Fdw.Data.Abstractions;
using Fdw.Data.DataSets.Abstractions;
using Fdw.Results;
using Fdw.Services.Data.Clients.Models;

namespace Fdw.Services.Data.Endpoints;

/// <summary>
/// What a write of a category must satisfy (FDW-1465): a name a route can carry, a display name, a type that exists, and a parent that
/// is a category of the tree and of the same type, because a hierarchy is of one type. A move must also make no cycle.
/// </summary>
public static class CategoryRules
{
    /// <summary>Checks a write and finds the parent it names.</summary>
    /// <param name="payload">The category in the request.</param>
    /// <param name="tree">The tree as it is now.</param>
    /// <param name="currentParentId">The parent the category sits under now, when it already exists; null for a new category or a top-level one.</param>
    /// <returns>The identity of the parent, null for a top-level category, or the refusal that names every problem.</returns>
    public static IGenericResult<Guid?> Parent(CategoryPayload payload, CategoryTree tree, Guid? currentParentId = null)
    {
        ArgumentNullException.ThrowIfNull(payload);
        ArgumentNullException.ThrowIfNull(tree);
        List<string> problems = [];
        if (string.IsNullOrWhiteSpace(payload.Name) || payload.Name.Contains('/', StringComparison.Ordinal))
        {
            problems.Add("the category needs a name without a slash");
        }

        if (string.IsNullOrWhiteSpace(payload.DisplayName))
        {
            problems.Add("the category needs a display name");
        }

        if (ReferenceEquals(CategoryTypes.ByName(payload.Type), CategoryTypes.NotFound))
        {
            problems.Add($"the type '{payload.Type}' is not a category type");
        }

        var parent = NamedParent(payload, tree, currentParentId);
        if (!string.IsNullOrWhiteSpace(payload.Parent) && parent is null && tree.IsAmbiguous(payload.Parent))
        {
            problems.Add($"the parent '{payload.Parent}' is a category of more than one tenant or organisation, so the name alone does not say which");
        }
        else if (!string.IsNullOrWhiteSpace(payload.Parent) && parent is null)
        {
            problems.Add($"the parent '{payload.Parent}' is not a category");
        }
        else if (parent is not null && !string.Equals(parent.Node.Type, payload.Type, StringComparison.Ordinal))
        {
            problems.Add($"the parent '{payload.Parent}' is a {parent.Node.Type} category and a {payload.Type} category cannot sit under it");
        }

        return problems.Count > 0
            ? CategoryRefusal.Of<Guid?>(string.Join("; ", problems))
            : GenericResult<Guid?>.Success(parent?.Node.Id);
    }

    // The parent a write names. A name more than one tenant or organisation holds says which only when it is the parent the category already
    // sits under: leaving that parent as it is names nothing new, so an edit of the category's other fields is not refused for it.
    private static CategoryTreeNode? NamedParent(CategoryPayload payload, CategoryTree tree, Guid? currentParentId)
    {
        if (string.IsNullOrWhiteSpace(payload.Parent))
        {
            return null;
        }

        if (!tree.IsAmbiguous(payload.Parent))
        {
            return tree.FindByName(payload.Parent);
        }

        return currentParentId is { } current && tree.Find(current) is { } held && string.Equals(held.Node.Name, payload.Parent, StringComparison.Ordinal)
            ? held
            : null;
    }

    /// <summary>
    /// Checks that everyone who can read a category can read the parent it sits under, because a tree whose parent a reader cannot see is
    /// not a tree for that reader and fails every screen that loads it (FDW-1533).
    /// </summary>
    /// <param name="tree">The tree as it is now.</param>
    /// <param name="parentId">The parent the category is to sit under; null for the top of a hierarchy.</param>
    /// <param name="tenantId">The tenant the category belongs to.</param>
    /// <param name="visibilityGroupId">The organisation the category belongs to; null when it belongs to the whole tenant.</param>
    /// <returns>Success, or the refusal that names the parent.</returns>
    /// <remarks>
    /// A parent covers the category when everyone can read it (a shared tenant's, with no organisation), or when it is the category's own
    /// tenant's and no narrower than the category's organisation.
    /// </remarks>
    public static IGenericResult Scope(CategoryTree tree, Guid? parentId, Guid tenantId, Guid? visibilityGroupId)
    {
        ArgumentNullException.ThrowIfNull(tree);
        if (parentId is not { } id)
        {
            return GenericResult.Success();
        }

        if (tree.Find(id) is not { } held)
        {
            return CategoryRefusal.Of<bool>("the parent is not a category");
        }

        var parent = held.Node;
        var shared = parent.VisibilityGroupId is null && parent.TenantId is { } owner && SharedTenants.Shares(owner);
        var covers = parent.TenantId == tenantId && (parent.VisibilityGroupId is null || parent.VisibilityGroupId == visibilityGroupId);
        return shared || covers
            ? GenericResult.Success()
            : CategoryRefusal.Of<bool>(
                $"the parent '{parent.Name}' belongs to another tenant or organisation, so people who can see this category could not see its parent");
    }

    /// <summary>Checks that moving a category under a parent makes no cycle.</summary>
    /// <param name="tree">The tree as it is now.</param>
    /// <param name="categoryId">The category being moved.</param>
    /// <param name="parentId">The parent it is to sit under; null for the top of the tree.</param>
    /// <returns>Success, or the refusal that says the category would sit under itself.</returns>
    public static IGenericResult Move(CategoryTree tree, Guid categoryId, Guid? parentId)
    {
        ArgumentNullException.ThrowIfNull(tree);
        return tree.WouldMakeCycle(categoryId, parentId)
            ? CategoryRefusal.Of<bool>("the category cannot sit under itself or under a category below it")
            : GenericResult.Success();
    }
}
