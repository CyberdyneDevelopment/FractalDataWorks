using System;

namespace Fdw.Services.Multitenancy.Endpoints;

/// <summary>
/// Request to change an organization (FDW-1758). A part left out is not changed.
/// </summary>
public class UpdateOrganizationRequest
{
    /// <summary>Gets or sets the organization.</summary>
    public Guid OrganizationId { get; set; }

    /// <summary>Gets or sets the new display name.</summary>
    public string? Name { get; set; }

    /// <summary>Gets or sets whether the organization is active: false disables it, true enables it again.</summary>
    public bool? IsActive { get; set; }
}
