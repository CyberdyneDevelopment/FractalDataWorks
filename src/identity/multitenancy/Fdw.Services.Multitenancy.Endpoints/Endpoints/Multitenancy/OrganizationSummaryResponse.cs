using System;

namespace Fdw.Services.Multitenancy.Endpoints;

/// <summary>One organization of the caller's tenant, as a list shows it (FDW-1016).</summary>
public class OrganizationSummaryResponse
{
    /// <summary>Gets or sets the organization's durable id.</summary>
    public Guid Id { get; set; }

    /// <summary>Gets or sets the organization's name.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets or sets the organization's slug.</summary>
    public string Slug { get; set; } = string.Empty;

    /// <summary>Gets or sets whether this is the tenant's default organization.</summary>
    public bool IsDefault { get; set; }

    /// <summary>Gets or sets whether this is the tenant's administrative organization.</summary>
    public bool IsAdminOrg { get; set; }

    /// <summary>Gets or sets whether the organization is active.</summary>
    public bool IsActive { get; set; }

    /// <summary>Gets or sets the durable id of the workspace the organization names as its default, or null when it names none.</summary>
    public Guid? DefaultWorkspaceId { get; set; }
}
