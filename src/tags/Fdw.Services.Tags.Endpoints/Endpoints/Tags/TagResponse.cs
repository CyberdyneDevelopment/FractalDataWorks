namespace Fdw.Services.Tags.Endpoints;

/// <summary>A tag as the API returns it.</summary>
public sealed class TagResponse
{
    /// <summary>Gets or sets the tag's name, unique across the tenant's tags.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets or sets whose tag it is: <c>Tenant</c>, <c>Organization</c> or <c>Workspace</c>.</summary>
    public string OwnerKind { get; set; } = string.Empty;

    /// <summary>Gets or sets the Id of the tenant, organization or workspace that owns the tag.</summary>
    public System.Guid OwnerId { get; set; }

    /// <summary>Gets or sets what the tag means.</summary>
    public string? Description { get; set; }
}
