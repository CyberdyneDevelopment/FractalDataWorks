using System.Collections.Generic;

namespace Fdw.Services.Authorization.Endpoints;

/// <summary>A request to define or change a role of an organization (FDW-1593).</summary>
public class OrganizationRoleRequest : OrganizationRoleKeyRequest
{
    /// <summary>Gets or sets what the role is for.</summary>
    public string? Description { get; set; }

    /// <summary>Gets or sets the permissions the role carries; at least one.</summary>
    public IList<string> Permissions { get; set; } = [];
}
