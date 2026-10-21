using System;

namespace Fdw.Services.Multitenancy.Endpoints;

/// <summary>
/// Request to make an organization, which a company's people call a department, together with its starting roles and first administrator
/// (FDW-1758).
/// </summary>
public class CreateOrganizationRequest
{
    /// <summary>Gets or sets the organization's display name.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets or sets the organization's slug, unique among the organizations of the tenant.</summary>
    public string Slug { get; set; } = string.Empty;

    /// <summary>Gets or sets the organization of the same tenant whose roles the new one starts with.</summary>
    public Guid CopyRolesFrom { get; set; }

    /// <summary>Gets or sets the person who becomes the organization's first administrator; they must have an account and belong to the tenant.</summary>
    public Guid FirstAdministrator { get; set; }

    /// <summary>Gets or sets the role, among those copied, that the first administrator holds.</summary>
    public string AdministratorRole { get; set; } = string.Empty;
}
