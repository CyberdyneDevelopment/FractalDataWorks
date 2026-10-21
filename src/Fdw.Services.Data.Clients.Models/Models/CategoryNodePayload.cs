using System;
using System.Collections.Generic;

namespace Fdw.Services.Data.Clients.Models;

/// <summary>One category of a tree, with the categories under it (FDW-1465).</summary>
public sealed class CategoryNodePayload
{
    /// <summary>Gets or sets the category's durable identity.</summary>
    public Guid Id { get; set; }

    /// <summary>Gets or sets the category's stable name.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets or sets the category's type.</summary>
    public string Type { get; set; } = string.Empty;

    /// <summary>Gets or sets what a person reads.</summary>
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>Gets or sets what the category is for.</summary>
    public string? Description { get; set; }

    /// <summary>Gets or sets the display names from the top of the tree down to this category, joined by " / ".</summary>
    public string Path { get; set; } = string.Empty;

    /// <summary>Gets or sets the categories under this one, in display-name order.</summary>
    public IList<CategoryNodePayload> Children { get; set; } = [];

    /// <summary>Gets or sets the tenant the category belongs to; each tenant and organisation holds its own hierarchy. Answered on a read; ignored on a write.</summary>
    public Guid? TenantId { get; set; }

    /// <summary>Gets or sets the organisation the category belongs to; null when it belongs to the whole tenant. Answered on a read; ignored on a write.</summary>
    public Guid? VisibilityGroupId { get; set; }
}
