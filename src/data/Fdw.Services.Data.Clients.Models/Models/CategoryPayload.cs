using System;

namespace Fdw.Services.Data.Clients.Models;

/// <summary>A category as the API writes and reads it (FDW-1465).</summary>
public sealed class CategoryPayload
{
    /// <summary>Gets or sets the category's durable identity. Answered on a read; ignored on a write.</summary>
    public Guid Id { get; set; }

    /// <summary>Gets or sets the category's stable name, unique per tenant, which a route names it by. A write states it once and never changes it.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets or sets the category's type, a name from GET /category-types: DataSet or Custom. A create states it; it never changes, and a category sits under a parent of its own type.</summary>
    public string Type { get; set; } = string.Empty;

    /// <summary>Gets or sets what a person reads; it may repeat under different parents.</summary>
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>Gets or sets what the category is for.</summary>
    public string? Description { get; set; }

    /// <summary>Gets or sets the name of the category it sits under; null for a top-level category.</summary>
    public string? Parent { get; set; }

    /// <summary>Gets or sets the display names from the top of the tree down to this category, joined by " / ". Answered on a read; ignored on a write.</summary>
    public string Path { get; set; } = string.Empty;

    /// <summary>Gets or sets the tenant the category belongs to; each tenant and organisation holds its own hierarchy. Answered on a read; ignored on a write.</summary>
    public Guid? TenantId { get; set; }

    /// <summary>Gets or sets the organisation the category belongs to; null when it belongs to the whole tenant. Answered on a read; ignored on a write.</summary>
    public Guid? VisibilityGroupId { get; set; }
}
