using System;

namespace Fdw.Data.Abstractions;

/// <summary>
/// One node of a category tree as a tree reads it (FDW-1465): the node's own stable name, what a person reads, and the node it
/// sits under. A domain that is organised in a category tree maps its stored rows to these and nothing else about the domain
/// reaches the tree.
/// </summary>
public sealed class CategoryNode
{
    /// <summary>Initializes a new instance of the <see cref="CategoryNode"/> class.</summary>
    /// <param name="id">The node's durable identity.</param>
    /// <param name="parentId">The identity of the node it sits under; null for a top-level node.</param>
    /// <param name="name">The node's stable name, unique within its scope (its tenant and organisation).</param>
    /// <param name="displayName">What a person reads; it may repeat under different parents.</param>
    /// <param name="description">What the category is for; null when it says nothing.</param>
    /// <param name="type">The type the node is stored under, which a domain with several kinds of category tells apart.</param>
    /// <param name="tenantId">The tenant the node belongs to; null when the caller states no scope.</param>
    /// <param name="visibilityGroupId">The organisation (visibility group) the node belongs to; null when it belongs to the whole tenant or states no scope.</param>
    public CategoryNode(
        Guid id, Guid? parentId, string name, string displayName, string? description, string type, Guid? tenantId = null, Guid? visibilityGroupId = null)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("A category node has a name.", nameof(name));
        }

        if (string.IsNullOrWhiteSpace(displayName))
        {
            throw new ArgumentException("A category node has a display name.", nameof(displayName));
        }

        if (string.IsNullOrWhiteSpace(type))
        {
            throw new ArgumentException("A category node has a type.", nameof(type));
        }

        Id = id;
        ParentId = parentId;
        Name = name;
        DisplayName = displayName;
        Description = description;
        Type = type;
        TenantId = tenantId;
        VisibilityGroupId = visibilityGroupId;
    }

    /// <summary>Gets the node's durable identity.</summary>
    public Guid Id { get; }

    /// <summary>Gets the identity of the node it sits under; null for a top-level node.</summary>
    public Guid? ParentId { get; }

    /// <summary>
    /// Gets the tenant the node belongs to. Each scope (tenant and organisation) holds its own hierarchy: the tree asks for a name to be
    /// unique within a scope, not across the scopes a caller can see. (The store's unique index is per tenant, which is stricter; the tree
    /// does not depend on that.)
    /// </summary>
    public Guid? TenantId { get; }

    /// <summary>Gets the organisation (visibility group) the node belongs to; null when it belongs to the whole tenant.</summary>
    public Guid? VisibilityGroupId { get; }

    /// <summary>Gets the node's stable name.</summary>
    public string Name { get; }

    /// <summary>Gets what a person reads.</summary>
    public string DisplayName { get; }

    /// <summary>Gets what the category is for; null when it says nothing.</summary>
    public string? Description { get; }

    /// <summary>Gets the type the node is stored under.</summary>
    public string Type { get; }
}
