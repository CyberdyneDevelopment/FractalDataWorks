using System;

namespace Fdw.Services.Tags.Endpoints;

/// <summary>Makes a tag.</summary>
public sealed class CreateTagRequest
{
    /// <summary>Gets or sets the tag's name, at most 100 characters and new across the tenant's tags.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets or sets whose tag it is: <c>Tenant</c>, <c>Organization</c> or <c>Workspace</c>.</summary>
    public string OwnerKind { get; set; } = string.Empty;

    /// <summary>Gets or sets the Id of the tenant, organization or workspace that owns the tag.</summary>
    public Guid OwnerId { get; set; }

    /// <summary>Gets or sets what the tag means.</summary>
    public string? Description { get; set; }
}
