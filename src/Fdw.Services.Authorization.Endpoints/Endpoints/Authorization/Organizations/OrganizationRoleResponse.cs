using System.Collections.Generic;

namespace Fdw.Services.Authorization.Endpoints;

/// <summary>A role of an organization (FDW-1593).</summary>
public class OrganizationRoleResponse
{
    /// <summary>Gets or sets the role's name.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets or sets what the role is for.</summary>
    public string? Description { get; set; }

    /// <summary>Gets or sets whether the organization was made with the role.</summary>
    public bool IsBuiltIn { get; set; }

    /// <summary>Gets or sets the permissions the role carries.</summary>
    public IList<string> Permissions { get; set; } = [];
}
